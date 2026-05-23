using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public static class PlantCodeHelper
    {
        public static bool IsEcologyPlant(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            return TryGetEcologySpecies(block.Code, out _);
        }

        public static bool IsVanillaEcologyPlant(Block block) => IsEcologyPlant(block);

        public static bool TryGetEcologySpecies(AssetLocation blockCode, out string species)
        {
            species = GetEcologySpecies(blockCode);
            return species != null;
        }

        public static string GetEcologySpecies(AssetLocation blockCode)
        {
            string path = blockCode?.Path;
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith("flower-"))
            {
                string rest = path.Substring("flower-".Length);
                if (rest.EndsWith("-free")) rest = rest.Substring(0, rest.Length - "-free".Length);
                else if (rest.EndsWith("-snow")) rest = rest.Substring(0, rest.Length - "-snow".Length);
                if (rest.StartsWith("lupine")) return "lupine";
                return rest;
            }

            if (path.StartsWith("tallplant-coopersreed")) return "coopersreed";
            if (path.StartsWith("tallplant-papyrus")) return "papyrus";
            if (path == "waterlily") return "waterlily";
            if (path.StartsWith("aquatic-watercrowfoot")) return "watercrowfoot";

            return null;
        }

        public static bool IsReedBlock(Block block)
        {
            string species = GetEcologySpecies(block?.Code);
            return species == "coopersreed" || species == "papyrus";
        }

        public static bool IsWatercrowfoot(AssetLocation code)
        {
            return GetEcologySpecies(code) == "watercrowfoot";
        }

        /// <summary>Lowest block of a water-crowfoot column (for reproduce origin).</summary>
        public static BlockPos GetColumnBase(IBlockAccessor acc, BlockPos pos)
        {
            BlockPos scan = pos.Copy();
            while (IsWatercrowfoot(acc.GetBlock(scan.DownCopy())?.Code))
            {
                scan.Down();
            }

            return scan;
        }

        public static BlockPos GetReproduceAnchor(IBlockAccessor acc, BlockPos pos, AssetLocation blockCode)
        {
            if (GetEcologySpecies(blockCode) == "watercrowfoot")
            {
                return GetColumnBase(acc, pos);
            }

            return pos.Copy();
        }

        public static EcologyHabitat GetEcologyHabitat(AssetLocation blockCode)
        {
            string species = GetEcologySpecies(blockCode);
            if (species == null) return EcologyHabitat.Terrestrial;
            if (WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic))
            {
                return aquatic.Habitat;
            }

            return EcologyHabitat.Terrestrial;
        }

        public static AssetLocation SpreadBlockCode(Block block)
        {
            if (block?.Code == null) return null;
            if (!IsEcologyPlant(block)) return null;

            if (GetEcologySpecies(block.Code) == "watercrowfoot")
            {
                return new AssetLocation("game:aquatic-watercrowfoot-section");
            }

            return block.Code;
        }

        /// <summary>Pick land-normal vs water-normal from target cell (vanilla habitat).</summary>
        public static Block ResolveReedSpreadBlock(ICoreAPI api, BlockPos plantPos, Block parentBlock)
        {
            string species = GetEcologySpecies(parentBlock?.Code);
            if (species != "coopersreed" && species != "papyrus") return parentBlock;

            IBlockAccessor acc = api.World.BlockAccessor;
            string habitat = BlockFluidHelper.IsDedicatedWaterCell(acc, plantPos) ? "water" : "land";
            string path = parentBlock.Code.Path ?? "";
            string cover = path.Contains("-snow") ? "snow" : "free";
            string code = "tallplant-" + species + "-" + habitat + "-normal-" + cover;
            Block block = api.World.GetBlock(new AssetLocation("game:" + code));
            return block ?? parentBlock;
        }

        public static bool SameEcologySpecies(AssetLocation a, AssetLocation b)
        {
            string sa = GetEcologySpecies(a);
            string sb = GetEcologySpecies(b);
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
