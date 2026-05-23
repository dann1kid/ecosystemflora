namespace WildFarming.Ecosystem
{
    public static class SuitabilityEvaluator
    {
        public const int ReproduceMinReplaceable = 5000;

        public static bool MeetsSurvivalRequirements(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (!ctx.HasClimate) return false;
            if (!ctx.GroundSideSolid) return false;
            if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return false;

            if (harshClimate && !ctx.InGreenhouse)
            {
                if (ctx.Temperature < req.MinTemp || ctx.Temperature > req.MaxTemp) return false;
            }

            if (!MeetsWorldgenRainForest(req, ctx)) return false;

            return true;
        }

        public static bool MeetsPlacementRequirements(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (!ctx.GroundSideSolid) return false;
            if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return false;
            if (ctx.SpaceReplaceable < req.MinReplaceable) return false;
            return true;
        }

        public static string DescribeSurvivalFailure(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (!ctx.HasClimate) return "No climate data.";
            if (!ctx.GroundSideSolid) return "No solid ground below.";
            string soil = SoilClassification.DescribeSoilFailure(req, ctx.GroundSoilKinds, ctx.GroundFertility);
            if (soil != null) return soil;

            if (harshClimate && !ctx.InGreenhouse)
            {
                if (ctx.Temperature > req.MaxTemp) return "Too hot.";
                if (ctx.Temperature < req.MinTemp) return "Too cold.";
            }

            string rainForest = DescribeRainForestFailure(req, ctx);
            if (rainForest != null) return rainForest;

            return null;
        }

        public static float Score(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (!ctx.HasClimate) return 0f;
            if (!ctx.GroundSideSolid) return 0f;
            if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return 0f;
            if (ctx.TouchesFluid) return 0f;

            float score = 1f;

            if (harshClimate && !ctx.InGreenhouse)
            {
                if (ctx.Temperature < req.MinTemp || ctx.Temperature > req.MaxTemp) return 0f;
                score = CombineFitness(score, RangeFitness(ctx.Temperature, req.MinTemp, req.MaxTemp));
            }

            if (EcosystemConfig.Loaded.ApplyWorldgenRainForest)
            {
                if (!MeetsWorldgenRainForest(req, ctx)) return 0f;
                score = CombineFitness(score, RangeFitness(ctx.WorldgenRainfall, req.MinRain, req.MaxRain));
                score = CombineFitness(score, RangeFitness(ctx.ForestDensity, req.MinForest, req.MaxForest));
            }

            return score;
        }

        /// <summary>
        /// Spread fitness: parent is alive — no seasonal temp gate; rain/forest still apply.
        /// </summary>
        public static float ReproduceFitness(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (!ctx.HasClimate) return 0f;

            if (req.Habitat == EcologyHabitat.WaterSurface
                || req.Habitat == EcologyHabitat.ReedNearWater
                || req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                if (!ctx.HasShallowWater) return 0f;
            }
            else
            {
                if (!ctx.GroundSideSolid) return 0f;
                if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return 0f;
                if (ctx.TouchesFluid) return 0f;
            }

            if (!EcosystemConfig.Loaded.ApplyWorldgenRainForest) return 1f;
            if (!MeetsWorldgenRainForest(req, ctx)) return 0f;

            float score = 1f;
            score = CombineFitness(score, RangeFitness(ctx.WorldgenRainfall, req.MinRain, req.MaxRain));
            score = CombineFitness(score, RangeFitness(ctx.ForestDensity, req.MinForest, req.MaxForest));
            return score;
        }

        /// <summary>Weakest factor wins — avoids 0.35³ &lt; MinFitness on otherwise valid edge cells.</summary>
        static float CombineFitness(float current, float factor) => System.Math.Min(current, factor);

        /// <summary>Can a juvenile be placed here. Parent already proved the area — physical + biome map checks.</summary>
        public static bool CanReproduce(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (req.Habitat == EcologyHabitat.WaterSurface)
            {
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable) return false;
                if (!ctx.HasShallowWater) return false;
            }
            else if (req.Habitat == EcologyHabitat.ReedNearWater)
            {
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable) return false;
                if (!ctx.HasShallowWater) return false;
            }
            else if (req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                if (!ctx.HasShallowWater) return false;
            }
            else
            {
                if (ctx.TouchesFluid) return false;
                if (!ctx.GroundSideSolid) return false;
                if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return false;
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable) return false;
            }

            if (!MeetsWorldgenRainForest(req, ctx)) return false;

            return true;
        }

        public static string DescribeReproduceFailure(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)
        {
            if (req.Habitat == EcologyHabitat.WaterSurface)
            {
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable)
                {
                    return "Space blocked (replaceable " + ctx.SpaceReplaceable + ", need " + ReproduceMinReplaceable + ").";
                }

                if (!ctx.HasShallowWater) return "No water surface below.";

                string rainForestLily = DescribeRainForestFailure(req, ctx);
                if (rainForestLily != null) return rainForestLily;

                return null;
            }

            if (req.Habitat == EcologyHabitat.ReedNearWater)
            {
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable)
                {
                    return "Space blocked (replaceable " + ctx.SpaceReplaceable + ", need " + ReproduceMinReplaceable + ").";
                }

                if (!ctx.HasShallowWater) return "No muddy gravel or wrong water depth.";

                string rainForestReed = DescribeRainForestFailure(req, ctx);
                if (rainForestReed != null) return rainForestReed;

                return null;
            }

            if (req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                if (!ctx.HasShallowWater) return "Water column too shallow/deep or no bed.";

                string rainForestCrowfoot = DescribeRainForestFailure(req, ctx);
                if (rainForestCrowfoot != null) return rainForestCrowfoot;

                return null;
            }

            if (ctx.TouchesFluid) return "Underwater or fluid at cell.";
            if (!ctx.GroundSideSolid) return "No solid ground below.";
            string soil = SoilClassification.DescribeSoilFailure(req, ctx.GroundSoilKinds, ctx.GroundFertility);
            if (soil != null) return soil;
            if (ctx.SpaceReplaceable < ReproduceMinReplaceable)
            {
                return "Space blocked (replaceable " + ctx.SpaceReplaceable + ", need " + ReproduceMinReplaceable + ").";
            }

            string rainForestTerrestrial = DescribeRainForestFailure(req, ctx);
            if (rainForestTerrestrial != null) return rainForestTerrestrial;

            return null;
        }

        static bool MeetsWorldgenRainForest(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (!EcosystemConfig.Loaded.ApplyWorldgenRainForest) return true;
            if (!ctx.HasClimate) return false;
            return InRange(ctx.WorldgenRainfall, req.MinRain, req.MaxRain)
                && InRange(ctx.ForestDensity, req.MinForest, req.MaxForest);
        }

        static string DescribeRainForestFailure(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (!EcosystemConfig.Loaded.ApplyWorldgenRainForest) return null;
            if (!ctx.HasClimate) return "No climate data.";

            if (ctx.WorldgenRainfall < req.MinRain)
            {
                return "Rainfall too low (" + ctx.WorldgenRainfall.ToString("0.00") + " < " + req.MinRain.ToString("0.00") + ").";
            }

            if (ctx.WorldgenRainfall > req.MaxRain)
            {
                return "Rainfall too high (" + ctx.WorldgenRainfall.ToString("0.00") + " > " + req.MaxRain.ToString("0.00") + ").";
            }

            if (ctx.ForestDensity < req.MinForest)
            {
                return "Forest too sparse (" + ctx.ForestDensity.ToString("0.00") + " < " + req.MinForest.ToString("0.00") + ").";
            }

            if (ctx.ForestDensity > req.MaxForest)
            {
                return "Forest too dense (" + ctx.ForestDensity.ToString("0.00") + " > " + req.MaxForest.ToString("0.00") + ").";
            }

            return null;
        }

        static bool InRange(float value, float min, float max) => value >= min && value <= max;

        static float RangeFitness(float value, float min, float max)
        {
            float range = max - min;
            if (range <= 0.01f) return 1f;

            float mid = (min + max) * 0.5f;
            float dist = System.Math.Abs(value - mid) / (range * 0.5f);
            return System.Math.Max(0.35f, 1f - dist * 0.35f);
        }
    }
}
