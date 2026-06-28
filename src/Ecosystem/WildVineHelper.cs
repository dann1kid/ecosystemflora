using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal readonly struct WildVineInfo
    {
        public readonly bool Tropical;
        public readonly bool IsEnd;
        public readonly BlockFacing Facing;

        public WildVineInfo(bool tropical, bool isEnd, BlockFacing facing)
        {
            Tropical = tropical;
            IsEnd = isEnd;
            Facing = facing;
        }
    }

    /// <summary>Vanilla <c>wildvine-*</c> block parsing and placement helpers.</summary>
    internal static class WildVineHelper
    {
        public const string TemperateSpecies = "wildvine";
        public const string TropicalSpecies = "wildvine-tropical";

        public static bool IsVineBlock(Block block) => TryParse(block, out _);

        public static bool IsEndBlock(Block block) => TryParse(block, out WildVineInfo info) && info.IsEnd;

        public static bool TryParse(Block block, out WildVineInfo info)
        {
            info = default;
            if (block?.Code == null || block.Code.Domain != "game") return false;

            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path) || !path.StartsWith("wildvine")) return false;

            bool tropical = path.Contains("-tropical-");
            bool isEnd = path.Contains("-end-");
            bool isSection = path.Contains("-section-");
            if (!isEnd && !isSection) return false;

            BlockFacing facing = ResolveFacing(block, path);
            if (facing == null) return false;

            info = new WildVineInfo(tropical, isEnd, facing);
            return true;
        }

        public static string SpeciesFor(bool tropical) => tropical ? TropicalSpecies : TemperateSpecies;

        public static bool IsKnown(string species) =>
            species == TemperateSpecies || species == TropicalSpecies;

        public static Block ResolveEndBlock(IWorldAccessor world, bool tropical, BlockFacing facing)
        {
            if (world == null || facing == null) return null;
            string path = tropical
                ? "wildvine-tropical-end-" + facing.Code
                : "wildvine-end-" + facing.Code;
            return world.GetBlock(new AssetLocation("game", path));
        }

        public static Block ResolveSectionBlock(IWorldAccessor world, bool tropical, BlockFacing facing)
        {
            if (world == null || facing == null) return null;
            string path = tropical
                ? "wildvine-tropical-section-" + facing.Code
                : "wildvine-section-" + facing.Code;
            return world.GetBlock(new AssetLocation("game", path));
        }

        public static bool MatchesColumn(Block block, WildVineInfo expected)
        {
            if (!TryParse(block, out WildVineInfo info)) return false;
            return info.Tropical == expected.Tropical && info.Facing == expected.Facing;
        }

        public static bool CanHostVine(IBlockAccessor acc, Block vineSample, BlockPos hostPos, BlockFacing vineFacing)
        {
            if (acc == null || vineSample == null || hostPos == null || vineFacing == null) return false;

            Block host = acc.GetBlock(hostPos);
            if (host.Id == 0) return false;

            return host.CanAttachBlockAt(acc, vineSample, hostPos, vineFacing);
        }

        public static BlockPos HostPos(BlockPos vinePos, BlockFacing vineFacing) =>
            vinePos.AddCopy(vineFacing.Opposite);

        public static BlockPos VinePosForHost(BlockPos hostPos, BlockFacing vineFacing) =>
            hostPos.AddCopy(vineFacing);

        public static BlockPos FindLowestEnd(IBlockAccessor acc, BlockPos start, in WildVineInfo info)
        {
            if (acc == null || start == null) return start;

            var pos = start.Copy();
            while (acc.IsValidPos(pos.DownCopy()))
            {
                Block below = acc.GetBlock(pos.DownCopy());
                if (!MatchesColumn(below, info)) break;
                pos.Down();
            }

            return pos;
        }

        static BlockFacing ResolveFacing(Block block, string path)
        {
            if (block?.Variant != null
                && block.Variant.TryGetValue("horizontalorientation", out string orient)
                && !string.IsNullOrEmpty(orient))
            {
                BlockFacing fromVariant = BlockFacing.FromCode(orient);
                if (fromVariant != null) return fromVariant;
            }

            int lastDash = path.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= path.Length - 1) return null;

            return BlockFacing.FromCode(path.Substring(lastDash + 1));
        }
    }
}
