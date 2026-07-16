using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Ecological trails: players via <see cref="EntityPlayer.OnFootStep"/>;
    /// animals via <see cref="EntityBehaviorFootTraffic"/> + <see cref="Entity.PhysicsUpdateWatcher"/>.
    /// </summary>
    internal sealed class FootTrafficService
    {
        static FootTrafficService active;

        /// <summary>Cap animal soil/plant applies sharing the same server-ms bucket (herds otherwise spike).</summary>
        const int MaxAnimalAppliesPerServerMs = 8;
        static long animalApplyBudgetMs = -1;
        static int animalAppliesThisMs;

        readonly ColumnTrafficStore store;
        readonly Dictionary<long, Action> hookedFootSteps = new Dictionary<long, Action>();
        readonly BlockPos scratch = new BlockPos(0);
        readonly BlockPos plantScratch = new BlockPos(0);

        ICoreAPI api;
        ICoreServerAPI sapi;
        EcosystemSystem ecosystem;
        PlayerDelegate playerJoinHandler;
        PlayerDelegate playerLeaveHandler;
        PlayerDelegate playerDisconnectHandler;
        EntityDelegate entitySpawnHandler;
        EntityDelegate entityLoadedHandler;

        public FootTrafficService(ColumnTrafficStore trafficStore)
        {
            store = trafficStore;
        }

        /// <summary>Called from <see cref="EntityBehaviorFootTraffic"/> after an animal stride.</summary>
        public static void TryApplyFromAnimal(Entity entity)
        {
            FootTrafficService svc = active;
            if (svc?.api == null || svc.ecosystem == null || entity == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableTrampling || !cfg.EnableAnimalFootTraffic) return;

            // Physics ticks are far denser than game ticks; unbounded herd applies hitch the server.
            long nowMs = entity.World?.ElapsedMilliseconds ?? 0;
            if (nowMs != animalApplyBudgetMs)
            {
                animalApplyBudgetMs = nowMs;
                animalAppliesThisMs = 0;
            }

            if (animalAppliesThisMs >= MaxAnimalAppliesPerServerMs) return;
            animalAppliesThisMs++;

            // Soil coverage is catch-up via ProcessDeferredCoverageSync — avoid SetBlock storms on every stride.
            svc.ApplyEntityFoot(entity, cfg, syncSoil: false);
        }

        public void Bind(ICoreServerAPI serverApi, EcosystemSystem eco)
        {
            Unbind();

            api = serverApi;
            sapi = serverApi;
            ecosystem = eco;
            active = this;

            playerJoinHandler = OnPlayerJoin;
            playerLeaveHandler = OnPlayerLeave;
            playerDisconnectHandler = OnPlayerLeave;
            sapi.Event.PlayerJoin += playerJoinHandler;
            sapi.Event.PlayerLeave += playerLeaveHandler;
            sapi.Event.PlayerDisconnect += playerDisconnectHandler;

            entitySpawnHandler = OnEntityAppear;
            entityLoadedHandler = OnEntityAppear;
            sapi.Event.OnEntitySpawn += entitySpawnHandler;
            sapi.Event.OnEntityLoaded += entityLoadedHandler;

            foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
            {
                HookPlayer(player);
            }

            RefreshAnimalAttachments();
        }

        public void Unbind()
        {
            if (ReferenceEquals(active, this)) active = null;

            DetachAllAnimalBehaviors();

            if (sapi != null)
            {
                if (playerJoinHandler != null) sapi.Event.PlayerJoin -= playerJoinHandler;
                if (playerLeaveHandler != null) sapi.Event.PlayerLeave -= playerLeaveHandler;
                if (playerDisconnectHandler != null) sapi.Event.PlayerDisconnect -= playerDisconnectHandler;
                if (entitySpawnHandler != null) sapi.Event.OnEntitySpawn -= entitySpawnHandler;
                if (entityLoadedHandler != null) sapi.Event.OnEntityLoaded -= entityLoadedHandler;
            }

            UnhookAllPlayers();

            playerJoinHandler = null;
            playerLeaveHandler = null;
            playerDisconnectHandler = null;
            entitySpawnHandler = null;
            entityLoadedHandler = null;
            sapi = null;
            api = null;
            ecosystem = null;
        }

        public void Clear()
        {
            UnhookAllPlayers();
            DetachAllAnimalBehaviors();
        }

        /// <summary>
        /// Attach or detach animal physics hooks to match current config.
        /// Safe to call after config UI / reload.
        /// </summary>
        public void RefreshAnimalAttachments()
        {
            if (sapi?.World == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool want = cfg.EcosystemEnabled && cfg.EnableTrampling && cfg.EnableAnimalFootTraffic;
            if (want)
            {
                AttachAnimalsAlreadyLoaded();
            }
            else
            {
                DetachAllAnimalBehaviors();
            }
        }

        void OnPlayerJoin(IPlayer player) => HookPlayer(player);

        void OnPlayerLeave(IPlayer player) => UnhookPlayer(player);

        void HookPlayer(IPlayer player)
        {
            if (player?.Entity is not EntityPlayer ep) return;
            long id = ep.EntityId;
            if (hookedFootSteps.ContainsKey(id)) return;

            Action handler = () => OnPlayerFootStep(ep);
            hookedFootSteps[id] = handler;
            ep.OnFootStep += handler;
        }

        void UnhookPlayer(IPlayer player)
        {
            if (player?.Entity is not EntityPlayer ep) return;
            UnhookEntity(ep);
        }

        void UnhookEntity(EntityPlayer ep)
        {
            if (ep == null) return;
            long id = ep.EntityId;
            if (!hookedFootSteps.TryGetValue(id, out Action handler)) return;
            ep.OnFootStep -= handler;
            hookedFootSteps.Remove(id);
        }

        void UnhookAllPlayers()
        {
            if (sapi?.World?.AllOnlinePlayers != null)
            {
                foreach (IServerPlayer player in sapi.World.AllOnlinePlayers)
                {
                    UnhookPlayer(player);
                }
            }

            hookedFootSteps.Clear();
        }

        void OnPlayerFootStep(EntityPlayer player)
        {
            if (player?.Pos == null || api == null || ecosystem == null || store == null) return;
            // Creative flight / mid-air: OnFootStep can still fire; skip — not a trail.
            if (!player.OnGround || player.Swimming) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.EnableTrampling) return;

            // Soil sync only on the foot that walked — never from calendar ticks.
            ApplyEntityFoot(player, cfg, syncSoil: true);
        }

        void OnEntityAppear(Entity entity)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.EnableTrampling || !cfg.EnableAnimalFootTraffic) return;
            if (CalendarSpeedHelper.GetSpeedMultiplier(entity.World?.Calendar) > 8f) return;
            // Skip far spawns/loads — PhysicsUpdateWatcher on every loaded creature is the hitch source.
            if (!IsEntityNearAnyPlayer(entity, cfg)) return;
            TryAttachAnimalBehavior(entity);
        }

        void AttachAnimalsAlreadyLoaded()
        {
            if (sapi?.World == null) return;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            foreach (Entity entity in sapi.World.LoadedEntities.Values)
            {
                if (!IsEntityNearAnyPlayer(entity, cfg)) continue;
                TryAttachAnimalBehavior(entity);
            }
        }

        bool IsEntityNearAnyPlayer(Entity entity, EcosystemConfig cfg)
        {
            if (entity?.Pos == null || api == null) return false;
            int radius = cfg.FootTrafficAnimalPlayerRadiusBlocks;
            if (radius <= 0) return true;

            scratch.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
            scratch.dimension = entity.Pos.Dimension;
            return PlayerProximity.IsNearAnyPlayer(api, scratch, radius);
        }

        void DetachAllAnimalBehaviors()
        {
            if (sapi?.World == null) return;

            foreach (Entity entity in sapi.World.LoadedEntities.Values)
            {
                TryDetachAnimalBehavior(entity);
            }
        }

        static void TryAttachAnimalBehavior(Entity entity)
        {
            if (entity == null || entity is EntityPlayer) return;
            if (entity.World?.Side != EnumAppSide.Server) return;
            if (!IsTramplingCreature(entity)) return;
            if (entity.HasBehavior(EntityBehaviorFootTraffic.BehaviorCode)) return;

            var behavior = new EntityBehaviorFootTraffic(entity);
            entity.AddBehavior(behavior);
            behavior.AfterInitialized(onFirstSpawn: false);
        }

        static void TryDetachAnimalBehavior(Entity entity)
        {
            if (entity == null) return;
            EntityBehavior existing = entity.GetBehavior(EntityBehaviorFootTraffic.BehaviorCode);
            if (existing is not EntityBehaviorFootTraffic foot) return;

            foot.UnhookPhysics();
            entity.RemoveBehavior(foot);
        }

        void ApplyEntityFoot(Entity entity, EcosystemConfig cfg, bool syncSoil)
        {
            if (entity?.Pos == null || api == null || ecosystem == null || store == null) return;

            IGameCalendar cal = api.World?.Calendar;
            if (cal == null) return;

            double now = cal.TotalHours;
            float hoursPerDay = cal.HoursPerDay > 0 ? cal.HoursPerDay : 24f;
            float decayPerDay = cfg.FootTrafficDecayPerDay;
            int pressurePerStep = cfg.FootTrafficPressurePerStep;
            if (pressurePerStep < 1) pressurePerStep = 1;

            int fx = (int)entity.Pos.X;
            int fy = (int)entity.Pos.Y;
            int fz = (int)entity.Pos.Z;
            int dim = entity.Pos.Dimension;

            scratch.Set(fx, fy, fz);
            scratch.dimension = dim;

            if (!LandClaimGuard.AllowsEcologyChange(api, scratch)) return;

            byte pressure = store.AddPressure(scratch, pressurePerStep, now, hoursPerDay, decayPerDay);

            int radius = cfg.TramplingRadius;
            if (radius < 0) radius = 0;
            if (radius > 2) radius = 2;

            // Default radius 0: claim already checked for the foot cell.
            bool claimPrechecked = radius == 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx * dx + dz * dz > radius * radius) continue;
                    scratch.Set(fx + dx, fy, fz + dz);
                    scratch.dimension = dim;
                    AffectPlant(scratch, now, hoursPerDay, decayPerDay, cfg, claimPrechecked && dx == 0 && dz == 0);
                }
            }

            if (!syncSoil) return;

            scratch.Set(fx, fy, fz);
            scratch.dimension = dim;
            MaybeSyncSoil(scratch, pressure, cfg);
        }

        /// <summary>Test / direct entry — one footstep at a surface cell.</summary>
        public void ApplyStepAt(
            ICoreAPI coreApi,
            EcosystemSystem eco,
            BlockPos feetPos,
            double nowHours,
            float hoursPerDay,
            EcosystemConfig cfg = null)
        {
            if (coreApi == null || eco == null || feetPos == null || store == null) return;
            cfg ??= EcosystemConfig.Loaded;
            if (!cfg.EnableTrampling) return;

            api = coreApi;
            ecosystem = eco;

            float decay = cfg.FootTrafficDecayPerDay;
            int pressureAdd = cfg.FootTrafficPressurePerStep;
            if (pressureAdd < 1) pressureAdd = 1;

            byte pressure = store.AddPressure(feetPos, pressureAdd, nowHours, hoursPerDay, decay);
            AffectPlant(feetPos, nowHours, hoursPerDay, decay, cfg, claimPrechecked: false);
            MaybeSyncSoil(feetPos, pressure, cfg);
        }

        void AffectPlant(
            BlockPos feetPos,
            double nowHours,
            float hoursPerDay,
            float decayPerDay,
            EcosystemConfig cfg,
            bool claimPrechecked)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            if (!TryResolveTramplePlant(acc, feetPos, out Block block)) return;

            // Lightweight — FromBlock rebuilds full ecology tables and was on the footstep hot path.
            if (PlantCodeHelper.GetEcologyHabitat(block) != EcologyHabitat.Terrestrial) return;
            if (!claimPrechecked && !LandClaimGuard.AllowsEcologyChange(api, plantScratch)) return;

            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            if (string.IsNullOrEmpty(species)) return;

            if (species == "tallgrass")
            {
                if (TallgrassSpreadHeight.TryRetreatOneStage(api, acc, plantScratch))
                {
                    store.ClearPlantHits(plantScratch);
                    ecosystem.InvalidateEnvironmentAround(plantScratch);
                    // Height dropped below target — resume establishment growth.
                    ecosystem.TryQueueTallgrassPromotionAtInspect(plantScratch, acc.GetBlock(plantScratch));
                    return;
                }
            }

            byte hits = store.IncrementPlantHits(plantScratch, nowHours, hoursPerDay, decayPerDay);
            int threshold = cfg.TramplingStressThreshold;
            if (threshold < 1) threshold = 1;

            if (hits < threshold) return;

            store.ClearPlantHits(plantScratch);
            ecosystem.RemoveEcologyPlant(
                plantScratch.Copy(),
                cascadeSymbiosis: true,
                reason: "trampled",
                soilEvent: SoilSuccessionEvent.Death);
        }

        /// <summary>
        /// Feet often sit in the plant cell; sometimes Pos.Y lands on soil or air adjacent to the plant.
        /// Result stays in <see cref="plantScratch"/> (no UpCopy/DownCopy allocs).
        /// </summary>
        bool TryResolveTramplePlant(IBlockAccessor acc, BlockPos feetPos, out Block block)
        {
            plantScratch.Set(feetPos.X, feetPos.Y, feetPos.Z);
            plantScratch.dimension = feetPos.dimension;
            block = acc.GetBlock(plantScratch);
            if (IsTrampleableSurfacePlant(block)) return true;

            plantScratch.Y = feetPos.Y + 1;
            block = acc.GetBlock(plantScratch);
            if (IsTrampleableSurfacePlant(block)) return true;

            plantScratch.Y = feetPos.Y - 1;
            block = acc.GetBlock(plantScratch);
            if (IsTrampleableSurfacePlant(block)) return true;

            block = null;
            return false;
        }

        static bool IsTrampleableSurfacePlant(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (PlantCodeHelper.IsEcologyPlant(block)) return true;
            if (PlantCodeHelper.IsWildBerryBushBlock(block)) return true;
            return PlantCodeHelper.IsThirdPartyEcologyBlock(block);
        }

        /// <summary>
        /// Align soil grass coverage with column pressure (wear and restore).
        /// Skips world work when the column's soil mark already matches the target.
        /// </summary>
        void MaybeSyncSoil(
            BlockPos feetPos,
            byte pressure,
            EcosystemConfig cfg)
        {
            if (!cfg.TramplingSoilDegradation) return;

            byte wearStep = FootTrafficWear.EffectiveWearStep(cfg);
            byte targetMark = FootTrafficWear.MarkForWearIndex(
                FootTrafficWear.TargetWearIndex(pressure, wearStep),
                wearStep);

            if (store.TryGetLastSoilPressure(feetPos, out byte lastMark) && lastMark == targetMark)
            {
                return;
            }

            if (TrafficCoverageSync.SyncAtSurface(api, feetPos, pressure, wearStep))
            {
                store.SetLastSoilPressure(feetPos, targetMark);
            }
        }

        internal static bool IsTramplingCreature(Entity entity)
        {
            if (entity == null || entity is EntityPlayer) return false;
            if (!entity.IsCreature) return false;
            string path = entity.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;
            if (path.StartsWith("item") || path.Contains("projectile")) return false;
            if (entity.SelectionBox == null) return false;
            if (entity.SelectionBox.YSize < 0.45f) return false;
            return true;
        }
    }
}
