using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Main-thread env capture; worker scoring; main-thread PendingSpreadQueue commit.</summary>
    internal sealed class BackgroundSpreadPipeline : System.IDisposable
    {
        readonly BackgroundSpreadScanner scanner = new BackgroundSpreadScanner();
        int tickSolveQueued;
        int tickSolveCompleted;

        public int WorkerPendingCount => scanner.PendingCount;

        public int LastTickSolveQueued => tickSolveQueued;

        public int LastTickSolveCompleted => tickSolveCompleted;

        public void BeginReproduceTick()
        {
            tickSolveQueued = 0;
            tickSolveCompleted = 0;
        }

        public void Start(System.Collections.Generic.IList<Block> blockRegistry, int workerCount) =>
            scanner.Start(blockRegistry, workerCount);

        public bool TryQueueSolve(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand)
        {
            if (api == null || origin == null || spreadBlock == null || requirements == null) return false;
            if (!SpreadSolveBatchBuilder.CanBackgroundSolve(requirements)) return false;

            if (!SpreadSolveBatchBuilder.TryBuildRequest(
                    api,
                    origin,
                    spreadBlock,
                    requirements,
                    minFitness,
                    harshClimate,
                    radius,
                    verticalSearch,
                    maxSpawns,
                    rand,
                    out SpreadSolveRequest request))
            {
                return false;
            }

            if (!scanner.TrySubmit(request, out _))
            {
                return false;
            }

            tickSolveQueued++;
            return true;
        }

        public void PollCompleted(EcosystemSystem eco, bool logFailures)
        {
            while (scanner.TryTakeCompleted(out BackgroundSpreadScanner.CompletedWork done))
            {
                tickSolveCompleted++;
                ApplyResult(eco, in done.Result, logFailures);
            }
        }

        public void Dispose() => scanner.Dispose();

        void ApplyResult(EcosystemSystem eco, in SpreadSolveResult result, bool logFailures)
        {
            if (eco == null || result.SpreadBlock == null || result.Requirements == null) return;

            ICoreAPI api = eco.ServerApi;
            if (api == null) return;

            PendingSpreadQueue queue = eco.PendingSpreads;
            if (queue == null) return;

            for (int i = 0; i < result.Winners.Count; i++)
            {
                SpreadSolveWinner winner = result.Winners[i];
                if (winner.TargetPos == null) continue;

                queue.Enqueue(new PendingSpreadIntent
                {
                    ParentOrigin = result.Origin?.Copy(),
                    TargetPos = winner.TargetPos.Copy(),
                    SpreadBlock = result.SpreadBlock,
                    Requirements = result.Requirements,
                    Displacing = winner.Displacing,
                });

                if (logFailures)
                {
                    api.Logger.Notification(
                        "[ecosystemflora] Worker queued {0} {1} at {2} near {3}",
                        winner.Displacing ? "displace" : "spread",
                        result.SpreadBlock.Code,
                        winner.TargetPos,
                        result.Origin);
                }
            }
        }
    }
}
