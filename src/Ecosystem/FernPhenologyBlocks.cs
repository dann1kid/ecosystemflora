using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class FernPhenologyBlocks
    {
        const string Prefix = "fernphase-";

        public static AssetLocation CodeForPhase(string species, FernPhenologyPhase phase, bool snow = false)
        {
            if (string.IsNullOrEmpty(species)) return null;
            string suffix = phase switch
            {
                FernPhenologyPhase.Dormant => "dormant",
                FernPhenologyPhase.Dieback => "dieback",
                _ => null,
            };

            if (suffix == null)
            {
                return PlantSnowCover.CodeWithCover(
                    FernJuvenileBlocks.MatureVanillaCode(species),
                    snow);
            }

            // Legacy fern phase codes omit the -free suffix (saves pre cover-variant migration).
            string path = Prefix + species + "-" + suffix;
            if (snow) path += JuvenileBlockNaming.SnowSuffix;
            return new AssetLocation("ecosystemflora", path);
        }

        public static AssetLocation CodeForPhase(string species, FernPhenologyPhase phase, Block referenceBlock)
        {
            bool snow = PlantSnowCover.PathHasSnowCover(referenceBlock?.Code?.Path);
            return CodeForPhase(species, phase, snow);
        }

        public static bool IsPhaseBlock(Block block)
        {
            return SpeciesFromPhaseCode(block?.Code) != null;
        }

        public static string SpeciesFromPhaseCode(AssetLocation code)
        {
            if (code?.Domain != "ecosystemflora" || code.Path == null) return null;
            if (!code.Path.StartsWith(Prefix)) return null;

            if (!TryParsePhasePath(code.Path, out string species, out _)) return null;
            return EcologyFernSpecies.IsKnown(species) ? species : null;
        }

        public static FernPhenologyPhase? PhaseFromCode(AssetLocation code)
        {
            if (!TryParsePhasePath(code?.Path, out _, out FernPhenologyPhase? phase)) return null;
            return phase;
        }

        static bool TryParsePhasePath(string path, out string species, out FernPhenologyPhase? phase)
        {
            species = null;
            phase = null;
            if (string.IsNullOrEmpty(path) || !path.StartsWith(Prefix)) return false;

            string rest = path.Substring(Prefix.Length);
            if (rest.EndsWith(JuvenileBlockNaming.FreeSuffix))
            {
                rest = rest.Substring(0, rest.Length - JuvenileBlockNaming.FreeSuffix.Length);
            }
            else if (rest.EndsWith(JuvenileBlockNaming.SnowSuffix))
            {
                rest = rest.Substring(0, rest.Length - JuvenileBlockNaming.SnowSuffix.Length);
            }

            int dash = rest.LastIndexOf('-');
            if (dash <= 0) return false;

            species = rest.Substring(0, dash);
            string phaseSuffix = rest.Substring(dash + 1);
            switch (phaseSuffix)
            {
                case "dormant": phase = FernPhenologyPhase.Dormant; return true;
                case "dieback": phase = FernPhenologyPhase.Dieback; return true;
                default: return false;
            }
        }
    }
}
