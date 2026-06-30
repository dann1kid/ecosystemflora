using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Shore sedge dormant/dieback blocks (single species: brownsedge).</summary>
    internal static class SedgePhenologyBlocks
    {
        const string Prefix = "sedgephase-";

        public static AssetLocation CodeForPhase(TallgrassPhenologyPhase phase, bool snow = false)
        {
            string suffix = phase switch
            {
                TallgrassPhenologyPhase.Dormant => "dormant",
                TallgrassPhenologyPhase.Dieback => "dieback",
                _ => null,
            };

            if (suffix == null) return null;

            string cover = snow ? JuvenileBlockNaming.SnowSuffix : JuvenileBlockNaming.FreeSuffix;
            return new AssetLocation("ecosystemflora", Prefix + suffix + cover);
        }

        public static bool IsPhaseBlock(Block block) =>
            block?.Code?.Domain == "ecosystemflora" && block.Code.Path?.StartsWith(Prefix) == true;

        public static bool TryGetPhase(Block block, out TallgrassPhenologyPhase phase)
        {
            phase = TallgrassPhenologyPhase.Active;
            if (!IsPhaseBlock(block)) return false;

            string path = block.Code.Path;
            if (path.Contains("dormant"))
            {
                phase = TallgrassPhenologyPhase.Dormant;
                return true;
            }

            if (path.Contains("dieback"))
            {
                phase = TallgrassPhenologyPhase.Dieback;
                return true;
            }

            return false;
        }

        public static bool IsSyncableMatureBlock(Block block)
        {
            string path = block?.Code?.Path;
            if (path == null || !path.StartsWith("tallplant-brownsedge")) return false;
            return !path.Contains("-harvested-");
        }
    }
}
