using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks maturing into vanilla <c>tallplant-brownsedge-*</c>.</summary>
    internal static class ShoreSedgeJuvenileBlocks
    {
        const string Prefix = "juvenile-sedge-";

        public static AssetLocation CodeForSpecies(string species)
        {
            return JuvenileBlockNaming.CodeForSpecies(Prefix, species);
        }

        public static AssetLocation MatureVanillaCode(string species)
        {
            if (species != EcologyShoreSedgeSpecies.Brownsedge) return null;
            return new AssetLocation("game", "tallplant-brownsedge-land-normal-free");
        }

        public static AssetLocation ResolveMatureCode(ICoreAPI api, BlockPos parentOrigin, string species)
        {
            if (api != null && parentOrigin != null && !string.IsNullOrEmpty(species))
            {
                Block parent = api.World.BlockAccessor.GetBlock(parentOrigin);
                if (parent?.Code != null
                    && string.Equals(
                        PlantCodeHelper.ResolveEcologySpecies(parent),
                        species,
                        System.StringComparison.OrdinalIgnoreCase)
                    && parent.Code.Path.StartsWith("tallplant-brownsedge"))
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
