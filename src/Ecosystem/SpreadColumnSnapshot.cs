using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Cached spread inputs for one plant cell (Phase 6.4).</summary>
    internal readonly struct SpreadColumnSnapshot
    {
        public readonly CellBlockSnapshot BlockSnap;
        public readonly float WorldgenRainfall;
        public readonly bool HasClimate;
        public readonly float LocalForestCover;

        public SpreadColumnSnapshot(
            CellBlockSnapshot blockSnap,
            float worldgenRainfall,
            bool hasClimate,
            float localForestCover)
        {
            BlockSnap = blockSnap;
            WorldgenRainfall = worldgenRainfall;
            HasClimate = hasClimate;
            LocalForestCover = localForestCover;
        }
    }
}
