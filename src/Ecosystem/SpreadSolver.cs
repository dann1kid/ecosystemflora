using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal readonly struct SpreadSolveWinner
    {
        public readonly BlockPos TargetPos;
        public readonly float Fitness;
        public readonly bool Displacing;

        public SpreadSolveWinner(BlockPos targetPos, float fitness, bool displacing)
        {
            TargetPos = targetPos;
            Fitness = fitness;
            Displacing = displacing;
        }
    }

    /// <summary>Worker-safe spread candidate scoring and weighted pick.</summary>
    internal static class SpreadSolver
    {
        readonly struct ScoredCandidate
        {
            public readonly SpreadSolveCell Cell;
            public readonly float Fitness;
            public readonly bool Displacing;

            public ScoredCandidate(in SpreadSolveCell cell, float fitness, bool displacing)
            {
                Cell = cell;
                Fitness = fitness;
                Displacing = displacing;
            }
        }

        public static int PickWinners(
            IList<SpreadSolveCell> cells,
            IList<Block> blocks,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            SpreadCollectPhase phase,
            float seasonSpreadMult,
            float seedFitnessScale,
            int maxSpawns,
            System.Random rand,
            List<SpreadSolveWinner> winners,
            bool emptyFirstTwoPhase = false)
        {
            if (emptyFirstTwoPhase)
            {
                int picked = PickWinners(
                    cells, blocks, requirements, minFitness, harshClimate,
                    SpreadCollectPhase.EmptyOnly, seasonSpreadMult, seedFitnessScale,
                    maxSpawns, rand, winners);
                if (picked > 0) return picked;

                winners?.Clear();
                return PickWinners(
                    cells, blocks, requirements, minFitness, harshClimate,
                    SpreadCollectPhase.DisplacementOnly, seasonSpreadMult, seedFitnessScale,
                    maxSpawns, rand, winners);
            }

            return PickWinnersCore(
                cells, blocks, requirements, minFitness, harshClimate, phase,
                seasonSpreadMult, seedFitnessScale, maxSpawns, rand, winners);
        }

        static int PickWinnersCore(
            IList<SpreadSolveCell> cells,
            IList<Block> blocks,
            PlantRequirements requirements,
            float minFitness,
            bool harshClimate,
            SpreadCollectPhase phase,
            float seasonSpreadMult,
            float seedFitnessScale,
            int maxSpawns,
            System.Random rand,
            List<SpreadSolveWinner> winners)
        {
            winners?.Clear();
            if (cells == null || blocks == null || requirements == null || winners == null || maxSpawns <= 0)
            {
                return 0;
            }

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            bool matSpread = SpreadSolveBatchBuilder.UsesMatSpread(requirements);
            bool crowfootSpread = SpreadSolveBatchBuilder.UsesCrowfootSpread(requirements);
            var scored = new List<ScoredCandidate>();

            for (int i = 0; i < cells.Count; i++)
            {
                SpreadSolveCell cell = cells[i];
                bool passesPreflight = crowfootSpread
                    ? SpreadSolvePreflight.PassesCrowfoot(in cell, requirements, blocks, out bool isEmpty)
                    : matSpread
                        ? SpreadSolvePreflight.PassesMat(in cell, requirements, blocks, out isEmpty)
                        : SpreadSolvePreflight.PassesTerrestrial(in cell, requirements, blocks, out isEmpty);
                if (!passesPreflight)
                {
                    continue;
                }

                BlockPos plantPos = cell.ToPos();
                float fitness;
                bool displacing = false;

                if (matSpread || crowfootSpread)
                {
                    if (phase != SpreadCollectPhase.All) continue;
                    if (!isEmpty && !cell.MatVacancyOk) continue;
                    if (!cell.SpacingOk) continue;

                    fitness = ScoreFromCell(in cell, requirements, plantPos, harshClimate, seasonSpreadMult, seedFitnessScale);
                    if (fitness < minFitness) continue;
                    scored.Add(new ScoredCandidate(in cell, fitness, false));
                    continue;
                }

                if (phase == SpreadCollectPhase.DisplacementOnly)
                {
                    if (isEmpty || requirements.Habitat != EcologyHabitat.Terrestrial || !cfg.UseCellDisplacement)
                    {
                        continue;
                    }

                    Block incumbent = ResolveBlock(blocks, cell.SpaceBlockId);
                    if (!CellCompetition.CanDisplaceFromSolveCell(
                            requirements,
                            incumbent,
                            plantPos,
                            in cell,
                            harshClimate,
                            seasonSpreadMult,
                            out fitness,
                            out _))
                    {
                        continue;
                    }

                    displacing = true;
                }
                else if (isEmpty)
                {
                    if (phase == SpreadCollectPhase.DisplacementOnly) continue;
                    if (!cell.SpacingOk) continue;

                    fitness = ScoreFromCell(in cell, requirements, plantPos, harshClimate, seasonSpreadMult, seedFitnessScale);
                    if (fitness < minFitness) continue;
                }
                else if (requirements.Habitat == EcologyHabitat.Terrestrial && cfg.UseCellDisplacement)
                {
                    if (phase == SpreadCollectPhase.EmptyOnly) continue;

                    Block incumbent = ResolveBlock(blocks, cell.SpaceBlockId);
                    if (!CellCompetition.CanDisplaceFromSolveCell(
                            requirements,
                            incumbent,
                            plantPos,
                            in cell,
                            harshClimate,
                            seasonSpreadMult,
                            out fitness,
                            out _))
                    {
                        continue;
                    }

                    displacing = true;
                    if (fitness < minFitness) continue;
                }
                else
                {
                    continue;
                }

                scored.Add(new ScoredCandidate(in cell, fitness, displacing));
            }

            if (scored.Count == 0) return 0;

            if (phase == SpreadCollectPhase.All
                && cfg.PreferSpreadToEmptyCells
                && !TurfColonizerSpread.PrefersOccupiedTurf(requirements.Species))
            {
                ApplyEmptySpreadPreference(scored, cfg.EmptySpreadFitnessMultiplier);
            }

            var remaining = new List<ScoredCandidate>(scored);
            int picked = 0;

            while (picked < maxSpawns && remaining.Count > 0)
            {
                int index = PickWeightedIndex(remaining, rand);
                ScoredCandidate chosen = remaining[index];
                remaining.RemoveAt(index);
                winners.Add(new SpreadSolveWinner(chosen.Cell.ToPos(), chosen.Fitness, chosen.Displacing));
                picked++;
            }

            return picked;
        }

        static float ScoreFromCell(
            in SpreadSolveCell cell,
            PlantRequirements requirements,
            BlockPos plantPos,
            bool harshClimate,
            float seasonSpreadMult,
            float seedFitnessScale)
        {
            EnvironmentalContext ctx = EnvironmentalContext.FromSpreadSolveCell(in cell);
            float fitness = CellCompetition.SpreadScoreFromContext(null, requirements, plantPos, harshClimate, ctx);

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.UseFloraContext)
            {
                fitness *= EcologySpreadFitness.ContextMultiplierFor(requirements, cell.FloraContext);
            }

            if (cfg.UseNicheContext && requirements.HasNicheProfile)
            {
                var niche = new LocalNiche((MoistureLevel)cell.NicheMoisture, (LightLevel)cell.NicheLight);
                fitness *= EcologySpreadFitness.NicheMultiplierFor(requirements, niche);
            }

            fitness *= cell.MyceliumFitnessMult;
            fitness *= seasonSpreadMult;
            fitness *= seedFitnessScale;
            return fitness;
        }

        static void ApplyEmptySpreadPreference(List<ScoredCandidate> candidates, float emptyMult)
        {
            if (emptyMult <= 1f) return;

            for (int i = 0; i < candidates.Count; i++)
            {
                ScoredCandidate c = candidates[i];
                if (c.Displacing || !c.Cell.IsEmpty) continue;

                candidates[i] = new ScoredCandidate(c.Cell, c.Fitness * emptyMult, false);
            }
        }

        static int PickWeightedIndex(List<ScoredCandidate> candidates, System.Random rand)
        {
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                total += candidates[i].Fitness;
            }

            if (total <= 0f) return rand.Next(candidates.Count);

            float roll = (float)rand.NextDouble() * total;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].Fitness;
                if (roll <= 0f) return i;
            }

            return candidates.Count - 1;
        }

        static Block ResolveBlock(IList<Block> blocks, int id)
        {
            if (blocks == null || id <= 0 || id >= blocks.Count) return null;
            return blocks[id];
        }
    }
}
