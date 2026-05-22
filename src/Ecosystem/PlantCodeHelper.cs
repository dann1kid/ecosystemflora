using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    public static class PlantCodeHelper
    {
        /// <summary>wildseeds-flower-catmint-free → wildplant-flower-catmint-free</summary>
        public static AssetLocation WildPlantCodeFromSeed(CollectibleObject seed)
        {
            if (seed?.Code == null) return null;

            string path = seed.Code.Path;
            const string seedPrefix = "wildseeds-";
            if (path.StartsWith(seedPrefix))
            {
                return new AssetLocation(seed.Code.Domain, "wildplant-" + path.Substring(seedPrefix.Length));
            }

            return new AssetLocation(seed.Code.Domain, "wildplant-" + seed.CodeEndWithoutParts(1));
        }

        /// <summary>wildplant-flower-catmint-free → flower-catmint-free</summary>
        public static string MatureCodePath(Block block)
        {
            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            const string prefix = "wildplant-";
            if (path.StartsWith(prefix)) return path.Substring(prefix.Length);

            return block.CodeEndWithoutParts(1);
        }

        public static AssetLocation MatureBlockLocation(Block wildPlantBlock)
        {
            string maturePath = MatureCodePath(wildPlantBlock);
            if (maturePath == null) return null;
            return new AssetLocation("game:" + maturePath);
        }
    }
}
