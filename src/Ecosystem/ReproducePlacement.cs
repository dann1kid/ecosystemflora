using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class ReproducePlacement
    {
        readonly struct SpreadCandidate
        {
            public BlockPos Pos { get; }
            public float Fitness { get; }

            public SpreadCandidate(BlockPos pos, float fitness)
            {
                Pos = pos;
                Fitness = fitness;
            }
        }

        /// <summary>
        /// Scans all columns in radius, collects valid free cells, then picks up to maxSpawns (weighted by fitness).
        /// </summary>
        public static int TryPlaceSpreadAmongNeighbors(
            ICoreAPI api,
            BlockPos origin,
            Block spreadBlock,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            int radius,
            int verticalSearch,
            int maxSpawns,
            System.Random rand,
            bool logFailures,
            out string failureReason,
            System.Action<BlockPos, PlantRequirements> onPlaced = null)
        {
            failureReason = null;
            if (maxSpawns <= 0) return 0;

            if (requirements != null && requirements.SpreadRadius > 0)
            {
                radius = requirements.SpreadRadius;
            }

            List<SpreadCandidate> candidates = CollectSpreadCandidates(
                api, origin, radius, verticalSearch, requirements, minFitness, harshClimate);

            if (candidates.Count == 0)
            {
                failureReason = "No valid free cells in radius " + radius;
                return 0;
            }

            int placed = 0;
            var remaining = new List<SpreadCandidate>(candidates);

            while (placed < maxSpawns && remaining.Count > 0)
            {
                int index = PickWeightedIndex(remaining, rand);
                SpreadCandidate chosen = remaining[index];
                remaining.RemoveAt(index);

                if (PlaceSpreadBlock(api, chosen.Pos, spreadBlock, requirements, origin))
                {
                    placed++;
                    onPlaced?.Invoke(chosen.Pos, requirements);
                    if (logFailures)
                    {
                        api.Logger.Notification(
                            "[wildfarming] Spawned {0} at {1} ({2} candidates near {3})",
                            spreadBlock.Code, chosen.Pos, candidates.Count, origin);
                    }
                }
            }

            if (placed == 0)
            {
                failureReason = "Placement failed after selecting candidate";
            }

            return placed;
        }

        static List<SpreadCandidate> CollectSpreadCandidates(
            ICoreAPI api,
            BlockPos origin,
            int radius,
            int verticalSearch,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate)
        {
            var candidates = new List<SpreadCandidate>();
            var seen = new HashSet<BlockPos>();
            IBlockAccessor acc = api.World.BlockAccessor;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    bool foundPos;
                    BlockPos plantPos;
                    if (requirements.Habitat == EcologyHabitat.TerrestrialTree)
                    {
                        int sun = requirements.MinSunlight > 0 ? requirements.MinSunlight : 11;
                        foundPos = TreePlacement.TryFindSaplingPos(
                            acc, origin, dx, dz, verticalSearch, sun, out plantPos, out _);
                    }
                    else if (requirements.Habitat != EcologyHabitat.Terrestrial)
                    {
                        foundPos = WaterPlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, requirements, out plantPos, out _);
                    }
                    else
                    {
                        foundPos = SurfacePlacement.TryFindPlantPos(
                            acc, origin, dx, dz, verticalSearch, out plantPos, out _);
                    }

                    if (!foundPos) continue;

                    if (requirements.MinSunlight > 0
                        && !TreePlacement.HasEnoughSunlight(acc, plantPos, requirements.MinSunlight))
                    {
                        continue;
                    }

                    if (!seen.Add(plantPos)) continue;

                    EnvironmentalContext ctx = EnvironmentalContext.Sample(api, plantPos, requirements);
                    if (!SuitabilityEvaluator.CanReproduce(requirements, ctx, harshClimate)) continue;

                    float fitness = SuitabilityEvaluator.ReproduceFitness(requirements, ctx);
                    if (fitness < minFitness) continue;

                    if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) continue;

                    candidates.Add(new SpreadCandidate(plantPos.Copy(), fitness));
                }
            }

            return candidates;
        }

        static int PickWeightedIndex(List<SpreadCandidate> candidates, System.Random rand)
        {
            if (candidates.Count == 1) return 0;

            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                total += candidates[i].Fitness;
            }

            if (total <= 0f) return rand.Next(candidates.Count);

            float roll = (float)rand.NextDouble() * total;
            float acc = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                acc += candidates[i].Fitness;
                if (roll <= acc) return i;
            }

            return candidates.Count - 1;
        }

        static bool PlaceSpreadBlock(
            ICoreAPI api,
            BlockPos plantPos,
            Block spreadBlock,
            PlantRequirements requirements,
            BlockPos parentOrigin)
        {
            if (requirements.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                IBlockAccessor acc = api.World.BlockAccessor;
                int height = CrowfootColumnPlacer.MeasureColumnHeight(acc, parentOrigin);
                if (height < 2) height = 3;

                BlockFluidHelper.TryMeasureUnderwaterColumnDepth(acc, plantPos, out int waterDepth, out _);
                if (waterDepth > 0 && waterDepth < height) height = waterDepth;

                bool preferFlower = spreadBlock.Code?.Path?.Contains("top") == true;
                return CrowfootColumnPlacer.PlaceColumn(api, plantPos, height, preferFlower, api.World.Rand);
            }

            IBlockAccessor accessor = api.World.BlockAccessor;

            if (requirements.Habitat == EcologyHabitat.ReedNearWater)
            {
                spreadBlock = PlantCodeHelper.ResolveReedSpreadBlock(api, plantPos, spreadBlock);
            }

            ItemStack stack = new ItemStack(spreadBlock);
            accessor.SetBlock(spreadBlock.BlockId, plantPos, stack);

            if (accessor.GetBlock(plantPos).Id != spreadBlock.Id) return false;

            accessor.MarkBlockDirty(plantPos);
            return true;
        }
    }
}
