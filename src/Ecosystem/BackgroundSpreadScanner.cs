using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal sealed class BackgroundSpreadScanner : System.IDisposable
    {
        readonly struct WorkItem
        {
            public readonly long Token;
            public readonly long OriginKey;
            public readonly SpreadSolveRequest Request;

            public WorkItem(long token, long originKey, SpreadSolveRequest request)
            {
                Token = token;
                OriginKey = originKey;
                Request = request;
            }
        }

        internal readonly struct CompletedWork
        {
            public readonly long Token;
            public readonly SpreadSolveResult Result;

            public CompletedWork(long token, in SpreadSolveResult result)
            {
                Token = token;
                Result = result;
            }
        }

        readonly ConcurrentQueue<WorkItem> queue = new ConcurrentQueue<WorkItem>();
        readonly ConcurrentQueue<CompletedWork> completed = new ConcurrentQueue<CompletedWork>();
        readonly ConcurrentDictionary<long, byte> pendingOrigins = new ConcurrentDictionary<long, byte>();
        readonly AutoResetEvent signal = new AutoResetEvent(false);
        readonly object startLock = new object();
        readonly object submitLock = new object();

        Thread[] workers;
        IList<Block> blocks;
        volatile bool disposed;
        volatile int workerGeneration;
        long nextToken;
        int maxPending = 128;
        int maxCompleted = 128;
        int rejectedSubmitCount;
        int droppedCompletedCount;

        public int PendingCount => queue.Count;

        /// <summary>Completed results waiting for main-thread drain.</summary>
        public int CompletedCount => completed.Count;

        public int RejectedSubmitCount => Volatile.Read(ref rejectedSubmitCount);

        public int DroppedCompletedCount => Volatile.Read(ref droppedCompletedCount);

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
            maxPending = pendingCap > 0 ? pendingCap : 128;
            maxCompleted = completedCap > 0 ? completedCap : maxPending;
        }

        public void Start(IList<Block> blockRegistry, int workerCount)
        {
            blocks = blockRegistry;
            int count = ResolveWorkerCount(workerCount);
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
                            ? "ecosystemflora-spread-solve"
                            : "ecosystemflora-spread-solve-" + workerIndex,
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

        public bool TrySubmit(SpreadSolveRequest request, out long token)
        {
            token = 0;
            if (request == null || disposed) return false;

            long originKey = OriginKey(request.Origin);

            lock (submitLock)
            {
                // Coalesce: at most one pending solve per origin.
                if (!pendingOrigins.TryAdd(originKey, 0))
                {
                    Interlocked.Increment(ref rejectedSubmitCount);
                    return false;
                }

                if (queue.Count >= maxPending)
                {
                    pendingOrigins.TryRemove(originKey, out _);
                    Interlocked.Increment(ref rejectedSubmitCount);
                    return false;
                }

                token = Interlocked.Increment(ref nextToken);
                queue.Enqueue(new WorkItem(token, originKey, request));
            }

            signal.Set();
            return true;
        }

        public bool TryTakeCompleted(out CompletedWork work) => completed.TryDequeue(out work);

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

        static long OriginKey(BlockPos origin)
        {
            if (origin == null) return 0;
            // Pack X/Z (chunk-local uniqueness is enough for coalesce); include Y in low bits.
            return ((long)origin.X << 32) ^ ((long)(uint)origin.Z << 8) ^ (uint)(origin.Y & 0xFF);
        }

        static int ResolveWorkerCount(int configured)
        {
            if (configured <= 0)
            {
                configured = System.Environment.ProcessorCount / 2;
            }

            if (configured < 1) configured = 1;
            if (configured > 8) configured = 8;
            return configured;
        }

        void WorkerLoop(int workerIndex, int generation)
        {
            var winnersScratch = new List<SpreadSolveWinner>();

            while (!disposed && workerGeneration == generation)
            {
                if (!queue.TryDequeue(out WorkItem item))
                {
                    signal.WaitOne(25);
                    continue;
                }

                try
                {
                    SpreadSolveRequest req = item.Request;
                    var result = new SpreadSolveResult
                    {
                        Origin = req.Origin?.Copy(),
                        SpreadBlock = req.SpreadBlock,
                        Requirements = req.Requirements,
                    };

                    var rand = new System.Random(req.RandomSeed);
                    SpreadSolver.PickWinners(
                        req.Cells,
                        blocks,
                        req.Requirements,
                        req.MinFitness,
                        req.HarshClimate,
                        req.Phase,
                        req.SeasonSpreadMult,
                        req.SeedFitnessScale,
                        req.MaxSpawns,
                        rand,
                        winnersScratch,
                        req.EmptyFirstTwoPhase);

                    for (int i = 0; i < winnersScratch.Count; i++)
                    {
                        result.Winners.Add(winnersScratch[i]);
                    }

                    if (completed.Count >= maxCompleted)
                    {
                        Interlocked.Increment(ref droppedCompletedCount);
                    }
                    else
                    {
                        completed.Enqueue(new CompletedWork(item.Token, in result));
                    }
                }
                catch (System.Exception)
                {
                    // Drop failed solve; sync path can retry on next attempt tick.
                }
                finally
                {
                    pendingOrigins.TryRemove(item.OriginKey, out _);
                }
            }
        }
    }
}
