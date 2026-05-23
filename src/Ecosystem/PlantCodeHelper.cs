using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public static class PlantCodeHelper
    {
        public static bool IsEcologyPlant(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            if (TryGetEcologySpecies(block.Code, out _)) return true;
            return IsTreeSaplingBlock(block) || IsWildBerryBushBlock(block);
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

            if (path.StartsWith("fruitingbush-wild-"))
            {
                string berry = ParseBerryType(path);
                if (berry != null) return berry;
            }

            if (path == "tallfern") return "tallfern";

            if (path.StartsWith("fern-"))
            {
                string fernType = path.Substring("fern-".Length);
                if (WildFernEcology.TryGet(fernType, out _)) return fernType;
            }

            string wood = GetTreeWood(blockCode);
            if (wood != null) return wood;

            return null;
        }

        public static bool IsTreeLogGrownBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("log-grown-")) return false;
            if (path.StartsWith("log-grown-aged")) return false;
            return GetTreeWood(block) != null;
        }

        public static bool IsTreeSaplingBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("sapling-")) return false;
            if (!path.EndsWith("-free")) return false;
            if (path.Contains("bambooshoots") || path.Contains("bamboo-")) return false;
            return GetTreeWood(block) != null;
        }

        /// <summary>True for mature tree parents, flowers, reeds, etc. Saplings count for spacing only.</summary>
        public static bool IsEcologySpreadParent(Block block)
        {
            if (block == null) return false;
            if (IsTreeSaplingBlock(block)) return false;
            if (IsWildBerryBushBlock(block)) return true;
            if (IsTreeLogGrownBlock(block)) return true;
            return IsEcologyPlant(block);
        }

        public static bool IsWildBerryBushBlock(Block block)
        {
            if (block?.Code == null || block.Code.Domain != "game") return false;
            string path = block.Code.Path;
            return path != null
                && path.StartsWith("fruitingbush-wild-")
                && ParseBerryType(path) != null;
        }

        static string ParseBerryType(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("fruitingbush-wild-")) return null;

            string rest = path.Substring("fruitingbush-wild-".Length);
            int free = rest.IndexOf("-free");
            if (free > 0) rest = rest.Substring(0, free);
            else
            {
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
            }

            return WildBerryEcology.TryGet(rest, out _) ? rest : null;
        }

        public static string GetTreeWood(Block block)
        {
            if (block?.Variant != null && block.Variant.TryGetValue("wood", out string variantWood))
            {
                if (WildTreeEcology.TryGet(variantWood, out _)) return variantWood;
            }

            return GetTreeWood(block?.Code);
        }

        public static string GetTreeWood(AssetLocation blockCode)
        {
            if (blockCode == null || blockCode.Domain != "game") return null;

            string path = blockCode.Path;
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith("log-grown-"))
            {
                string rest = path.Substring("log-grown-".Length);
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
                if (rest == "aged") return null;
                return WildTreeEcology.TryGet(rest, out _) ? rest : null;
            }

            if (path.StartsWith("sapling-"))
            {
                string rest = path.Substring("sapling-".Length);
                int dash = rest.IndexOf('-');
                if (dash > 0) rest = rest.Substring(0, dash);
                return WildTreeEcology.TryGet(rest, out _) ? rest : null;
            }

            return null;
        }

        public static BlockPos GetTreeTrunkBase(IBlockAccessor acc, BlockPos logPos)
        {
            BlockPos scan = logPos.Copy();
            string wood = GetTreeWood(acc.GetBlock(scan)?.Code);
            if (wood == null) return logPos.Copy();

            while (true)
            {
                BlockPos below = scan.DownCopy();
                Block belowBlock = acc.GetBlock(below);
                if (!IsTreeLogGrownBlock(belowBlock)) break;
                if (GetTreeWood(belowBlock) != wood) break;
                scan.Set(below);
            }

            return scan;
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

            if (IsTreeLogGrownBlock(acc.GetBlock(pos)))
            {
                return GetTreeTrunkBase(acc, pos);
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

            if (WildTreeEcology.TryGet(species, out _))
            {
                return EcologyHabitat.TerrestrialTree;
            }

            return EcologyHabitat.Terrestrial;
        }

        public static AssetLocation SpreadBlockCode(Block block)
        {
            if (block?.Code == null) return null;

            string wood = GetTreeWood(block);
            if (wood != null && IsTreeLogGrownBlock(block))
            {
                return new AssetLocation("game:sapling-" + wood + "-free");
            }

            if (!IsEcologyPlant(block)) return null;

            if (IsTreeSaplingBlock(block)) return null;

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
