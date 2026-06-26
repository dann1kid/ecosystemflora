using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks (ecosystemflora) maturing into vanilla <c>fern-*</c> / <c>tallfern</c>.</summary>
    internal static class FernJuvenileBlocks
    {
        const string Prefix = "juvenile-fern-";

        public static AssetLocation CodeForSpecies(string species)
        {
            return JuvenileBlockNaming.CodeForSpecies(Prefix, species);
        }

        public static AssetLocation MatureVanillaCode(string species)
        {
            if (string.IsNullOrEmpty(species)) return null;
            if (species == "tallfern")
            {
                return new AssetLocation("game", "tallfern");
            }

            return new AssetLocation("game", "fern-" + species);
        }

        public static AssetLocation ResolveMatureCode(ICoreAPI api, BlockPos parentOrigin, string species)
        {
            if (api != null && parentOrigin != null && !string.IsNullOrEmpty(species))
            {
                Block parent = api.World.BlockAccessor.GetBlock(parentOrigin);
                if (parent?.Code != null && PlantCodeHelper.SameEcologySpecies(parent.Code, MatureVanillaCode(species)))
                {
                    return parent.Code;
                }
            }

            return MatureVanillaCode(species);
        }

        public static bool IsJuvenileBlock(Block block)
        {
            return SpeciesFromJuvenileCode(block?.Code) != null;
        }

        public static bool MatchesJuvenileBlock(Block block, PlantRequirements requirements)
        {
            if (block == null || requirements == null) return false;
            string species = SpeciesFromJuvenile(block);
            return species != null
                && string.Equals(species, requirements.Species, System.StringComparison.OrdinalIgnoreCase);
        }

        public static string SpeciesFromJuvenileCode(AssetLocation code)
        {
            return JuvenileBlockNaming.SpeciesFromCode(Prefix, code);
        }

        public static string SpeciesFromJuvenile(Block block)
        {
            return SpeciesFromJuvenileCode(block?.Code);
        }
    }
}
