using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Resolves free vs snow plant cover variants (vanilla <c>cover</c> variant group).</summary>
    internal static class PlantSnowCover
    {
        /// <summary>Match vanilla frostable ground-cover swap (approx. freezing).</summary>
        internal const float FrostCoverTemperatureC = 0f;

        internal const float SnowAccumCoverThreshold = 0.02f;

        public static bool PathHasSnowCover(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith("-snow", System.StringComparison.Ordinal);
        }

        public static bool BlockHasCoverVariant(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal)
                || path.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal))
            {
                return true;
            }

            return IsLegacyFernPhasePath(path);
        }

        /// <summary>Snow layer block directly above the plant cell.</summary>
        public static bool EnvironmentWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;

            BlockPos above = plantPos.UpCopy();
            IBlockAccessor acc = api.World.BlockAccessor;
            return acc.IsValidPos(above) && IsSnowLayerBlock(acc.GetBlock(above));
        }

        /// <summary>
        /// Whether a frostable plant should use its snow cover variant now (climate + snow layer + accum map).
        /// </summary>
        public static bool ResolveWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (EnvironmentWantsSnowCover(api, plantPos)) return true;
            return ClimateWantsSnowCover(api, plantPos);
        }

        public static bool ClimateWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;

            if (api.ModLoader != null && GreenhouseHelper.IsGreenhouse(api, plantPos)) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            ClimateCondition now = acc.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            if (now != null && now.Temperature <= FrostCoverTemperatureC) return true;

            return TryGetSnowAccum(acc, plantPos, out float accum)
                && accum >= SnowAccumCoverThreshold;
        }

        /// <summary>
        /// Spread / phase placement: inherit parent cover, then snow layer, then climate at target cell.
        /// </summary>
        public static bool ShouldUseSnowVariant(ICoreAPI api, BlockPos parentOrigin, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null) return false;

            IBlockAccessor acc = api.World.BlockAccessor;

            if (parentOrigin != null)
            {
                Block parent = acc.GetBlock(parentOrigin);
                if (PathHasSnowCover(parent?.Code?.Path)) return true;
            }

            BlockPos target = plantPos ?? parentOrigin;
            if (target != null)
            {
                return ResolveWantsSnowCover(api, target);
            }

            return false;
        }

        /// <summary>Swap <c>cover</c> variant suffix on block codes that use <c>-free</c> / <c>-snow</c>.</summary>
        public static AssetLocation CodeWithCover(AssetLocation code, bool snow)
        {
            if (code == null) return null;
            string path = code.Path;
            if (string.IsNullOrEmpty(path)) return code;

            if (snow)
            {
                if (path.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal))
                {
                    return code;
                }

                if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal))
                {
                    return new AssetLocation(
                        code.Domain,
                        path.Substring(0, path.Length - JuvenileBlockNaming.FreeSuffix.Length)
                            + JuvenileBlockNaming.SnowSuffix);
                }

                if (IsLegacyFernPhasePath(path))
                {
                    return new AssetLocation(code.Domain, path + JuvenileBlockNaming.SnowSuffix);
                }

                return code;
            }

            if (path.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal))
            {
                string bare = path.Substring(0, path.Length - JuvenileBlockNaming.SnowSuffix.Length);
                if (IsLegacyFernPhasePath(bare))
                {
                    return new AssetLocation(code.Domain, bare);
                }

                return new AssetLocation(
                    code.Domain,
                    bare + JuvenileBlockNaming.FreeSuffix);
            }

            return code;
        }

        /// <summary>Fern phase blocks use bare <c>-dormant</c>/<c>-dieback</c> for free cover (no <c>-free</c> suffix).</summary>
        internal static bool IsLegacyFernPhasePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("fernphase-", System.StringComparison.Ordinal))
            {
                return false;
            }

            return path.EndsWith("-dormant", System.StringComparison.Ordinal)
                || path.EndsWith("-dieback", System.StringComparison.Ordinal);
        }

        internal static bool TryGetSnowAccum(IBlockAccessor acc, BlockPos pos, out float snowAccum)
        {
            snowAccum = 0f;
            if (acc == null || pos == null) return false;

            IMapChunk chunk = acc.GetMapChunkAtBlockPos(pos);
            float[] map = chunk?.SnowAccum;
            if (map == null || map.Length == 0) return false;

            int cs = GlobalConstants.ChunkSize;
            int lx = pos.X % cs;
            int lz = pos.Z % cs;
            if (lx < 0) lx += cs;
            if (lz < 0) lz += cs;
            int idx = lz * cs + lx;
            if (idx < 0 || idx >= map.Length) return false;

            snowAccum = map[idx];
            return true;
        }

        static bool IsSnowLayerBlock(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (block.BlockMaterial == EnumBlockMaterial.Snow) return true;

            string path = block.Code?.Path;
            return !string.IsNullOrEmpty(path) && path.StartsWith("snowlayer");
        }
    }
}
