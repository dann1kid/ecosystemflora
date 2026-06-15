using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Client
{
    /// <summary>Textured leaf voxel — calm flutter-fall or wind-driven glide.</summary>
    internal sealed class CanopyLeafVoxelParticleProps : BlockBreakingParticleProps
    {
        readonly Vec3d spawnPos = new Vec3d();
        readonly Vec3f velocityScratch = new Vec3f();
        CanopyAmbienceWind wind;
        float swaySign = 1f;
        float patternScale = 1f;
        float flutterFreqX;
        float flutterFreqZ;
        float flutterFreqY;

        public void Configure(
            ICoreClientAPI capi,
            Block leafBlock,
            BlockPos colorPos,
            Vec3d pos,
            CanopyAmbienceWind sampleWind,
            Random rand)
        {
            wind = sampleWind;
            swaySign = rand.NextDouble() > 0.5 ? 1f : -1f;
            patternScale = 0.75f + (float)rand.NextDouble() * 0.5f;
            // Incommensurate frequencies — back-and-forth glide, not horizontal orbits.
            flutterFreqX = (1.85f + (float)rand.NextDouble() * 1.25f) * patternScale;
            flutterFreqZ = (2.65f + (float)rand.NextDouble() * 1.75f) * patternScale;
            flutterFreqY = (3.35f + (float)rand.NextDouble() * 1.15f) * patternScale;
            spawnPos.Set(pos.X, pos.Y, pos.Z);

            blockdamage = new BlockDamage
            {
                Block = leafBlock,
                Position = colorPos,
                Facing = BlockFacing.UP,
            };

            boyant = true;
            RandomVelocityChange = !wind.IsCalm;
            DieOnRainHeightmap = true;

            ParentVelocity = GlobalConstants.CurrentWindSpeedClient ?? new Vec3f();
            ParentVelocityWeight = wind.IsCalm ? 0f : wind.Strength * 1.15f;

            Init(capi);
        }

        public override Vec3d Pos
        {
            get
            {
                return new Vec3d(
                    spawnPos.X + (rand.NextDouble() - 0.5) * 0.2,
                    spawnPos.Y + (rand.NextDouble() - 0.5) * 0.12,
                    spawnPos.Z + (rand.NextDouble() - 0.5) * 0.2);
            }
        }

        public override float Quantity => 1f;

        public override float Size => 1.5f + (float)rand.NextDouble() * 1.5f;

        public override EvolvingNatFloat SizeEvolve => EvolvingNatFloat.NoValueSet;

        public override float LifeLength
        {
            get
            {
                if (wind.IsCalm) return 9f + (float)rand.NextDouble() * 4f;
                return 12f + wind.Strength * 8f + (float)rand.NextDouble() * 4f;
            }
        }

        public override float GravityEffect
        {
            get
            {
                if (wind.IsCalm) return 0.66f + (float)rand.NextDouble() * 0.15f;
                return 0.33f + (1f - wind.Strength) * 0.15f;
            }
        }

        public override bool TerrainCollision => false;

        public override bool SelfPropelled => false;

        public override EvolvingNatFloat[] VelocityEvolve
        {
            get
            {
                float yGlide = wind.IsCalm ? 0.9f : 0.78f + wind.Strength * 0.14f;
                return new EvolvingNatFloat[]
                {
                    new EvolvingNatFloat(EnumTransformFunction.SINUS, flutterFreqX),
                    new EvolvingNatFloat(EnumTransformFunction.CLAMPEDPOSITIVESINUS, flutterFreqY * yGlide),
                    new EvolvingNatFloat(EnumTransformFunction.SINUS, flutterFreqZ),
                };
            }
        }

        public override Vec3f GetVelocity(Vec3d pos)
        {
            if (wind.IsCalm)
            {
                float drift = 0.004f + (float)rand.NextDouble() * 0.005f;
                float fall = 0.255f + (float)rand.NextDouble() * 0.105f;
                velocityScratch.Set(
                    swaySign * drift,
                    -fall,
                    swaySign * drift * 0.35f);
                return velocityScratch;
            }

            float gust = 0.35f + wind.Strength * 0.9f;
            float fallWind = 0.165f + (float)rand.NextDouble() * 0.075f;
            float flutter = (0.012f + (float)rand.NextDouble() * 0.018f) * (0.5f + wind.Strength);

            velocityScratch.Set(
                wind.HorizontalDir.X * gust + swaySign * flutter,
                -fallWind + wind.Raw.Y * 0.2f,
                wind.HorizontalDir.Z * gust + swaySign * flutter * 0.7f);

            return velocityScratch;
        }
    }
}
