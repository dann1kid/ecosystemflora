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

            if (species == "rafflesiared")
            {
                return new AssetLocation("game", "flower-rafflesia-red-free");
            }

            if (species == "rafflesiabrown")
            {
                return new AssetLocation("game", "flower-rafflesia-brown-free");
            }

            if (species == "croton")
            {
                return new AssetLocation("game", "flower-croton-small-crimson-green-free");
            }

            return new AssetLocation("game", "flower-" + species + Suffix);
        }

        /// <summary>Lupine, croton, and rafflesia inherit parent variant when spread origin is known.</summary>
        public static AssetLocation ResolveMatureCode(ICoreAPI api, BlockPos parentOrigin, string species)
        {
            if (api != null && parentOrigin != null && !string.IsNullOrEmpty(species))
            {
                Block parent = api.World.BlockAccessor.GetBlock(parentOrigin);
                string path = parent?.Code?.Path;
                if (!string.IsNullOrEmpty(path))
                {
                    if (species == "lupine" && path.Contains("lupine"))
                    {
                        return parent.Code;
                    }

                    if (species == "croton" && path.Contains("croton"))
                    {
                        return parent.Code;
                    }

                    if (species == "rafflesiared" && path.Contains("rafflesia-red"))
                    {
                        return parent.Code;
                    }

                    if (species == "rafflesiabrown" && path.Contains("rafflesia-brown"))
                    {
                        return parent.Code;
                    }
                }
            }

            return MatureVanillaCode(species);
        }

        public static bool IsJuvenileBlock(Block block)
        {
            return SpeciesFromJuvenileCode(block?.Code) != null;
        }

        public static string SpeciesFromJuvenileCode(AssetLocation code)
        {
            if (code == null) return null;
            if (!Domain.Equals(code.Domain, System.StringComparison.OrdinalIgnoreCase)) return null;
            string path = code.Path ?? "";
            if (!path.StartsWith(Prefix) || !path.EndsWith(Suffix)) return null;
            string inner = path.Substring(Prefix.Length, path.Length - Prefix.Length - Suffix.Length);
            return inner.Length > 0 ? inner : null;
        }

        public static string SpeciesFromJuvenile(Block block)
        {
            return SpeciesFromJuvenileCode(block?.Code);
        }
    }
}
