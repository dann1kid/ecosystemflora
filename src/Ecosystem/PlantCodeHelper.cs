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
