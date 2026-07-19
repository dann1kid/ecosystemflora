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
        int tickSolveRejected;

        public int WorkerPendingCount => scanner.PendingCount;

        public int LastTickSolveQueued => tickSolveQueued;

        public int LastTickSolveCompleted => tickSolveCompleted;

        public int LastTickSolveRejected => tickSolveRejected;

        public void BeginReproduceTick()
        {
            tickSolveQueued = 0;
            tickSolveCompleted = 0;
            tickSolveRejected = 0;
        }

        public void Start(System.Collections.Generic.IList<Block> blockRegistry, int workerCount)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded ?? new EcosystemConfig();
            scanner.ConfigureLimits(
                cfg.ResolveMaxSpreadSolvePending(),
                cfg.ResolveMaxSpreadSolveCompleted());
            scanner.Start(blockRegistry, workerCount);
        }

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
                tickSolveRejected++;
                return false;
            }

            tickSolveQueued++;
            return true;
        }

        public void PollCompleted(EcosystemSystem eco, bool logFailures, int maxDrain = 0)
        {
            if (maxDrain <= 0) maxDrain = int.MaxValue;

            int drained = 0;
            while (drained < maxDrain && scanner.TryTakeCompleted(out BackgroundSpreadScanner.CompletedWork done))
            {
                tickSolveCompleted++;
                drained++;
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

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            int intentCap = cfg != null ? cfg.ResolveMaxPendingSpreadIntents() : 256;

            for (int i = 0; i < result.Winners.Count; i++)
            {
                SpreadSolveWinner winner = result.Winners[i];
                if (winner.TargetPos == null) continue;

                if (intentCap > 0 && queue.Count >= intentCap)
                {
                    break;
                }

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

            if (result.Winners.Count == 0)
            {
                eco.NotifySpreadSolveNoWinners(result.Origin, result.Requirements);
            }
        }
    }
}
