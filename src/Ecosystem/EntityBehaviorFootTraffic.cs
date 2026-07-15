using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Animal stride hook via <see cref="Entity.PhysicsUpdateWatcher"/> (players use <see cref="EntityPlayer.OnFootStep"/>).
    /// Attached only while animal foot traffic is enabled; scoped near players.
    /// </summary>
    public sealed class EntityBehaviorFootTraffic : EntityBehavior
    {
        public const string BehaviorCode = "ecosystemFootTraffic";

        /// <summary>Shared near-player snapshot so N animals don't re-scan players each physics tick.</summary>
        static readonly PlayerProximity.Snapshot NearPlayers = new PlayerProximity.Snapshot();
        static long nearPlayersRefreshMs;
        const int NearPlayersRefreshIntervalMs = 250;

        float strideAccum;
        PhysicsTickDelegate physicsHandler;
        readonly BlockPos nearScratch = new BlockPos(0);

        public EntityBehaviorFootTraffic(Entity entity) : base(entity)
        {
        }

        public override string PropertyName() => BehaviorCode;

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override void AfterInitialized(bool onFirstSpawn)
        {
            base.AfterInitialized(onFirstSpawn);
            HookPhysics();
        }

        public void HookPhysics()
        {
            if (entity?.World == null || entity.World.Side != EnumAppSide.Server) return;
            if (entity is EntityPlayer) return;
            if (physicsHandler != null) return;

            physicsHandler = OnPhysicsUpdate;
            entity.PhysicsUpdateWatcher += physicsHandler;
        }

        public void UnhookPhysics()
        {
            if (physicsHandler != null && entity != null)
            {
                entity.PhysicsUpdateWatcher -= physicsHandler;
                physicsHandler = null;
            }
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            UnhookPhysics();
            base.OnEntityDespawn(despawn);
        }

        void OnPhysicsUpdate(float accum, Vec3d prevPos)
        {
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EcosystemEnabled || !cfg.EnableTrampling || !cfg.EnableAnimalFootTraffic) return;
            if (entity == null || entity is EntityPlayer) return;
            if (CalendarSpeedHelper.GetSpeedMultiplier(entity.World?.Calendar) > 8f) return;
            if (prevPos == null) return;
            if (!entity.OnGround || entity.Swimming) return;
            if (entity.State != EnumEntityState.Active) return;

            // Standing still: skip before near-player snapshot work.
            double dx = entity.Pos.X - prevPos.X;
            double dz = entity.Pos.Z - prevPos.Z;
            float distSq = (float)(dx * dx + dz * dz);
            if (distSq < 0.0001f) return;

            // Far from players: skip before Sqrt / apply.
            if (!IsNearAnyPlayer(entity, cfg)) return;

            float dist = MathF.Sqrt(distSq);
            float stride = cfg.FootTrafficAnimalStrideBlocks;
            if (stride < 0.4f) stride = 0.4f;

            strideAccum += dist;
            if (strideAccum < stride) return;
            strideAccum = 0f;

            FootTrafficService.TryApplyFromAnimal(entity);
        }

        bool IsNearAnyPlayer(Entity e, EcosystemConfig cfg)
        {
            ICoreAPI worldApi = e.Api;
            if (worldApi == null) return false;

            int radius = cfg.FootTrafficAnimalPlayerRadiusBlocks;
            if (radius <= 0) return true;

            long nowMs = worldApi.World.ElapsedMilliseconds;
            if (nowMs - nearPlayersRefreshMs >= NearPlayersRefreshIntervalMs)
            {
                NearPlayers.Refresh(worldApi, radius);
                nearPlayersRefreshMs = nowMs;
            }

            nearScratch.Set((int)e.Pos.X, (int)e.Pos.Y, (int)e.Pos.Z);
            nearScratch.dimension = e.Pos.Dimension;
            return NearPlayers.IsNearChunk(nearScratch) && NearPlayers.IsNear(nearScratch, radius);
        }
    }
}
