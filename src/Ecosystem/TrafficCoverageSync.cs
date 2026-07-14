using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Keep wild soil grass coverage aligned with column traffic pressure:
    /// compact when pressure rises, restore when it falls.
    /// </summary>
    internal static class TrafficCoverageSync
    {
        const int ColumnScanExtraUp = 2;
        const int ColumnScanDown = 8;
        const int MaxSyncSteps = 4;

        static readonly BlockPos ResolveScratch = new BlockPos(0);
        static readonly BlockPos ResolveScratch2 = new BlockPos(0);

        /// <summary>
        /// Sync soil under a surface/foot cell to the coverage implied by <paramref name="pressure"/>.
        /// Returns true when coverage is aligned (already matched or successfully updated).
        /// </summary>
        public static bool SyncAtSurface(ICoreAPI api, BlockPos surfacePos, byte pressure, byte wearStep)
        {
            if (api == null || surfacePos == null) return false;
            if (!EcosystemConfig.Loaded.TramplingSoilDegradation) return false;
            if (!TryResolveSoilGround(api, surfacePos, out BlockPos groundPos)) return false;
            return SyncGround(api, groundPos, pressure, wearStep);
        }

        /// <summary>XZ-only sync using rain map height (world save age/prune).</summary>
        public static bool SyncColumnXZ(ICoreAPI api, int x, int z, int dimension, byte pressure, byte wearStep)
        {
            if (api?.World?.BlockAccessor == null) return false;
            if (!EcosystemConfig.Loaded.TramplingSoilDegradation) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            int rainY = acc.GetRainMapHeightAt(x, z);
            ResolveScratch.Set(x, rainY, z);
            ResolveScratch.dimension = dimension;
            if (!TryResolveSoilGround(api, ResolveScratch, out BlockPos groundPos)) return false;
            return SyncGround(api, groundPos, pressure, wearStep);
        }

        /// <summary>
        /// True when the soil block's traffic wear index matches pressure (or was brought into match).
        /// False when soil cannot be resolved / claims block the change.
        /// </summary>
        static bool SyncGround(ICoreAPI api, BlockPos groundPos, byte pressure, byte wearStep)
        {
            IBlockAccessor acc = api.World.BlockAccessor;
            if (!LandClaimGuard.AllowsEcologyChange(api, groundPos)) return false;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.MyceliumSkipSoilSuccession
                && WildSoilGroundRules.HasActiveMycelium(acc, groundPos))
            {
                return false;
            }

            if (cfg.SoilSuccessionSkipWhenBuiltAbove
                && !SoilSuccessionGuard.CanModifyGroundBelow(acc, groundPos))
            {
                return false;
            }

            int target = FootTrafficWear.TargetWearIndex(pressure, wearStep);
            Block ground = acc.GetBlock(groundPos);
            int actual = SoilTrafficCoverage.GetTrafficWearIndex(ground);
            if (actual == target) return true;

            bool changed = false;

            for (int i = 0; i < MaxSyncSteps; i++)
            {
                ground = acc.GetBlock(groundPos);
                actual = SoilTrafficCoverage.GetTrafficWearIndex(ground);
                if (actual == target) break;

                if (actual < target)
                {
                    if (!SoilTrafficCoverage.TryCompactOneStep(api, acc, groundPos)) break;
                    changed = true;
                    continue;
                }

                if (!SoilTrafficCoverage.TryRestoreOneStep(api, acc, groundPos)) break;
                changed = true;
            }

            if (changed)
            {
                EcosystemSystem.Instance?.InvalidateEnvironmentAround(groundPos);
            }

            actual = SoilTrafficCoverage.GetTrafficWearIndex(acc.GetBlock(groundPos));
            return actual == target;
        }

        /// <summary>
        /// Feet cell, one below, or rain-map column scan for <c>soil-*-*</c>.
        /// </summary>
        public static bool TryResolveSoilGround(ICoreAPI api, BlockPos surfaceHint, out BlockPos groundPos)
        {
            groundPos = null;
            if (api?.World?.BlockAccessor == null || surfaceHint == null) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            ResolveScratch2.Set(surfaceHint.X, surfaceHint.Y, surfaceHint.Z);
            ResolveScratch2.dimension = surfaceHint.dimension;

            if (IsTrafficSoil(acc.GetBlock(ResolveScratch2)))
            {
                groundPos = ResolveScratch2.Copy();
                return true;
            }

            ResolveScratch2.Y = surfaceHint.Y - 1;
            if (acc.IsValidPos(ResolveScratch2) && IsTrafficSoil(acc.GetBlock(ResolveScratch2)))
            {
                groundPos = ResolveScratch2.Copy();
                return true;
            }

            int rainY = acc.GetRainMapHeightAt(surfaceHint.X, surfaceHint.Z);
            int yMax = Math.Max(surfaceHint.Y, rainY) + ColumnScanExtraUp;
            int yMin = Math.Max(0, Math.Min(surfaceHint.Y, rainY) - ColumnScanDown);

            for (int y = yMax; y >= yMin; y--)
            {
                ResolveScratch2.Set(surfaceHint.X, y, surfaceHint.Z);
                ResolveScratch2.dimension = surfaceHint.dimension;
                if (!acc.IsValidPos(ResolveScratch2)) continue;
                if (!IsTrafficSoil(acc.GetBlock(ResolveScratch2))) continue;

                groundPos = ResolveScratch2.Copy();
                return true;
            }

            return false;
        }

        static bool IsTrafficSoil(Block ground)
        {
            if (!WildSoilBlockMapper.IsSuccessionTarget(ground)) return false;
            string path = ground?.Code?.Path;
            return path != null
                && path.StartsWith("soil-", StringComparison.Ordinal)
                && !WildSoilGroundRules.IsFarmland(ground);
        }
    }
}
