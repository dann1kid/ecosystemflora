using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    internal static class FernPhenologyBlocks
    {
        const string Domain = "ecosystemflora";
        const string Prefix = "fernphase-";

        public static AssetLocation CodeForPhase(string species, FernPhenologyPhase phase, bool snow = false)
        {
            if (string.IsNullOrEmpty(species)) return null;

            string phaseSuffix = SuffixForPhase(phase);
            if (phaseSuffix == null) return null;

            string cover = snow ? JuvenileBlockNaming.SnowSuffix : JuvenileBlockNaming.FreeSuffix;
            return new AssetLocation(Domain, Prefix + species + "-" + phaseSuffix + cover);
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
            return TryParse(code, out string species, out _) ? species : null;
        }

        public static FernPhenologyPhase? PhaseFromCode(AssetLocation code)
        {
            if (!TryParse(code, out _, out FernPhenologyPhase? phase)) return null;
            return phase;
        }

        static string SuffixForPhase(FernPhenologyPhase phase)
        {
            switch (phase)
            {
                case FernPhenologyPhase.Dormant: return "dormant";
                case FernPhenologyPhase.Dieback: return "dieback";
                case FernPhenologyPhase.Sporulating: return "sporulating";
                default: return null;
            }
        }

        static bool TryParse(AssetLocation code, out string species, out FernPhenologyPhase? phase)
        {
            species = null;
            phase = null;

            if (code == null || !Domain.Equals(code.Domain, System.StringComparison.OrdinalIgnoreCase)) return false;

            string path = code.Path ?? "";
            if (!path.StartsWith(Prefix, System.StringComparison.Ordinal)) return false;
            if (!TryStripCoverSuffix(path, out string inner)) return false;

            inner = inner.Substring(Prefix.Length);
            int dash = inner.LastIndexOf('-');
            if (dash <= 0) return false;

            species = inner.Substring(0, dash);
            string phaseSuffix = inner.Substring(dash + 1);

            switch (phaseSuffix)
            {
                case "dormant": phase = FernPhenologyPhase.Dormant; return EcologyFernSpecies.IsKnown(species);
                case "dieback": phase = FernPhenologyPhase.Dieback; return EcologyFernSpecies.IsKnown(species);
                case "sporulating": phase = FernPhenologyPhase.Sporulating; return EcologyFernSpecies.IsKnown(species);
                default: return false;
            }
        }

        static bool TryStripCoverSuffix(string path, out string withoutCover)
        {
            withoutCover = null;
            if (string.IsNullOrEmpty(path)) return false;

            if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal))
            {
                withoutCover = path.Substring(0, path.Length - JuvenileBlockNaming.FreeSuffix.Length);
                return true;
            }

            if (path.EndsWith(JuvenileBlockNaming.SnowSuffix, System.StringComparison.Ordinal))
            {
                withoutCover = path.Substring(0, path.Length - JuvenileBlockNaming.SnowSuffix.Length);
                return true;
            }

            if (PlantSnowCover.IsLegacyBareFernPhasePath(path))
            {
                withoutCover = path;
                return true;
            }

            return false;
        }
    }
}
