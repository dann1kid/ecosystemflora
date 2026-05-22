namespace WildFarming.Ecosystem
{
    public static class SuitabilityEvaluator
    {
        /// <summary>Soil, space, and climate range — used for player planting and maturation.</summary>
        public static bool MeetsHardRequirements(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (!ctx.HasClimate) return false;
            if (!ctx.GroundSideSolid) return false;
            if (ctx.GroundFertility < req.MinFertility) return false;
            if (ctx.SpaceReplaceable < req.MinReplaceable) return false;

            if (harshClimate && !ctx.InGreenhouse)
            {
                if (ctx.Temperature < req.MinTemp || ctx.Temperature > req.MaxTemp) return false;
            }

            return true;
        }

        public static float Score(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (!MeetsHardRequirements(req, ctx, harshClimate)) return 0f;

            float score = 1f;

            if (harshClimate && !ctx.InGreenhouse)
            {
                float range = req.MaxTemp - req.MinTemp;
                if (range > 0.01f)
                {
                    float mid = (req.MinTemp + req.MaxTemp) * 0.5f;
                    float dist = System.Math.Abs(ctx.Temperature - mid) / (range * 0.5f);
                    score *= System.Math.Max(0.35f, 1f - dist * 0.35f);
                }
            }

            return score;
        }

        /// <summary>Spontaneous reproduction — stricter fitness threshold.</summary>
        public static bool CanReproduce(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate, float minFitness)
        {
            return Score(req, ctx, harshClimate) >= minFitness;
        }
    }
}
