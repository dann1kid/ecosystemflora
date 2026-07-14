using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class CellCompetition
    {
        public static float SpreadScore(
            ICoreAPI api,
            PlantRequirements challenger,
            BlockPos targetPos,
            bool harshClimate)
        {
            if (api == null || challenger == null || targetPos == null) return 0f;

            EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
            EnvironmentalContext ctx = EnvironmentalContext.SampleForSpread(api, targetPos, challenger, cache);
            return SpreadScoreFromContext(api, challenger, targetPos, harshClimate, ctx);
        }

        internal static float SpreadClimateFitness(
            ICoreAPI api,
            PlantRequirements challenger,
            BlockPos targetPos,
            bool harshClimate,
            EnvironmentalContext ctx,
            bool occupied = false)
        {
            if (!SuitabilityEvaluator.CanCompeteForCell(challenger, ctx, harshClimate, occupied))
            {
                return 0f;
            }

            float fitness = SuitabilityEvaluator.ReproduceFitness(challenger, ctx);
            fitness = EcologySpreadFitness.ApplyContext(api, challenger, targetPos, fitness);
            fitness = EcologySpreadFitness.ApplyNiche(api, challenger, targetPos, fitness);
            fitness = MyceliumZone.ApplySpreadFitness(api, challenger, targetPos, fitness);
            fitness = EcologySpreadFitness.ApplyTraffic(api, targetPos, fitness);
            return fitness;
        }

        /// <summary>Weighted pick only — season and SpreadRate already gate attempt cadence via <see cref="SpeciesSpread"/>.</summary>
        internal static float SpreadAttemptWeight(
            ICoreAPI api,
            PlantRequirements challenger,
            BlockPos targetPos,
            float climateFitness)
        {
            if (climateFitness <= 0f) return 0f;

            float weight = climateFitness;
            weight *= SeasonEcology.SpreadActivityMultiplier(api, targetPos, challenger);
            if (challenger.SpreadRate > 0f)
            {
                weight *= challenger.SpreadRate;
            }

            return weight;
        }

        internal static float SpreadClimateFitnessFromSolveCell(
            PlantRequirements challenger,
            BlockPos targetPos,
            in SpreadSolveCell cell,
            bool harshClimate,
            bool occupied = false)
        {
            EnvironmentalContext ctx = EnvironmentalContext.FromSpreadSolveCell(in cell);
            if (!SuitabilityEvaluator.CanCompeteForCell(challenger, ctx, harshClimate, occupied))
            {
                return 0f;
            }

            float fitness = SuitabilityEvaluator.ReproduceFitness(challenger, ctx);
            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (cfg.UseFloraContext)
            {
                fitness *= EcologySpreadFitness.ContextMultiplierFor(challenger, cell.FloraContext);
            }

            if (cfg.UseNicheContext && challenger.HasNicheProfile
                && challenger.Habitat == EcologyHabitat.Terrestrial)
            {
                var niche = new LocalNiche((MoistureLevel)cell.NicheMoisture, (LightLevel)cell.NicheLight);
                fitness *= EcologySpreadFitness.NicheMultiplierFor(challenger, niche);
            }

            fitness *= cell.MyceliumFitnessMult;
            fitness *= cell.TrafficFitnessMult;
            return fitness;
        }

        internal static float SpreadScoreFromContext(
            ICoreAPI api,
            PlantRequirements challenger,
            BlockPos targetPos,
            bool harshClimate,
            EnvironmentalContext ctx,
            bool occupied = false)
        {
            float climate = SpreadClimateFitness(api, challenger, targetPos, harshClimate, ctx, occupied);
            return SpreadAttemptWeight(api, challenger, targetPos, climate);
        }

        public static float IncumbentHoldScore(
            ICoreAPI api,
            Block incumbentBlock,
            BlockPos targetPos,
            bool harshClimate)
        {
            if (api == null || incumbentBlock == null || targetPos == null) return float.MaxValue;

            PlantRequirements incumbent = PlantRequirements.FromBlock(incumbentBlock);
            if (string.IsNullOrEmpty(incumbent.Species)) return float.MaxValue;

            EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
            EnvironmentalContext ctx = EnvironmentalContext.SampleForSpread(api, targetPos, incumbent, cache);
            return IncumbentHoldScoreFromContext(api, incumbent, targetPos, harshClimate, ctx);
        }

        internal static float IncumbentHoldScoreFromContext(
            ICoreAPI api,
            PlantRequirements incumbent,
            BlockPos targetPos,
            bool harshClimate,
            EnvironmentalContext ctx)
        {
            if (!SuitabilityEvaluator.MeetsSurvivalRequirements(incumbent, ctx, harshClimate))
            {
                return 0f;
            }

            float hold = SuitabilityEvaluator.ReproduceFitness(incumbent, ctx);
            hold = EcologySpreadFitness.ApplyContext(api, incumbent, targetPos, hold);
            hold = EcologySpreadFitness.ApplyNiche(api, incumbent, targetPos, hold);
            hold = EcologySpreadFitness.ApplyTraffic(api, targetPos, hold);
            if (incumbent.HoldStrength > 0f)
            {
                hold *= incumbent.HoldStrength;
            }

            return hold;
        }

        public static bool CanDisplace(
            ICoreAPI api,
            PlantRequirements challenger,
            Block incumbentBlock,
            BlockPos targetPos,
            bool harshClimate,
            in CellBlockSnapshot snap,
            out float challengerScore,
            out float incumbentScore)
        {
            challengerScore = 0f;
            incumbentScore = float.MaxValue;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseCellDisplacement || api == null || challenger == null || incumbentBlock == null)
            {
                return false;
            }

            if (!PlantCodeHelper.IsEcologySpreadParent(incumbentBlock)) return false;
            if (PlantCodeHelper.IsArborealHostBlock(incumbentBlock)) return false;

            IBlockAccessor acc = api.World.BlockAccessor;
            if (!SpreadPreflight.PassesPhysicalGate(acc, targetPos, challenger, in snap, out _))
            {
                return false;
            }

            string incumbentSpecies = PlantCodeHelper.ResolveEcologySpecies(incumbentBlock);
            if (incumbentSpecies != null && incumbentSpecies == challenger.Species) return false;

            EnvironmentalColumnCache cache = EcosystemSystem.Instance?.ColumnCache;
            EnvironmentalContext ctx = EnvironmentalContext.SampleForSpread(
                api, targetPos, in snap, challenger, cache);
            if (!SuitabilityEvaluator.CanCompeteForCell(challenger, ctx, harshClimate, occupied: true))
            {
                return false;
            }

            challengerScore = SpreadClimateFitness(api, challenger, targetPos, harshClimate, ctx, occupied: true);
            challengerScore = MeadowTurfCompetition.AdjustChallengerSpreadScore(
                challengerScore,
                challenger.Species,
                incumbentSpecies);

            PlantRequirements incumbent = PlantRequirements.FromBlock(incumbentBlock);
            if (string.IsNullOrEmpty(incumbent.Species))
            {
                incumbentScore = float.MaxValue;
            }
            else
            {
                incumbentScore = IncumbentHoldScoreFromContext(api, incumbent, targetPos, harshClimate, ctx);
            }

            if (challengerScore <= 0f) return false;

            return challengerScore >= incumbentScore * cfg.DisplacementHoldMargin;
        }

        internal static bool CanDisplaceFromSolveCell(
            PlantRequirements challenger,
            Block incumbentBlock,
            BlockPos targetPos,
            in SpreadSolveCell cell,
            bool harshClimate,
            float seasonSpreadMult,
            out float challengerScore,
            out float incumbentScore)
        {
            challengerScore = 0f;
            incumbentScore = float.MaxValue;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.UseCellDisplacement || challenger == null || incumbentBlock == null)
            {
                return false;
            }

            if (!PlantCodeHelper.IsEcologySpreadParent(incumbentBlock)) return false;
            if (PlantCodeHelper.IsArborealHostBlock(incumbentBlock)) return false;

            string incumbentSpecies = PlantCodeHelper.ResolveEcologySpecies(incumbentBlock);
            if (incumbentSpecies != null && incumbentSpecies == challenger.Species) return false;

            EnvironmentalContext ctx = EnvironmentalContext.FromSpreadSolveCell(in cell);
            if (!SuitabilityEvaluator.CanCompeteForCell(challenger, ctx, harshClimate, occupied: true))
            {
                return false;
            }

            challengerScore = SpreadClimateFitnessFromSolveCell(
                challenger, targetPos, in cell, harshClimate, occupied: true);
            challengerScore = MeadowTurfCompetition.AdjustChallengerSpreadScore(
                challengerScore,
                challenger.Species,
                incumbentSpecies);

            PlantRequirements incumbent = PlantRequirements.FromBlock(incumbentBlock);
            if (string.IsNullOrEmpty(incumbent.Species))
            {
                incumbentScore = float.MaxValue;
            }
            else
            {
                incumbentScore = IncumbentHoldScoreFromContext(null, incumbent, targetPos, harshClimate, ctx);
                incumbentScore *= cell.TrafficFitnessMult;
            }

            if (challengerScore <= 0f) return false;

            return challengerScore >= incumbentScore * cfg.DisplacementHoldMargin;
        }
    }
}
