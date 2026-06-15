using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    public readonly struct CanopyAmbienceSample
    {
        public CanopyAmbienceSample(bool hasCanopy, int canopyY, float density, string dominantWood)
        {
            HasCanopy = hasCanopy;
            CanopyY = canopyY;
            Density = density;
            DominantWood = dominantWood;
        }

        public bool HasCanopy { get; }
        public int CanopyY { get; }
        public float Density { get; }
        public string DominantWood { get; }
    }

    /// <summary>Detects deciduous canopy height and density above the player.</summary>
    public static class CanopyAmbienceSampler
    {
        const int ColumnCount = 5;
        const int MaxScanUpBlocks = 24;

        static readonly int[] ColumnDx = { 0, 1, -1, 0, 0 };
        static readonly int[] ColumnDz = { 0, 0, 0, 1, -1 };

        public static CanopyAmbienceSample Sample(
            IBlockAccessor acc,
            int playerX,
            int playerY,
            int playerZ,
            int minHeightBlocks)
        {
            if (acc == null || minHeightBlocks < 1) return default;

            int scanBaseY = playerY + minHeightBlocks;
            int topY = playerY + MaxScanUpBlocks;
            if (topY >= acc.MapSizeY) topY = acc.MapSizeY - 1;
            if (scanBaseY > topY) return default;

            var columnCanopyY = new int[ColumnCount];
            for (int i = 0; i < ColumnCount; i++)
            {
                columnCanopyY[i] = -1;
            }

            var woodCounts = new Dictionary<string, int>();
            var scratch = new BlockPos(0);

            for (int c = 0; c < ColumnCount; c++)
            {
                int x = playerX + ColumnDx[c];
                int z = playerZ + ColumnDz[c];

                for (int y = scanBaseY; y <= topY; y++)
                {
                    scratch.Set(x, y, z);
                    Block block = acc.GetBlock(scratch);
                    if (!CanopyFoliageRules.IsSeasonalFoliageBlock(block)) continue;

                    columnCanopyY[c] = y;
                    string wood = ResolveWood(block);
                    if (!string.IsNullOrEmpty(wood))
                    {
                        if (!woodCounts.TryGetValue(wood, out int count)) count = 0;
                        woodCounts[wood] = count + 1;
                    }

                    break;
                }
            }

            int lowestCanopyY = int.MaxValue;
            int columnsWithCanopy = 0;
            for (int c = 0; c < ColumnCount; c++)
            {
                if (columnCanopyY[c] < 0) continue;

                columnsWithCanopy++;
                if (columnCanopyY[c] < lowestCanopyY)
                {
                    lowestCanopyY = columnCanopyY[c];
                }
            }

            if (columnsWithCanopy == 0 || lowestCanopyY == int.MaxValue)
            {
                return default;
            }

            int nearBand = 0;
            for (int c = 0; c < ColumnCount; c++)
            {
                int y = columnCanopyY[c];
                if (y >= 0 && y <= lowestCanopyY + 2)
                {
                    nearBand++;
                }
            }

            float density = nearBand / (float)ColumnCount;
            string dominantWood = ResolveDominantWood(woodCounts);

            return new CanopyAmbienceSample(true, lowestCanopyY, density, dominantWood);
        }

        static string ResolveWood(Block block)
        {
            if (block == null) return null;

            FoliageCellKind kind = CanopyFoliageRules.Classify(block);
            if (kind == FoliageCellKind.LogGrown)
            {
                return PlantCodeHelper.GetTreeWood(block);
            }

            if (kind == FoliageCellKind.BranchyLeaf || kind == FoliageCellKind.RegularLeaf)
            {
                return CanopyBlockHelper.GetWoodFromFoliageBlock(block);
            }

            return null;
        }

        static string ResolveDominantWood(Dictionary<string, int> woodCounts)
        {
            if (woodCounts == null || woodCounts.Count == 0) return null;

            string best = null;
            int bestCount = -1;
            foreach (KeyValuePair<string, int> kv in woodCounts)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    best = kv.Key;
                }
            }

            return best;
        }
    }
}
