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
            if (string.IsNullOrEmpty(path)) return false;
            return path.EndsWith("-snow", System.StringComparison.Ordinal)
                || path.EndsWith("-snow2", System.StringComparison.Ordinal)
                || path.EndsWith("-snow3", System.StringComparison.Ordinal);
        }

        public static bool BlockHasCoverVariant(AssetLocation code) =>
            code != null && BlockHasCoverVariant(code.Domain, code.Path);

        /// <summary>Path-only helper for tests; assumes <c>ecosystemflora</c> domain.</summary>
        public static bool BlockHasCoverVariant(string path) =>
            BlockHasCoverVariant("ecosystemflora", path);

        /// <summary>
        /// Mod ecology phase blocks only — not vanilla <c>cover</c> variants (fences, stairs, slabs).
        /// </summary>
        public static bool BlockHasCoverVariant(string domain, string path)
        {
            if (string.IsNullOrEmpty(path) || domain != "ecosystemflora") return false;
            if (!IsManagedEcologyPhasePath(path)) return false;

            if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal)
                || PathHasSnowCover(path))
            {
                return true;
            }

            return IsLegacyBareFernPhasePath(path);
        }

        public static bool ShouldSyncCoverVariant(AssetLocation code) =>
            BlockHasCoverVariant(code);

        /// <summary>
        /// Legacy helper kept for tests; vanilla tallgrass cover is engine-owned after the radical phenology change.
        /// </summary>
        public static bool IsVanillaTallgrassCoverBlock(AssetLocation code) =>
            code != null && IsVanillaTallgrassCoverPath(code.Domain, code.Path);

        public static bool IsVanillaTallgrassCoverPath(string domain, string path)
        {
            if (string.IsNullOrEmpty(path) || domain != "game") return false;
            if (!path.StartsWith("tallgrass-", System.StringComparison.Ordinal)) return false;
            if (path.StartsWith("tallgrass-eaten", System.StringComparison.Ordinal)) return false;

            return path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal)
                || PathHasSnowCover(path);
        }

        static bool IsManagedEcologyPhasePath(string path)
        {
            return path.StartsWith("flowerphase-", System.StringComparison.Ordinal)
                || path.StartsWith("fernphase-", System.StringComparison.Ordinal)
                || path.StartsWith("tallgrassphase-", System.StringComparison.Ordinal)
                || path.StartsWith("sedgephase-", System.StringComparison.Ordinal)
                || path.StartsWith("juvenile-flower-", System.StringComparison.Ordinal)
                || path.StartsWith("juvenile-fern-", System.StringComparison.Ordinal)
                || path.StartsWith("juvenile-sedge-", System.StringComparison.Ordinal);
        }

        /// <summary>Open to rain/snow at this cell (rain heightmap).</summary>
        public static bool IsExposedToSky(IBlockAccessor acc, BlockPos pos)
        {
            if (acc == null || pos == null) return false;
            int rainY = acc.GetRainMapHeightAt(pos.X, pos.Z);
            // Rain map height is the first Y that blocks precipitation in this column.
            // Plants at the exposed surface commonly sit exactly at this height, so use >=.
            return pos.Y >= rainY;
        }

        /// <summary>Bare <c>fernphase-*-{dormant|dieback|sporulating}</c> without cover suffix (pre-unification saves).</summary>
        internal static bool IsLegacyBareFernPhasePath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("fernphase-", System.StringComparison.Ordinal))
            {
                return false;
            }

            if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal)
                || path.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal))
            {
                return false;
            }

            return path.EndsWith("-dormant", System.StringComparison.Ordinal)
                || path.EndsWith("-dieback", System.StringComparison.Ordinal)
                || path.EndsWith("-sporulating", System.StringComparison.Ordinal);
        }

        [System.Obsolete("Use IsLegacyBareFernPhasePath")]
        internal static bool IsLegacyFernPhasePath(string path) => IsLegacyBareFernPhasePath(path);

        /// <summary>Snow layer block directly above the plant cell.</summary>
        public static bool EnvironmentWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;

            BlockPos above = plantPos.UpCopy();
            IBlockAccessor acc = api.World.BlockAccessor;
            return acc.IsValidPos(above) && IsSnowLayerBlock(acc.GetBlock(above));
        }

        /// <summary>
        /// Whether a frostable plant should use its snow cover variant now.
        /// Requires frost at the cell and visible snow (layer above or ground accum map).
        /// </summary>
        public static bool ResolveWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;
            IBlockAccessor acc = api.World.BlockAccessor;
            if (BlockFluidHelper.ExcludesSnowCover(acc, plantPos)) return false;
            if (!IsExposedToSky(acc, plantPos)) return false;
            if (!ClimateWantsSnowCover(api, plantPos)) return false;
            return SurroundingsHaveSnow(api, plantPos);
        }

        /// <summary>Snow layer above the plant cell or chunk snow-accum at this column.</summary>
        public static bool SurroundingsHaveSnow(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;
            if (EnvironmentWantsSnowCover(api, plantPos)) return true;

            return TryGetSnowAccum(api.World.BlockAccessor, plantPos, out float accum)
                && accum >= SnowAccumCoverThreshold;
        }

        /// <summary>Frost at the plant cell only; residual snow-accum map does not keep cover above freezing.</summary>
        public static bool ClimateWantsSnowCover(ICoreAPI api, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null || plantPos == null) return false;

            if (api.ModLoader != null && GreenhouseHelper.IsGreenhouse(api, plantPos)) return false;

            ClimateCondition now = api.World.BlockAccessor.GetClimateAt(plantPos, EnumGetClimateMode.NowValues);
            return now != null && now.Temperature <= FrostCoverTemperatureC;
        }

        /// <summary>
        /// Spread / phase placement: inherit parent cover, then snow layer, then climate at target cell.
        /// </summary>
        public static bool ShouldUseSnowVariant(ICoreAPI api, BlockPos parentOrigin, BlockPos plantPos)
        {
            if (api?.World?.BlockAccessor == null) return false;

            BlockPos target = plantPos ?? parentOrigin;
            if (target == null || !ResolveWantsSnowCover(api, target)) return false;

            IBlockAccessor acc = api.World.BlockAccessor;

            if (parentOrigin != null)
            {
                Block parent = acc.GetBlock(parentOrigin);
                if (PathHasSnowCover(parent?.Code?.Path)) return true;
            }

            return true;
        }

        /// <summary>Swap <c>cover</c> variant suffix on block codes that use <c>-free</c> / <c>-snow</c>.</summary>
        public static AssetLocation CodeWithCover(AssetLocation code, bool snow)
        {
            if (code == null) return null;
            string path = code.Path;
            if (string.IsNullOrEmpty(path)) return code;

            if (snow)
            {
                if (PathHasSnowCover(path))
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

                if (IsLegacyBareFernPhasePath(path))
                {
                    return new AssetLocation(code.Domain, path + JuvenileBlockNaming.SnowSuffix);
                }

                return code;
            }

            if (PathHasSnowCover(path))
            {
                string bare = path;
                if (bare.EndsWith("-snow3", System.StringComparison.Ordinal)) bare = bare.Substring(0, bare.Length - "-snow3".Length);
                else if (bare.EndsWith("-snow2", System.StringComparison.Ordinal)) bare = bare.Substring(0, bare.Length - "-snow2".Length);
                else if (bare.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal)) bare = bare.Substring(0, bare.Length - JuvenileBlockNaming.SnowSuffix.Length);

                if (IsLegacyBareFernPhasePath(bare))
                {
                    return new AssetLocation(code.Domain, bare + JuvenileBlockNaming.FreeSuffix);
                }

                return new AssetLocation(
                    code.Domain,
                    bare + JuvenileBlockNaming.FreeSuffix);
            }

            return code;
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
