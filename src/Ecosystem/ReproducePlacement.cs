using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class ReproducePlacement
    {
        static readonly List<SpreadCandidate> scratchCandidates = new List<SpreadCandidate>();
        static readonly HashSet<BlockPos> scratchSeen = new HashSet<BlockPos>();

        readonly struct SpreadCandidate
        {
            public BlockPos Pos { get; }
            public float Fitness { get; }
            public bool Displacing { get; }

            public SpreadCandidate(BlockPos pos, float fitness, bool displacing)
            {
                Pos = pos;
                Fitness = fitness;
                Displacing = displacing;
            }
        }

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
            System.Action<BlockPos, PlantRequirements, bool> onPlaced = null)
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
                failureReason = "No competitive cells in radius " + radius;
                return 0;
            }

            if (EcosystemConfig.Loaded.PreferSpreadToEmptyCells)
            {
                candidates = FilterPreferEmpty(candidates);
            }

            int placed = 0;
            var remaining = new List<SpreadCandidate>(candidates);

            while (placed < maxSpawns && remaining.Count > 0)
            {
                int index = PickWeightedIndex(remaining, rand);
                SpreadCandidate chosen = remaining[index];
                remaining.RemoveAt(index);

                if (PlaceSpreadBlock(api, chosen.Pos, spreadBlock, requirements, origin, chosen.Displacing))
                {
                    placed++;
                    onPlaced?.Invoke(chosen.Pos, requirements, chosen.Displacing);
                    if (logFailures)
                    {
                        api.Logger.Notification(
                            "[wildfarming] {0} {1} at {2} ({3} candidates near {4})",
                            chosen.Displacing ? "Displaced" : "Spread",
                            spreadBlock.Code,
                            chosen.Pos,
                            candidates.Count,
                            origin);
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
            scratchCandidates.Clear();
            scratchSeen.Clear();
            IBlockAccessor acc = api.World.BlockAccessor;
            EcosystemConfig cfg = EcosystemConfig.Loaded;

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

                    if (!scratchSeen.Add(plantPos)) continue;

                    if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) continue;

                    CellBlockSnapshot snap = CellBlockSnapshot.Sample(acc, plantPos);
                    if (!SpreadPreflight.PassesPhysicalGate(acc, plantPos, requirements, in snap, out bool isEmpty))
                    {
                        continue;
                    }

                    float fitness;
                    bool displacing = false;
                    bool canOccupy = isEmpty
                        || SpreadVacancy.CanOccupy(acc, plantPos, requirements, snap.Space, isEmpty);

                    if (canOccupy)
                    {
                        EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
                        EnvironmentalContext ctx = EnvironmentalContext.SampleForSpread(
                            api, plantPos, in snap, requirements, cache);
                        fitness = CellCompetition.SpreadScoreFromContext(
                            api, requirements, plantPos, harshClimate, ctx);
                        if (fitness < minFitness) continue;
                        if (!PlantSpacing.MeetsSpacing(acc, plantPos, requirements, out _)) continue;
                    }
                    else if (requirements.Habitat == EcologyHabitat.Terrestrial && cfg.UseCellDisplacement)
                    {
                        if (!CellCompetition.CanDisplace(
                            api, requirements, snap.Space, plantPos, harshClimate, in snap,
                            out float challengerScore, out float incumbentScore))
                        {
                            continue;
                        }

                        fitness = challengerScore;
                        displacing = true;
                        if (fitness < minFitness) continue;
                    }
                    else
                    {
                        continue;
                    }

                    scratchCandidates.Add(new SpreadCandidate(plantPos.Copy(), fitness, displacing));
                }
            }

            var result = new List<SpreadCandidate>(scratchCandidates);
            scratchCandidates.Clear();
            scratchSeen.Clear();
            return result;
        }

        static List<SpreadCandidate> FilterPreferEmpty(List<SpreadCandidate> candidates)
        {
            var empty = new List<SpreadCandidate>();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!candidates[i].Displacing) empty.Add(candidates[i]);
            }

            return empty.Count > 0 ? empty : candidates;
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
            BlockPos parentOrigin,
            bool displacing)
        {
            if (!LandClaimGuard.AllowsEcologyChange(api, plantPos)) return false;

            if (requirements.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                IBlockAccessor acc = api.World.BlockAccessor;

                if (!BlockFluidHelper.TrySnapCrowfootColumnBase(acc, plantPos, out BlockPos columnBase))
                {
                    return false;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, columnBase)) return false;

                int height = CrowfootColumnPlacer.MeasureColumnHeight(acc, parentOrigin);
                if (height < 2) height = 3;

                if (BlockFluidHelper.TryMeasureWaterColumn(acc, columnBase, out int waterDepth, out _))
                {
                    if (waterDepth > 0 && waterDepth < height) height = waterDepth;
                }

                bool preferFlower = spreadBlock.Code?.Path?.Contains("top") == true;
                return CrowfootColumnPlacer.PlaceColumn(api, columnBase, height, preferFlower, api.World.Rand);
            }

            IBlockAccessor accessor = api.World.BlockAccessor;

            if (displacing)
            {
                Block incumbent = accessor.GetBlock(plantPos);
                EcosystemSystem eco = EcosystemSystem.Instance;
                if (eco != null && PlantCodeHelper.IsEcologySpreadParent(incumbent))
                {
                    eco.RemoveEcologyPlant(plantPos, cascadeSymbiosis: true, reason: "displaced");
                }
            }

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
