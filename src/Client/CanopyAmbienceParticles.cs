using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;

namespace WildFarming.Client
{
    internal static class CanopyAmbienceParticles
    {
        public static bool TrySpawnLeafDrift(
            ICoreClientAPI capi,
            in FoliageSpawnPoint spawn,
            CanopyAmbienceWind wind,
            Random rand,
            out IParticlePropertiesProvider props)
        {
            props = null;
            if (capi?.World == null || spawn.Block == null || string.IsNullOrEmpty(spawn.Wood)) return false;

            BlockPos colorPos = spawn.Block;
            Block leafBlock = CanopyBlockHelper.ResolveGrownLeafBlock(
                capi.World,
                spawn.Wood,
                colorPos,
                colorPos,
                null);

            if (leafBlock == null || leafBlock.Id == 0) return false;

            var leafProps = new CanopyLeafVoxelParticleProps();
            leafProps.Configure(capi, leafBlock, colorPos, spawn.Pos, wind, rand);
            props = leafProps;
            return true;
        }

        public static SimpleParticleProperties CreateMote(Vec3d pos, int color, CanopyAmbienceWind wind)
        {
            var props = new SimpleParticleProperties(
                1, 1, color,
                pos, pos.Add(0.4, 0.2, 0.4),
                new Vec3f(-0.012f, -0.018f, -0.012f),
                new Vec3f(0.012f, -0.006f, 0.012f),
                lifeLength: 5f,
                gravityEffect: 0.035f,
                minSize: 0.14f,
                maxSize: 0.22f,
                model: EnumParticleModel.Quad);

            props.Async = true;
            props.RandomVelocityChange = !wind.IsCalm;
            props.SelfPropelled = false;
            props.WindAffectednes = wind.IsCalm ? 0f : wind.Strength * 0.65f;
            props.WithTerrainCollision = false;
            props.DieOnRainHeightmap = true;
            return props;
        }

        public static SimpleParticleProperties CreateDriftFallback(Vec3d pos, int color, CanopyAmbienceWind wind, Random rand)
        {
            float gust = wind.IsCalm ? 0f : 0.25f + wind.Strength * 0.55f;
            float tumble = (float)(rand.NextDouble() - 0.5) * 0.04f * (0.4f + wind.Strength);
            float fall = wind.IsCalm ? 0.255f : 0.165f;

            var props = new SimpleParticleProperties(
                1, 1, color,
                pos, pos.Add(0.25, 0.15, 0.25),
                new Vec3f(wind.HorizontalDir.X * gust + tumble - 0.008f, -fall - 0.045f, wind.HorizontalDir.Z * gust - tumble - 0.008f),
                new Vec3f(wind.HorizontalDir.X * gust + tumble + 0.008f, -fall - 0.015f, wind.HorizontalDir.Z * gust - tumble + 0.008f),
                lifeLength: wind.IsCalm ? 10f : 14f + wind.Strength * 6f,
                gravityEffect: wind.IsCalm ? 0.66f : 0.36f,
                minSize: 0.38f,
                maxSize: 0.52f,
                model: EnumParticleModel.Quad);

            props.Async = true;
            props.RandomVelocityChange = !wind.IsCalm;
            props.SelfPropelled = false;
            props.WindAffectednes = wind.IsCalm ? 0f : wind.Strength * 0.9f;
            props.WithTerrainCollision = false;
            props.DieOnRainHeightmap = true;
            return props;
        }
    }
}
