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

        readonly ColumnTrafficStore store;
        readonly Dictionary<long, Action> hookedFootSteps = new Dictionary<long, Action>();
        readonly BlockPos scratch = new BlockPos(0);

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
            svc.ApplyEntityFoot(entity, cfg);
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

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.EnableTrampling) return;

            ApplyEntityFoot(player, cfg);
        }

        void OnEntityAppear(Entity entity)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.EnableTrampling || !cfg.EnableAnimalFootTraffic) return;
            TryAttachAnimalBehavior(entity);
        }

        void AttachAnimalsAlreadyLoaded()
        {
            if (sapi?.World == null) return;

            foreach (Entity entity in sapi.World.LoadedEntities.Values)
            {
                TryAttachAnimalBehavior(entity);
            }
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

        void ApplyEntityFoot(Entity entity, EcosystemConfig cfg)
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
            if (!TryResolveTramplePlant(acc, feetPos, out BlockPos plantPos, out Block block)) return;

            // Trample wear must hit immature tallgrass / juveniles — not only spread parents.
            PlantRequirements req = PlantRequirements.FromBlock(block);
            if (req.Habitat != EcologyHabitat.Terrestrial) return;
            if (!claimPrechecked && !LandClaimGuard.AllowsEcologyChange(api, plantPos)) return;

            string species = req.Species ?? PlantCodeHelper.ResolveEcologySpecies(block);
            if (string.IsNullOrEmpty(species)) return;

            if (species == "tallgrass")
            {
                if (TallgrassSpreadHeight.TryRetreatOneStage(api, acc, plantPos))
                {
                    store.ClearPlantHits(plantPos);
                    ecosystem.InvalidateEnvironmentAround(plantPos);
                    return;
                }
            }

            byte hits = store.IncrementPlantHits(plantPos, nowHours, hoursPerDay, decayPerDay);
            int threshold = cfg.TramplingStressThreshold;
            if (threshold < 1) threshold = 1;

            if (hits < threshold) return;

            store.ClearPlantHits(plantPos);
            ecosystem.RemoveEcologyPlant(
                plantPos,
                cascadeSymbiosis: true,
                reason: "trampled",
                soilEvent: SoilSuccessionEvent.Death);
        }

        /// <summary>
        /// Feet often sit in the plant cell; sometimes Pos.Y lands on soil or air adjacent to the plant.
        /// </summary>
        static bool TryResolveTramplePlant(
            IBlockAccessor acc,
            BlockPos feetPos,
            out BlockPos plantPos,
            out Block block)
        {
            plantPos = feetPos;
            block = acc.GetBlock(feetPos);
            if (IsTrampleableSurfacePlant(block)) return true;

            BlockPos up = feetPos.UpCopy();
            block = acc.GetBlock(up);
            if (IsTrampleableSurfacePlant(block))
            {
                plantPos = up;
                return true;
            }

            BlockPos down = feetPos.DownCopy();
            block = acc.GetBlock(down);
            if (IsTrampleableSurfacePlant(block))
            {
                plantPos = down;
                return true;
            }

            plantPos = feetPos;
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
