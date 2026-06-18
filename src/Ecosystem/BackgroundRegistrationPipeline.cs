using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Snapshot capture on main thread; column classification on a background worker.</summary>
    internal sealed class BackgroundRegistrationPipeline : System.IDisposable
    {
        sealed class ChunkWork
        {
            public RegistrationChunkSnapshotBuilder Builder;
            public bool Submitted;
        }

        readonly BackgroundRegistrationScanner scanner = new BackgroundRegistrationScanner();
        readonly System.Collections.Generic.Dictionary<long, ChunkWork> active =
            new System.Collections.Generic.Dictionary<long, ChunkWork>();

        public void Start(System.Collections.Generic.IList<Block> blockRegistry) => scanner.Start(blockRegistry);

        public bool IsBusy(Vec2i chunk) =>
            scanner.IsScanningChunk(chunk) || active.ContainsKey(BackgroundRegistrationScanner.ChunkKey(chunk));

        public void PollCompleted(EcosystemSystem eco, EcosystemConfig cfg)
        {
            while (scanner.TryTakeCompleted(out BackgroundRegistrationScanner.CompletedWork done))
            {
                active.Remove(done.ChunkKey);
                ApplyScanResult(eco, cfg, done.ChunkCoord, done.Result);
            }
        }

        public bool TryAdvance(
            EcosystemSystem eco,
            IBlockAccessor acc,
            EcosystemConfig cfg,
            PendingChunkScan job,
            bool highPriority,
            long deadlineTicks,
            out bool needsRequeue)
        {
            needsRequeue = false;
            if (acc == null || eco == null || cfg == null) return false;

            long chunkKey = BackgroundRegistrationScanner.ChunkKey(job.ChunkCoord);
            if (scanner.IsScanningChunk(job.ChunkCoord))
            {
                needsRequeue = true;
                return true;
            }

            if (!active.TryGetValue(chunkKey, out ChunkWork work))
            {
                work = new ChunkWork
                {
                    Builder = new RegistrationChunkSnapshotBuilder(job.ChunkCoord),
                };
                active[chunkKey] = work;
            }

            if (!work.Builder.Completed)
            {
                IMapChunk mapChunk = acc.GetMapChunk(job.ChunkCoord.X, job.ChunkCoord.Y);
                if (mapChunk == null)
                {
                    eco.NotifyRegistrationScanCompleted(job.ChunkCoord);
                    active.Remove(chunkKey);
                    return true;
                }

                if (!work.Builder.Advance(
                        acc,
                        mapChunk,
                        cfg.MaxRegistrationSnapshotCellsPerTick,
                        deadlineTicks))
                {
                    needsRequeue = true;
                    return true;
                }
            }

            if (!work.Submitted)
            {
                int maxHits = PendingRegistrationQueue.MaxHitsPerPass;
                var request = new ChunkEcologyColumnPass.Request
                {
                    MaxFlowerHits = maxHits,
                    MaxTreeHits = maxHits,
                    MaxVineHits = cfg.EnableWildVineEcology ? maxHits : 0,
                    SyncFoliage = false,
                    FoliageIndex = null,
                };

                if (!scanner.TrySubmit(work.Builder.Snapshot, in request, highPriority, out _))
                {
                    needsRequeue = true;
                    return true;
                }

                work.Submitted = true;
            }

            return true;
        }

        public void OnChunkUnload(Vec2i chunk)
        {
            long chunkKey = BackgroundRegistrationScanner.ChunkKey(chunk);
            active.Remove(chunkKey);
            scanner.CancelChunk(chunk);
        }

        public void Dispose() => scanner.Dispose();

        void ApplyScanResult(
            EcosystemSystem eco,
            EcosystemConfig cfg,
            Vec2i chunkCoord,
            ChunkEcologyColumnPass.Result pass)
        {
            eco.EnqueueRegistrationScanHits(chunkCoord, pass);
            if (pass.Completed)
            {
                eco.NotifyRegistrationScanCompleted(chunkCoord);
            }
        }
    }
}
