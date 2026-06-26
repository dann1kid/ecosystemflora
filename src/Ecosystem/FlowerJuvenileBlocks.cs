using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Juvenile spread blocks (ecosystemflora domain) for colonizer maturation.</summary>
    internal static class FlowerJuvenileBlocks
    {
        const string Prefix = "juvenile-flower-";
        const string Suffix = JuvenileBlockNaming.Suffix;

        public static AssetLocation CodeForSpecies(string species)
        {
            return JuvenileBlockNaming.CodeForSpecies(Prefix, species);
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
            return JuvenileBlockNaming.SpeciesFromCode(Prefix, code);
        }

        public static string SpeciesFromJuvenile(Block block)
        {
            return SpeciesFromJuvenileCode(block?.Code);
        }
    }
}
