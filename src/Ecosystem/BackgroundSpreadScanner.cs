using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal sealed class BackgroundSpreadScanner : System.IDisposable
    {
        readonly struct WorkItem
        {
            public readonly long Token;
            public readonly SpreadSolveRequest Request;

            public WorkItem(long token, SpreadSolveRequest request)
            {
                Token = token;
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
        readonly AutoResetEvent signal = new AutoResetEvent(false);
        readonly object startLock = new object();

        Thread[] workers;
        IList<Block> blocks;
        volatile bool disposed;
        long nextToken;

        public int PendingCount => queue.Count;

        public void Start(IList<Block> blockRegistry, int workerCount)
        {
            blocks = blockRegistry;
            int count = ResolveWorkerCount(workerCount);
            lock (startLock)
            {
                if (workers != null) return;

                workers = new Thread[count];
                for (int i = 0; i < count; i++)
                {
                    int workerIndex = i;
                    workers[i] = new Thread(() => WorkerLoop(workerIndex))
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

        public bool TrySubmit(SpreadSolveRequest request, out long token)
        {
            token = 0;
            if (request == null || disposed) return false;

            token = Interlocked.Increment(ref nextToken);
            queue.Enqueue(new WorkItem(token, request));
            signal.Set();
            return true;
        }

        public bool TryTakeCompleted(out CompletedWork work) => completed.TryDequeue(out work);

        public void Dispose()
        {
            disposed = true;
            signal.Set();
            if (workers != null)
            {
                for (int i = 0; i < workers.Length; i++)
                {
                    workers[i]?.Join(500);
                }
            }

            signal.Dispose();
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

        void WorkerLoop(int workerIndex)
        {
            var winnersScratch = new List<SpreadSolveWinner>();

            while (!disposed)
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

                    completed.Enqueue(new CompletedWork(item.Token, in result));
                }
                catch (System.Exception)
                {
                    // Drop failed solve; sync path can retry on next attempt tick.
                }
            }
        }
    }
}
