using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Wild tree spread: one log-grown block and a small crown instead of vanilla sapling treegen.</summary>
    internal static class TreeYoungStructure
    {
        static readonly int[] HorizDx = { 1, -1, 0, 0 };
        static readonly int[] HorizDz = { 0, 0, 1, -1 };

        public static bool TryPlaceSeedling(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos plantPos,
            string wood,
            System.Random rand)
        {
            if (api == null || acc == null || plantPos == null || string.IsNullOrEmpty(wood)) return false;
            if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) return false;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(plantPos))) return false;

            Block log = ResolveLogBlock(api.World, wood);
            if (log == null || log.Id == 0) return false;

            acc.SetBlock(log.BlockId, plantPos);
            acc.MarkBlockDirty(plantPos);
            EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(plantPos);

            Block logBlock = acc.GetBlock(plantPos);
            int foliagePlaced = 0;

            int slots = 2 + (rand != null ? rand.Next(2) : 1);
            int start = rand != null ? rand.Next(4) : 0;
            for (int n = 0; n < 4 && foliagePlaced < slots; n++)
            {
                int dir = (start + n) % 4;
                var side = new BlockPos(
                    plantPos.X + HorizDx[dir],
                    plantPos.Y + 1,
                    plantPos.Z + HorizDz[dir],
                    plantPos.dimension);
                bool branchy = rand != null && rand.NextDouble() < 0.3;
                TryPlaceFoliage(api, acc, side, wood, plantPos, logBlock, branchy, ref foliagePlaced);
            }

            return PlantCodeHelper.IsTreeLogGrownBlock(acc.GetBlock(plantPos));
        }

        static void TryPlaceFoliage(
            ICoreAPI api,
            IBlockAccessor acc,
            BlockPos target,
            string wood,
            BlockPos anchorPos,
            Block anchorBlock,
            bool branchy,
            ref int placed)
        {
            if (!acc.IsValidPos(target)) return;
            if (!LandClaimGuard.AllowsEcologyChange(api, target)) return;
            if (!PlantVacancyRules.IsVacantPlantSpace(acc.GetBlock(target))) return;

            Block leaf = branchy
                ? CanopyBlockHelper.ResolveBranchyLeafBlock(api.World, wood, target, anchorPos, anchorBlock)
                : CanopyBlockHelper.ResolveGrownLeafBlock(api.World, wood, target, anchorPos, anchorBlock);
            if (leaf == null || leaf.Id == 0) return;

            acc.SetBlock(leaf.BlockId, target);
            acc.MarkBlockDirty(target);
            EcosystemSystem.Instance?.FoliageCells?.OnBlockAdded(target);
            placed++;
        }

        static Block ResolveLogBlock(IWorldAccessor world, string wood)
        {
            if (world == null || string.IsNullOrEmpty(wood)) return null;

            Block block = world.GetBlock(new AssetLocation("game", "log-grown-" + wood + "-ud"));
            if (block != null && block.Id != 0) return block;

            block = world.GetBlock(new AssetLocation("game", "log-grown-" + wood + "-north"));
            return block != null && block.Id != 0 ? block : null;
        }
    }
}
