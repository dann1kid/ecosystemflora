using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class ReproducePlacement
    {
        public static List<Vec2i> ShuffledHorizontalOffsets(int radius, System.Random rand)
        {
            var cells = new List<Vec2i>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    cells.Add(new Vec2i(dx, dz));
                }
            }

            for (int i = cells.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                Vec2i tmp = cells[i];
                cells[i] = cells[j];
                cells[j] = tmp;
            }

            return cells;
        }

        public static bool TryPlaceSpreadNear(
            ICoreAPI api,
            BlockPos origin,
            int dx,
            int dz,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int verticalSearch,
            bool logFailures,
            out string failureReason)
        {
            failureReason = null;
            IBlockAccessor acc = api.World.BlockAccessor;

            if (!SurfacePlacement.TryFindPlantPos(acc, origin, dx, dz, verticalSearch, out BlockPos plantPos, out failureReason))
            {
                return false;
            }

            return TryPlaceSpread(
                api,
                plantPos,
                spreadBlock,
                requirements,
                minFitness,
                harshClimate,
                logFailures,
                out failureReason);
        }

        public static bool TryPlaceSpread(
            ICoreAPI api,
            BlockPos plantPos,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            bool logFailures,
            out string failureReason)
        {
            failureReason = null;
            IBlockAccessor acc = api.World.BlockAccessor;
            EnvironmentalContext ctx = EnvironmentalContext.Sample(api, plantPos);

            if (!SuitabilityEvaluator.CanReproduce(requirements, ctx, harshClimate))
            {
                failureReason = SuitabilityEvaluator.DescribeReproduceFailure(requirements, ctx, harshClimate);
                return false;
            }

            float score = SuitabilityEvaluator.Score(requirements, ctx, harshClimate);
            if (score < minFitness)
            {
                failureReason = "Fitness " + score.ToString("0.00") + " < " + minFitness.ToString("0.00");
                return false;
            }

            ItemStack stack = new ItemStack(spreadBlock);
            acc.SetBlock(spreadBlock.BlockId, plantPos, stack);

            if (acc.GetBlock(plantPos).Id != spreadBlock.Id)
            {
                failureReason = "Block id mismatch after place";
                return false;
            }

            acc.MarkBlockDirty(plantPos);
            return true;
        }
    }
}
