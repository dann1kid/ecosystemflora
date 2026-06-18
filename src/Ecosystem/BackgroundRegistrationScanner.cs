using System.Collections.Concurrent;
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

        Thread worker;
        System.Collections.Generic.IList<Block> blocks;
        volatile bool disposed;

        public void Start(System.Collections.Generic.IList<Block> blockRegistry)
        {
            blocks = blockRegistry;
            lock (startLock)
            {
                if (worker != null) return;

                worker = new Thread(WorkerLoop)
                {
                    IsBackground = true,
                    Name = "ecosystemflora-registration-scan",
                };
                worker.Start();
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
            if (inFlightByChunk.ContainsKey(chunkKey)) return false;

            token = System.Threading.Interlocked.Increment(ref nextToken);
            var item = new WorkItem(token, chunkKey, snapshot, in request, highPriority);
            if (!inFlightByChunk.TryAdd(chunkKey, token)) return false;

            if (highPriority)
            {
                priorityQueue.Enqueue(item);
            }
            else
            {
                backgroundQueue.Enqueue(item);
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
            signal.Set();
            worker?.Join(500);
            signal.Dispose();
        }

        long nextToken;

        void WorkerLoop()
        {
            while (!disposed)
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

                    completed.Enqueue(new CompletedWork(
                        item.Token,
                        item.ChunkKey,
                        item.Snapshot.ChunkCoord,
                        in result));
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
