using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class BackgroundRegistrationScanner : System.IDisposable
    {
        readonly struct WorkItem
        {
            public readonly long Token;
            public readonly long ChunkKey;
            public readonly RegistrationChunkSnapshot Snapshot;
            public readonly ChunkEcologyColumnPass.Request Request;
            public readonly bool HighPriority;

            public WorkItem(
                long token,
                long chunkKey,
                RegistrationChunkSnapshot snapshot,
                in ChunkEcologyColumnPass.Request request,
                bool highPriority)
            {
                Token = token;
                ChunkKey = chunkKey;
                Snapshot = snapshot;
                Request = request;
                HighPriority = highPriority;
            }
        }

        internal readonly struct CompletedWork
        {
            public readonly long Token;
            public readonly long ChunkKey;
            public readonly Vec2i ChunkCoord;
            public readonly ChunkEcologyColumnPass.Result Result;

            public CompletedWork(
                long token,
                long chunkKey,
                Vec2i chunkCoord,
                in ChunkEcologyColumnPass.Result result)
            {
                Token = token;
                ChunkKey = chunkKey;
                ChunkCoord = chunkCoord;
                Result = result;
            }
        }

        readonly ConcurrentQueue<WorkItem> priorityQueue = new ConcurrentQueue<WorkItem>();
        readonly ConcurrentQueue<WorkItem> backgroundQueue = new ConcurrentQueue<WorkItem>();
        readonly ConcurrentQueue<CompletedWork> completed = new ConcurrentQueue<CompletedWork>();
        readonly ConcurrentDictionary<long, byte> cancelledTokens = new ConcurrentDictionary<long, byte>();
        readonly ConcurrentDictionary<long, long> inFlightByChunk = new ConcurrentDictionary<long, long>();
        readonly AutoResetEvent signal = new AutoResetEvent(false);
        readonly object startLock = new object();
        readonly object submitLock = new object();

        Thread[] workers;
        System.Collections.Generic.IList<Block> blocks;
        volatile bool disposed;
        volatile int workerGeneration;
        long nextToken;
        int maxPending = 32;
        int maxCompleted = 32;
        int rejectedSubmitCount;
        int droppedCompletedCount;

        public int PendingCount => priorityQueue.Count + backgroundQueue.Count;

        public int CompletedCount => completed.Count;

        public int RejectedSubmitCount => Volatile.Read(ref rejectedSubmitCount);

        public int DroppedCompletedCount => Volatile.Read(ref droppedCompletedCount);

        /// <summary>Active worker thread count (0 if not started).</summary>
        public int WorkerCount
        {
            get
            {
                lock (startLock)
                {
                    return workers?.Length ?? 0;
                }
            }
        }

        public void ConfigureLimits(int pendingCap, int completedCap)
        {
            maxPending = pendingCap > 0 ? pendingCap : 32;
            maxCompleted = completedCap > 0 ? completedCap : maxPending;
        }

        public void Start(System.Collections.Generic.IList<Block> blockRegistry, int workerCount)
        {
            blocks = blockRegistry;
            int count = RegistrationWorkerScale.Resolve(workerCount);
            lock (startLock)
            {
                if (disposed) return;
                if (workers != null && workers.Length == count) return;

                StopWorkersUnlocked();
                workerGeneration++;
                int generation = workerGeneration;
                workers = new Thread[count];
                for (int i = 0; i < count; i++)
                {
                    int workerIndex = i;
                    int gen = generation;
                    workers[i] = new Thread(() => WorkerLoop(workerIndex, gen))
                    {
                        IsBackground = true,
                        Name = count == 1
                            ? "ecosystemflora-registration-scan"
                            : "ecosystemflora-registration-scan-" + workerIndex,
                    };
                    workers[i].Start();
                }
            }
        }

        void StopWorkersUnlocked()
        {
            if (workers == null) return;

            workerGeneration++;
            signal.Set();
            Thread[] old = workers;
            workers = null;
            for (int i = 0; i < old.Length; i++)
            {
                old[i]?.Join(500);
            }
        }

        public bool IsScanningChunk(Vec2i chunk) =>
            inFlightByChunk.ContainsKey(ChunkKey(chunk));

        public bool TrySubmit(
            RegistrationChunkSnapshot snapshot,
            in ChunkEcologyColumnPass.Request request,
            bool highPriority,
            out long token)
        {
            token = 0;
            if (snapshot == null || disposed) return false;

            long chunkKey = ChunkKey(snapshot.ChunkCoord);

            lock (submitLock)
            {
                if (inFlightByChunk.ContainsKey(chunkKey)) return false;
                if (PendingCount >= maxPending)
                {
                    Interlocked.Increment(ref rejectedSubmitCount);
                    return false;
                }

                token = Interlocked.Increment(ref nextToken);
                if (!inFlightByChunk.TryAdd(chunkKey, token)) return false;

                var item = new WorkItem(token, chunkKey, snapshot, in request, highPriority);
                if (highPriority)
                {
                    priorityQueue.Enqueue(item);
                }
                else
                {
                    backgroundQueue.Enqueue(item);
                }
            }

            signal.Set();
            return true;
        }

        public bool TryTakeCompleted(out CompletedWork work) => completed.TryDequeue(out work);

        public void CancelChunk(Vec2i chunk)
        {
            long chunkKey = ChunkKey(chunk);
            if (inFlightByChunk.TryRemove(chunkKey, out long token))
            {
                cancelledTokens.TryAdd(token, 0);
            }
        }

        public void Dispose()
        {
            disposed = true;
            lock (startLock)
            {
                StopWorkersUnlocked();
            }

            signal.Set();
            signal.Dispose();
        }

        void WorkerLoop(int workerIndex, int generation)
        {
            while (!disposed && workerGeneration == generation)
            {
                if (!TryTakeWork(out WorkItem item))
                {
                    signal.WaitOne(25);
                    continue;
                }

                if (cancelledTokens.TryRemove(item.Token, out _))
                {
                    inFlightByChunk.TryRemove(item.ChunkKey, out _);
                    continue;
                }

                try
                {
                    var view = new SnapshotRegistrationColumnView(item.Snapshot, blocks);
                    ChunkEcologyColumnPass.Result result = ChunkEcologyColumnPass.Run(
                        api: null,
                        view,
                        item.Snapshot.ChunkCoord,
                        item.Request,
                        resumeLx: 0,
                        resumeLz: 0,
                        resumeY: -1,
                        onTreeFound: null,
                        budgetDeadline: 0);

                    if (cancelledTokens.TryRemove(item.Token, out _))
                    {
                        continue;
                    }

                    if (completed.Count >= maxCompleted)
                    {
                        Interlocked.Increment(ref droppedCompletedCount);
                    }
                    else
                    {
                        completed.Enqueue(new CompletedWork(
                            item.Token,
                            item.ChunkKey,
                            item.Snapshot.ChunkCoord,
                            in result));
                    }
                }
                catch (System.Exception)
                {
                    // Drop failed scan; chunk can be re-queued on the next ecology tick.
                }
                finally
                {
                    inFlightByChunk.TryRemove(item.ChunkKey, out _);
                }
            }
        }

        bool TryTakeWork(out WorkItem item)
        {
            if (priorityQueue.TryDequeue(out item)) return true;
            return backgroundQueue.TryDequeue(out item);
        }

        internal static long ChunkKey(Vec2i chunk) =>
            ((long)chunk.X << 32) | (uint)chunk.Y;
    }
}
