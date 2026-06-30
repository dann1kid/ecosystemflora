using System;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Full-size phenology phase blocks (vegetative / dormant / dieback) — not spread seedlings.</summary>
    internal static class FlowerPhenologyBlocks
    {
        const string Domain = "ecosystemflora";
        const string Prefix = "flowerphase-";

        public static AssetLocation CodeForPhase(string species, FlowerPhenologyPhase phase, bool snow = false)
        {
            if (string.IsNullOrEmpty(species)) return null;
            if (phase == FlowerPhenologyPhase.Bloom) return null;

            string phaseSuffix = SuffixForPhase(phase);
            if (phaseSuffix == null) return null;

            string cover = snow ? JuvenileBlockNaming.SnowSuffix : JuvenileBlockNaming.FreeSuffix;
            return new AssetLocation(Domain, Prefix + species + "-" + phaseSuffix + cover);
        }

        public static AssetLocation CodeForPhase(string species, FlowerPhenologyPhase phase, Block referenceBlock)
        {
            bool snow = PlantSnowCover.PathHasSnowCover(referenceBlock?.Code?.Path);
            return CodeForPhase(species, phase, snow);
        }

        public static string SpeciesFromPhaseBlock(Block block) => SpeciesFromPhaseCode(block?.Code);

        public static string SpeciesFromPhaseCode(AssetLocation code)
        {
            return TryParse(code, out string species, out _) ? species : null;
        }

        public static bool TryGetPhase(AssetLocation code, out FlowerPhenologyPhase phase)
        {
            return TryParse(code, out _, out phase);
        }

        public static bool IsPhaseBlock(Block block) => TryParse(block?.Code, out _, out _);

        static string SuffixForPhase(FlowerPhenologyPhase phase)
        {
            switch (phase)
            {
                case FlowerPhenologyPhase.Vegetative: return "vegetative";
                case FlowerPhenologyPhase.Dormant: return "dormant";
                case FlowerPhenologyPhase.Dieback: return "dieback";
                default: return null;
            }
        }

        static bool TryParse(AssetLocation code, out string species, out FlowerPhenologyPhase phase)
        {
            species = null;
            phase = FlowerPhenologyPhase.Vegetative;

            if (code == null || !Domain.Equals(code.Domain, StringComparison.OrdinalIgnoreCase)) return false;

            string path = code.Path ?? "";
            if (!path.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            if (!TryStripCoverSuffix(path, out string inner)) return false;
            inner = inner.Substring(Prefix.Length);
            int lastDash = inner.LastIndexOf('-');
            if (lastDash <= 0 || lastDash >= inner.Length - 1) return false;

            string phaseSuffix = inner.Substring(lastDash + 1);
            species = inner.Substring(0, lastDash);

            switch (phaseSuffix)
            {
                case "vegetative": phase = FlowerPhenologyPhase.Vegetative; break;
                case "dormant": phase = FlowerPhenologyPhase.Dormant; break;
                case "dieback": phase = FlowerPhenologyPhase.Dieback; break;
                default: return false;
            }

            return species.Length > 0;
        }

        static bool TryStripCoverSuffix(string path, out string withoutCover)
        {
            withoutCover = null;
            if (string.IsNullOrEmpty(path)) return false;

            if (path.EndsWith(JuvenileBlockNaming.FreeSuffix, StringComparison.Ordinal))
            {
                withoutCover = path.Substring(0, path.Length - JuvenileBlockNaming.FreeSuffix.Length);
                return true;
            }

            if (path.EndsWith(JuvenileBlockNaming.SnowSuffix, StringComparison.Ordinal))
            {
                withoutCover = path.Substring(0, path.Length - JuvenileBlockNaming.SnowSuffix.Length);
                return true;
            }

            return false;
        }
    }
}
