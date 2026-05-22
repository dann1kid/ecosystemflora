using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    public class EcosystemSystem
    {
        public static EcosystemSystem Instance { get; private set; }

        ICoreAPI api;
        readonly ReproducerRegistry registry = new ReproducerRegistry();
        readonly Queue<Vec2i> pendingChunkScans = new Queue<Vec2i>();

        long reproduceListenerId;
        long chunkScanListenerId;
        ChunkColumnLoadedDelegate chunkLoadedHandler;
        ChunkColumnUnloadDelegate chunkUnloadedHandler;

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

            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            reproduceListenerId = api.Event.RegisterGameTickListener(OnReproduceTick, 2000);
            chunkScanListenerId = api.Event.RegisterGameTickListener(OnChunkScanTick, 500);

            if (api is ICoreServerAPI sapi)
            {
                chunkLoadedHandler = OnChunkColumnLoaded;
                chunkUnloadedHandler = OnChunkColumnUnloaded;
                sapi.Event.ChunkColumnLoaded += chunkLoadedHandler;
                sapi.Event.ChunkColumnUnloaded += chunkUnloadedHandler;
            }

            WildFlowerClimate.LogMissingSpecies(api);
        }

        public void Dispose()
        {
            if (api is ICoreServerAPI sapi)
            {
                if (chunkLoadedHandler != null) sapi.Event.ChunkColumnLoaded -= chunkLoadedHandler;
                if (chunkUnloadedHandler != null) sapi.Event.ChunkColumnUnloaded -= chunkUnloadedHandler;
            }

            if (api != null)
            {
                if (reproduceListenerId != 0) api.Event.UnregisterGameTickListener(reproduceListenerId);
                if (chunkScanListenerId != 0) api.Event.UnregisterGameTickListener(chunkScanListenerId);
            }

            pendingChunkScans.Clear();
            reproduceListenerId = 0;
            chunkScanListenerId = 0;
            chunkLoadedHandler = null;
            chunkUnloadedHandler = null;
            Instance = null;
            api = null;
        }

        public EnvironmentalContext Sample(BlockPos plantPos) => EnvironmentalContext.Sample(api, plantPos);

        public bool CanSurviveAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.MeetsSurvivalRequirements(
                requirements, Sample(plantPos), EcosystemConfig.Loaded.HarshWildPlants);
        }

        public string DescribeSurvivalAt(BlockPos plantPos, PlantRequirements requirements)
        {
            return SuitabilityEvaluator.DescribeSurvivalFailure(
                requirements, Sample(plantPos), EcosystemConfig.Loaded.HarshWildPlants);
        }

        public void RegisterReproducer(BlockPos origin, AssetLocation plantBlockCode, PlantRequirements requirements, bool spawnBurst = false)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.OnlyActivateNearPlayers && !PlayerProximity.IsNearAnyPlayer(api, origin, cfg.PlayerActivationRadiusBlocks))
            {
                return;
            }

            try
            {
                Block plantBlock = api.World.GetBlock(plantBlockCode);
                if (plantBlock == null || !EcologyAttributes.ReproduceEnabled(plantBlock)) return;

                AssetLocation spreadCode = PlantCodeHelper.SpreadBlockCode(plantBlock) ?? plantBlockCode.Clone();
                AssetLocation matureCode = PlantCodeHelper.MatureBlockLocation(plantBlock) ?? spreadCode;

                double now = api.World.Calendar.TotalHours;
                double nextAttempt = now;
                if (cfg.StaggerReproduceAttempts)
                {
                    nextAttempt = now + api.World.Rand.NextDouble() * cfg.ReproduceIntervalHours;
                }

                var entry = new ReproducerEntry(
                    origin.Copy(),
                    spreadCode.Clone(),
                    matureCode,
                    requirements,
                    nextAttempt);

                registry.Add(entry);

                if (cfg.ReproduceDebug)
                {
                    api.Logger.Notification("[wildfarming] Registered {0} at {1} (registry {2})", spreadCode, origin, registry.Count);
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

        void OnChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled) return;
            pendingChunkScans.Enqueue(chunkCoord);
        }

        void OnChunkColumnUnloaded(Vec3i chunkCoord)
        {
            registry.RemoveChunk(new Vec2i(chunkCoord.X, chunkCoord.Z));
        }

        void OnChunkScanTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            int columnsLeft = cfg.MaxChunkColumnsScannedPerTick;
            int registrationsLeft = cfg.MaxRegistrationsPerTick;

            while (columnsLeft > 0 && pendingChunkScans.Count > 0 && registrationsLeft > 0)
            {
                Vec2i chunkCoord = pendingChunkScans.Dequeue();
                columnsLeft--;

                if (cfg.OnlyActivateNearPlayers && !PlayerProximity.ChunkNearAnyPlayer(api, chunkCoord, cfg.PlayerActivationRadiusBlocks))
                {
                    continue;
                }

                List<ChunkFlowerHit> hits = ChunkFlowerScanner.ScanColumn(chunkCoord, acc, registrationsLeft);
                foreach (ChunkFlowerHit hit in hits)
                {
                    if (registry.Contains(hit.Pos)) continue;

                    Block block = api.World.GetBlock(hit.BlockCode);
                    PlantRequirements requirements = PlantRequirements.FromBlock(block);
                    RegisterReproducer(hit.Pos, hit.BlockCode, requirements, spawnBurst: false);
                    registrationsLeft--;
                    if (registrationsLeft <= 0) break;
                }
            }
        }

        void OnReproduceTick(float dt)
        {
            if (!EcosystemConfig.Loaded.EcosystemEnabled || api == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            IBlockAccessor acc = api.World.BlockAccessor;
            double now = api.World.Calendar.TotalHours;

            registry.ProcessDue(
                now,
                cfg.ReproduceIntervalHours,
                cfg.MaxReproduceAttemptsPerTick,
                entry =>
                {
                    if (cfg.OnlyActivateNearPlayers && !PlayerProximity.IsNearAnyPlayer(api, entry.Origin, cfg.PlayerActivationRadiusBlocks))
                    {
                        return false;
                    }

                    Block block = acc.GetBlock(entry.Origin);
                    if (block.Id == 0 || !entry.IsMatureBlock(block))
                    {
                        return false;
                    }

                    TrySpawnOffspring(entry, skipChanceRoll: false, maxSpawns: 1);
                    return true;
                });
        }

        void TrySpawnOffspring(ReproducerEntry entry, bool skipChanceRoll, int maxSpawns)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;

            if (!skipChanceRoll && api.World.Rand.NextDouble() > cfg.ReproduceChance) return;

            Block spreadBlock = api.World.GetBlock(entry.JuvenileBlockCode);
            if (spreadBlock == null)
            {
                api.Logger.Warning("[wildfarming] Spread block not found: {0}", entry.JuvenileBlockCode);
                return;
            }

            List<Vec2i> offsets = ReproducePlacement.ShuffledHorizontalOffsets(cfg.ReproduceRadius, api.World.Rand);
            int spawned = 0;
            int loggedFailures = 0;

            foreach (Vec2i offset in offsets)
            {
                if (ReproducePlacement.TryPlaceSpreadNear(
                    api, entry.Origin, offset.X, offset.Y, spreadBlock, entry.Requirements,
                    cfg.MinFitness, cfg.HarshWildPlants, cfg.ReproduceVerticalSearch, cfg.ReproduceDebug,
                    out string failureReason))
                {
                    spawned++;
                    if (cfg.ReproduceDebug)
                    {
                        api.Logger.Notification("[wildfarming] Spawned {0} near {1}", spreadBlock.Code, entry.Origin);
                    }
                    if (spawned >= maxSpawns) return;
                }
                else if (cfg.ReproduceDebug && failureReason != null && loggedFailures < 3)
                {
                    api.Logger.Notification("[wildfarming] Skip ({0},{1}) near {2}: {3}", offset.X, offset.Y, entry.Origin, failureReason);
                    loggedFailures++;
                }
            }
        }
    }
}
