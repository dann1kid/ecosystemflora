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

            if (!MeetsRainfall(req, ctx)) return false;
            if (!MeetsLocalForest(req, ctx)) return false;

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

            string rainfall = DescribeRainfallFailure(req, ctx);
            if (rainfall != null) return rainfall;

            string forest = DescribeLocalForestFailure(req, ctx);
            if (forest != null) return forest;

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

            if (!MeetsRainfall(req, ctx)) return 0f;
            score = CombineFitness(score, RangeFitness(ctx.WorldgenRainfall, req.MinRain, req.MaxRain));

            if (!MeetsLocalForest(req, ctx)) return 0f;
            score = CombineFitness(score, RangeFitness(ctx.LocalForestCover, req.MinForest, req.MaxForest));

            return score;
        }

        /// <summary>
        /// Spread fitness: parent is alive — no seasonal temp gate; rainfall + local forest cover apply.
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

            if (!MeetsRainfall(req, ctx)) return 0f;
            if (!MeetsLocalForest(req, ctx)) return 0f;

            float score = 1f;
            if (EcosystemConfig.Loaded.ApplyWorldgenRainForest)
            {
                score = CombineFitness(score, RangeFitness(ctx.WorldgenRainfall, req.MinRain, req.MaxRain));
            }

            score = CombineFitness(score, RangeFitness(ctx.LocalForestCover, req.MinForest, req.MaxForest));
            return score;
        }

        /// <summary>Weakest factor wins — avoids 0.35³ &lt; MinFitness on otherwise valid edge cells.</summary>
        static float CombineFitness(float current, float factor) => System.Math.Min(current, factor);

        /// <summary>Physical + climate suitability for spread target (empty or occupied).</summary>
        public static bool CanCompeteForCell(
            PlantRequirements req,
            IEnvironmentalContext ctx,
            bool harshClimate,
            bool occupied)
        {
            if (req.Habitat == EcologyHabitat.TerrestrialTree
                || req.Habitat == EcologyHabitat.WaterSurface
                || req.Habitat == EcologyHabitat.ReedNearWater
                || req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                return CanReproduce(req, ctx, harshClimate);
            }

            if (ctx.TouchesFluid) return false;
            if (!ctx.GroundSideSolid) return false;
            if (!SoilClassification.MeetsSoilRequirements(req, ctx.GroundSoilKinds, ctx.GroundFertility)) return false;

            if (!occupied && ctx.SpaceReplaceable < ReproduceMinReplaceable) return false;

            if (!MeetsRainfall(req, ctx)) return false;
            if (!MeetsLocalForest(req, ctx)) return false;

            return true;
        }

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

            if (!MeetsRainfall(req, ctx)) return false;
            if (!MeetsLocalForest(req, ctx)) return false;

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

                string climate = DescribeClimateFailure(req, ctx);
                if (climate != null) return climate;

                return null;
            }

            if (req.Habitat == EcologyHabitat.ReedNearWater)
            {
                if (ctx.SpaceReplaceable < ReproduceMinReplaceable)
                {
                    return "Space blocked (replaceable " + ctx.SpaceReplaceable + ", need " + ReproduceMinReplaceable + ").";
                }

                if (!ctx.HasShallowWater) return "No gravel bed or wrong water depth.";

                string climate = DescribeClimateFailure(req, ctx);
                if (climate != null) return climate;

                return null;
            }

            if (req.Habitat == EcologyHabitat.UnderwaterColumn)
            {
                if (!ctx.HasShallowWater) return "Water column too shallow/deep or no bed.";

                string climate = DescribeClimateFailure(req, ctx);
                if (climate != null) return climate;

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

            string climateTerrestrial = DescribeClimateFailure(req, ctx);
            if (climateTerrestrial != null) return climateTerrestrial;

            return null;
        }

        static string DescribeClimateFailure(PlantRequirements req, IEnvironmentalContext ctx)
        {
            string rainfall = DescribeRainfallFailure(req, ctx);
            if (rainfall != null) return rainfall;

            return DescribeLocalForestFailure(req, ctx);
        }

        static bool MeetsRainfall(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (!EcosystemConfig.Loaded.ApplyWorldgenRainForest) return true;
            if (!ctx.HasClimate) return false;
            return InRange(ctx.WorldgenRainfall, req.MinRain, req.MaxRain);
        }

        static bool MeetsLocalForest(PlantRequirements req, IEnvironmentalContext ctx)
        {
            return InRange(ctx.LocalForestCover, req.MinForest, req.MaxForest);
        }

        static string DescribeRainfallFailure(PlantRequirements req, IEnvironmentalContext ctx)
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

            return null;
        }

        static string DescribeLocalForestFailure(PlantRequirements req, IEnvironmentalContext ctx)
        {
            if (ctx.LocalForestCover < req.MinForest)
            {
                return "Local forest too sparse (" + ctx.LocalForestCover.ToString("0.00") + " < " + req.MinForest.ToString("0.00") + ").";
            }

            if (ctx.LocalForestCover > req.MaxForest)
            {
                return "Local forest too dense (" + ctx.LocalForestCover.ToString("0.00") + " > " + req.MaxForest.ToString("0.00") + ").";
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
