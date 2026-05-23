using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class CrowfootColumnPlacer
    {
        static Block sectionBlock;
        static Block tipBlock;
        static Block topBlock;

        public static bool PlaceColumn(ICoreAPI api, BlockPos basePos, int targetHeight, bool preferFlower, System.Random rand)
        {
            if (!EnsureBlocks(api)) return false;
            if (targetHeight < 2) targetHeight = 2;

            IBlockAccessor acc = api.World.BlockAccessor;
            BlockPos pos = basePos.Copy();
            int sections = targetHeight - 1;

            for (int i = 0; i < sections; i++)
            {
                acc.SetBlock(sectionBlock.BlockId, pos);
                pos.Up();
            }

            Block cap = preferFlower && rand.NextDouble() < 0.35 ? topBlock : tipBlock;
            acc.SetBlock(cap.BlockId, pos);
            acc.MarkBlockDirty(basePos);
            return PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(basePos).Code);
        }

        public static int MeasureColumnHeight(IBlockAccessor acc, BlockPos anyPartPos)
        {
            BlockPos basePos = PlantCodeHelper.GetColumnBase(acc, anyPartPos);
            int height = 0;
            BlockPos scan = basePos.Copy();
            while (PlantCodeHelper.IsWatercrowfoot(acc.GetBlock(scan).Code))
            {
                height++;
                scan.Up();
            }

            return height;
        }

        static bool EnsureBlocks(ICoreAPI api)
        {
            if (sectionBlock != null) return true;

            sectionBlock = api.World.GetBlock(new AssetLocation("game:aquatic-watercrowfoot-section"));
            tipBlock = api.World.GetBlock(new AssetLocation("game:aquatic-watercrowfoot-tip"));
            topBlock = api.World.GetBlock(new AssetLocation("game:aquatic-watercrowfoot-top"));
            return sectionBlock != null && tipBlock != null && topBlock != null;
        }
    }
}
