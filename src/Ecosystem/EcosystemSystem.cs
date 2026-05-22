using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    public class EcosystemSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly List<ReproducerEntry> reproducers = new List<ReproducerEntry>();
        long reproduceListenerId;
        ChunkColumnLoadedDelegate chunkLoadedHandler;

        public void InitPre(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            try
            {
                EcosystemConfig fromDisk = api.LoadModConfig<EcosystemConfig>("wildfarming-ecosystem.json");
                if (fromDisk != null) EcosystemConfig.Loaded = fromDisk;
                else api.StoreModConfig(EcosystemConfig.Loaded, "wildfarming-ecosystem.json");
            }
            catch
            {
                api.StoreModConfig(EcosystemConfig.Loaded, "wildfarming-ecosystem.json");
            }
        }

        public void Init(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Server) return;

            this.api = api;
            Instance = this;

            if (EcosystemConfig.Loaded.EcosystemEnabled)
            {
                reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, 8000);
            }

            if (api is ICoreServerAPI sapi)
            {
                chunkLoadedHandler = OnChunkColumnLoaded;
                sapi.Event.ChunkColumnLoaded += chunkLoadedHandler;
            }
        }

        public void Dispose()
        {
            if (api is ICoreServerAPI sapi && chunkLoadedHandler != null)
            {
                sapi.Event.ChunkColumnLoaded -= chunkLoadedHandler;
                chunkLoadedHandler = null;
            }

            if (api != null && reproduceListenerId != 0)
            {
                api.Event.UnregisterGameTickListener(reproduceListenerId);
            }
            reproducers.Clear();
            reproduceListenerId = 0;
            Instance = null;
            api = null;
        }

        public EnvironmentalContext Sample(BlockPos plantPos)
        {
            return EnvironmentalContext.Sample(api, plantPos);
        }

        public bool CanSurviveAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.MeetsSurvivalRequirements(
                requirements,
                Sample(plantPos),
                EcosystemConfig.Loaded.HarshWildPlants);
        }

        public string DescribeSurvivalAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.DescribeSurvivalFailure(
                requirements,
                Sample(plantPos),
                EcosystemConfig.Loaded.HarshWildPlants);
        }

        public void RegisterReproducer(BlockPos origin, AssetLocation plantBlockCode, PlantRequirements requirements, bool spawnBurst = false)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            try
            {
                Block plantBlock = api.World.GetBlock(plantBlockCode);
                if (plantBlock == null || !EcologyAttributes.ReproduceEnabled(plantBlock))
                {
                    return;
                }

                AssetLocation spreadCode = PlantCodeHelper.SpreadBlockCode(plantBlock) ?? plantBlockCode.Clone();
                AssetLocation matureCode = PlantCodeHelper.MatureBlockLocation(plantBlock) ?? spreadCode;

                for (int i = reproducers.Count - 1; i >= 0; i--)
                {
                    if (reproducers[i].Origin.Equals(origin))
                    {
                        reproducers.RemoveAt(i);
                    }
                }

                AssetLocation spreadCopy = spreadCode.Clone();
                double now = api.World.Calendar.TotalHours;
                var entry = new ReproducerEntry(origin.Copy(), spreadCopy, matureCode, requirements, now);
                reproducers.Add(entry);

                if (EcosystemConfig.Loaded.ReproduceDebug)
                {
                    api.Logger.Notification("[wildfarming] Registered {0} at {1}", spreadCopy, origin);
                }

                if (spawnBurst)
                {
                    TrySpawnOffspring(entry, skipChanceRoll: true, maxSpawns: 3);
                }
            }
            catch (System.Exception ex)
            {
                api.Logger.Error("[wildfarming] RegisterReproducer failed at {0}: {1}", origin, ex);
            }
        }

        void OnReproduceTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            double now = api.World.Calendar.TotalHours;
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            for (int i = reproducers.Count - 1; i >= 0; i--)
            {
                ReproducerEntry entry = reproducers[i];
                Block block = acc.GetBlock(entry.Origin);

                if (block.Id == 0 || !entry.IsMatureBlock(block))
                {
                    reproducers.RemoveAt(i);
                    continue;
                }

                if (now < entry.NextAttemptHours) continue;

                entry.NextAttemptHours = now + cfg.ReproduceIntervalHours;
                TrySpawnOffspring(entry, skipChanceRoll: false, maxSpawns: 1);
            }
        }

        void TrySpawnOffspring(ReproducerEntry entry, bool skipChanceRoll, int maxSpawns)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            if (!skipChanceRoll && api.World.Rand.NextDouble() > cfg.ReproduceChance)
            {
                return;
            }

            Block juvenile = api.World.GetBlock(entry.JuvenileBlockCode);
            if (juvenile == null)
            {
                api.Logger.Warning("[wildfarming] Juvenile block not found: {0}", entry.JuvenileBlockCode);
                return;
            }

            List<Vec2i> offsets = ReproducePlacement.ShuffledHorizontalOffsets(cfg.ReproduceRadius, api.World.Rand);
            int spawned = 0;
            int loggedFailures = 0;

            foreach (Vec2i offset in offsets)
            {
                if (ReproducePlacement.TryPlaceSpreadNear(
                    api,
                    entry.Origin,
                    offset.X,
                    offset.Y,
                    juvenile,
                    entry.Requirements,
                    cfg.MinFitness,
                    cfg.HarshWildPlants,
                    cfg.ReproduceVerticalSearch,
                    cfg.ReproduceDebug,
                    out string failureReason))
                {
                    spawned++;
                    if (EcosystemConfig.Loaded.ReproduceDebug)
                    {
                        api.Logger.Notification("[wildfarming] Spawned {0} near {1}", juvenile.Code, entry.Origin);
                    }
                    if (spawned >= maxSpawns) return;
                }
                else if (cfg.ReproduceDebug && failureReason != null && loggedFailures < 5)
                {
                    api.Logger.Notification("[wildfarming] Skip column ({0},{1}) near {2}: {3}", offset.X, offset.Y, entry.Origin, failureReason);
                    loggedFailures++;
                }
            }

            if (cfg.ReproduceDebug && spawned == 0)
            {
                api.Logger.Notification("[wildfarming] No spawn cell near {0} (radius {1}, minFitness {2})", entry.Origin, cfg.ReproduceRadius, cfg.MinFitness);
            }
        }

        void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            int chunkSize = GlobalConstants.ChunkSize;
            int x0 = chunkCoord.X * chunkSize;
            int z0 = chunkCoord.Y * chunkSize;
            int y1 = acc.MapSizeY - 1;

            BlockPos start = new BlockPos(x0, 0, z0);
            BlockPos end = new BlockPos(x0 + chunkSize - 1, y1, z0 + chunkSize - 1);

            acc.WalkBlocks(start, end, (block, x, y, z) =>
            {
                if (!EcologyAttributes.ReproduceEnabled(block)) return;

                BlockPos pos = new BlockPos(x, y, z);
                PlantRequirements requirements = PlantRequirements.FromBlock(block);
                RegisterReproducer(pos, block.Code, requirements, spawnBurst: false);
            });
        }

        sealed class ReproducerEntry : IReproducible
        {
            public BlockPos Origin { get; }
            public AssetLocation JuvenileBlockCode { get; }
            public AssetLocation MatureBlockCode { get; }
            public PlantRequirements Requirements { get; }
            public double NextAttemptHours { get; set; }

            public ReproducerEntry(
                BlockPos origin,
                AssetLocation juvenileBlockCode,
                AssetLocation matureBlockCode,
                PlantRequirements requirements,
                double nextAttemptHours)
            {
                Origin = origin;
                JuvenileBlockCode = juvenileBlockCode;
                MatureBlockCode = matureBlockCode;
                Requirements = requirements;
                NextAttemptHours = nextAttemptHours;
            }

            public bool IsMatureBlock(Block block)
            {
                return block?.Code != null && block.Code.Equals(MatureBlockCode);
            }
        }
    }
}
