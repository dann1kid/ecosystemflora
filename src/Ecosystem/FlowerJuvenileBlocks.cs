using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks (ecosystemflora domain) for colonizer maturation.</summary>
    internal static class FlowerJuvenileBlocks
    {
        const string Domain = "ecosystemflora";
        const string Prefix = "juvenile-flower-";
        const string Suffix = "-free";

        public static AssetLocation CodeForSpecies(string species)
        {
            if (string.IsNullOrEmpty(species)) return null;
            return new AssetLocation(Domain, Prefix + species + Suffix);
        }

        public static AssetLocation MatureVanillaCode(string species)
        {
            if (string.IsNullOrEmpty(species)) return null;
            if (species == "lupine")
            {
                return new AssetLocation("game", "flower-lupine-blue-free");
            }

            return new AssetLocation("game", "flower-" + species + Suffix);
        }

        /// <summary>Lupine inherits parent color variant; other species use vanilla mature code.</summary>
        public static AssetLocation ResolveMatureCode(ICoreAPI api, BlockPos parentOrigin, string species)
        {
            if (species == "lupine" && api != null && parentOrigin != null)
            {
                Block parent = api.World.BlockAccessor.GetBlock(parentOrigin);
                if (parent?.Code != null && parent.Code.Path != null && parent.Code.Path.Contains("lupine"))
                {
                    return parent.Code;
                }
            }

            return MatureVanillaCode(species);
        }

        public static bool IsJuvenileBlock(Block block)
        {
            if (block?.Code == null) return false;
            if (!Domain.Equals(block.Code.Domain, System.StringComparison.OrdinalIgnoreCase)) return false;
            string path = block.Code.Path ?? "";
            return path.StartsWith(Prefix) && path.EndsWith(Suffix);
        }

        public static string SpeciesFromJuvenile(Block block)
        {
            if (!IsJuvenileBlock(block)) return null;
            string path = block.Code.Path;
            string inner = path.Substring(Prefix.Length, path.Length - Prefix.Length - Suffix.Length);
            return inner.Length > 0 ? inner : null;
        }
    }
}
