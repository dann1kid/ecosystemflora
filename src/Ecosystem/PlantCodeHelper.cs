using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public static class PlantCodeHelper
    {
        public static bool IsVanillaEcologyPlant(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;

            string path = block.Code.Path;
            return path.StartsWith("flower-");
        }

        /// <summary>Spread target equals the same vanilla block code (no wildplant stage).</summary>
        public static AssetLocation SpreadBlockCode(Block block)
        {
            if (block?.Code == null) return null;
            if (IsVanillaEcologyPlant(block)) return block.Code;
            return null;
        }

        /// <summary>Species segment from flower-{species}-free|snow.</summary>
        public static string GetFlowerSpecies(AssetLocation blockCode)
        {
            string path = blockCode?.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("flower-")) return null;

            string rest = path.Substring("flower-".Length);
            if (rest.EndsWith("-free")) rest = rest.Substring(0, rest.Length - "-free".Length);
            else if (rest.EndsWith("-snow")) rest = rest.Substring(0, rest.Length - "-snow".Length);

            // flower-lupine-{color} → lupine (separate blocktype from flower.json)
            if (rest.StartsWith("lupine")) return "lupine";

            return rest;
        }

        public static bool SameFlowerSpecies(AssetLocation a, AssetLocation b)
        {
            string sa = GetFlowerSpecies(a);
            string sb = GetFlowerSpecies(b);
            return sa != null && sa == sb;
        }

        public static AssetLocation MatureBlockLocation(Block block)
        {
            AssetLocation vanilla = SpreadBlockCode(block);
            if (vanilla != null) return vanilla;

            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            const string prefix = "wildplant-";
            if (path.StartsWith(prefix))
            {
                return new AssetLocation("game:" + path.Substring(prefix.Length));
            }

            return null;
        }
    }
}
