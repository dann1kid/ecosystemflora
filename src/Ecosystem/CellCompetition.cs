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

        internal static float SpreadScoreFromContext(
            ICoreAPI api,
            PlantRequirements challenger,
            BlockPos targetPos,
            bool harshClimate,
            EnvironmentalContext ctx)
        {
            if (!SuitabilityEvaluator.CanCompeteForCell(challenger, ctx, harshClimate, occupied: false))
            {
                return 0f;
            }

            float fitness = SuitabilityEvaluator.ReproduceFitness(challenger, ctx);
            fitness = EcologySpreadFitness.ApplyContext(api, challenger, targetPos, fitness);
            fitness = EcologySpreadFitness.ApplyNiche(api, challenger, targetPos, fitness);
            fitness *= SeasonEcology.SpreadActivityMultiplier(api, targetPos, challenger);
            if (challenger.SpreadRate > 0f)
            {
                fitness *= challenger.SpreadRate;
            }

            return fitness;
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

            challengerScore = SpreadScoreFromContext(api, challenger, targetPos, harshClimate, ctx);

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
    }
}
