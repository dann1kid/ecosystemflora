using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class TallgrassPhenologyBlocks
    {
        const string Prefix = "tallgrassphase-";

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

        public static AssetLocation CodeForPhase(TallgrassPhenologyPhase phase, Block referenceBlock)
        {
            bool snow = PlantSnowCover.PathHasSnowCover(referenceBlock?.Code?.Path);
            return CodeForPhase(phase, snow);
        }

        public static bool IsPhaseBlock(Block block) =>
            block?.Code?.Domain == "ecosystemflora" && block.Code.Path?.StartsWith(Prefix) == true;

        public static TallgrassPhenologyPhase? PhaseFromBlock(Block block)
        {
            string path = block?.Code?.Path;
            if (path == null) return null;
            if (path.Contains("dormant")) return TallgrassPhenologyPhase.Dormant;
            if (path.Contains("dieback")) return TallgrassPhenologyPhase.Dieback;
            return null;
        }
    }
}
