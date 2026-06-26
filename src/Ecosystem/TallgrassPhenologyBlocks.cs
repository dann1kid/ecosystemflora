using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class TallgrassPhenologyBlocks
    {
        const string Prefix = "tallgrassphase-";

        public static AssetLocation CodeForPhase(TallgrassPhenologyPhase phase, Block referenceBlock)
        {
            string suffix = phase switch
            {
                TallgrassPhenologyPhase.Dormant => "dormant",
                TallgrassPhenologyPhase.Dieback => "dieback",
                _ => null,
            };

            if (suffix == null) return null;

            bool snow = referenceBlock?.Code?.Path?.Contains("-snow") == true;
            bool free = referenceBlock?.Code?.Path?.Contains("-free") != false;
            string path = Prefix + suffix + (free ? "-free" : "") + (snow ? "-snow" : "");
            return new AssetLocation("ecosystemflora", path);
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
