using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class FernPhenologyBlocks
    {
        const string Prefix = "fernphase-";

        public static AssetLocation CodeForPhase(string species, FernPhenologyPhase phase)
        {
            if (string.IsNullOrEmpty(species)) return null;
            string suffix = phase switch
            {
                FernPhenologyPhase.Dormant => "dormant",
                FernPhenologyPhase.Dieback => "dieback",
                _ => null,
            };

            if (suffix == null) return FernJuvenileBlocks.MatureVanillaCode(species);
            return new AssetLocation("ecosystemflora", Prefix + species + "-" + suffix);
        }

        public static bool IsPhaseBlock(Block block)
        {
            return SpeciesFromPhaseCode(block?.Code) != null;
        }

        public static string SpeciesFromPhaseCode(AssetLocation code)
        {
            if (code?.Domain != "ecosystemflora" || code.Path == null) return null;
            if (!code.Path.StartsWith(Prefix)) return null;

            string rest = code.Path.Substring(Prefix.Length);
            int dash = rest.LastIndexOf('-');
            if (dash <= 0) return null;

            string species = rest.Substring(0, dash);
            return EcologyFernSpecies.IsKnown(species) ? species : null;
        }

        public static FernPhenologyPhase? PhaseFromCode(AssetLocation code)
        {
            if (code?.Path == null) return null;
            if (code.Path.EndsWith("-dormant")) return FernPhenologyPhase.Dormant;
            if (code.Path.EndsWith("-dieback")) return FernPhenologyPhase.Dieback;
            return null;
        }
    }
}
