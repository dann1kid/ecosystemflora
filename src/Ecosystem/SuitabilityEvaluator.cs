namespace WildFarming.Ecosystem

{

    public static class SuitabilityEvaluator

    {

        public const int ReproduceMinReplaceable = 5000;



        /// <summary>Can an established plant survive here (climate + soil under roots).</summary>

        public static bool MeetsSurvivalRequirements(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)

        {

            if (!ctx.HasClimate) return false;

            if (!ctx.GroundSideSolid) return false;

            if (ctx.GroundFertility < req.MinFertility) return false;



            if (harshClimate && !ctx.InGreenhouse)

            {

                if (ctx.Temperature < req.MinTemp || ctx.Temperature > req.MaxTemp) return false;

            }



            return true;

        }



        /// <summary>Can the player place a seed on this cell (empty enough space above ground).</summary>

        public static bool MeetsPlacementRequirements(PlantRequirements req, IEnvironmentalContext ctx)

        {

            if (!ctx.GroundSideSolid) return false;

            if (ctx.GroundFertility < req.MinFertility) return false;

            if (ctx.SpaceReplaceable < req.MinReplaceable) return false;

            return true;

        }



        public static string DescribeSurvivalFailure(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)

        {

            if (!ctx.HasClimate) return "No climate data.";

            if (!ctx.GroundSideSolid) return "No solid ground below.";

            if (ctx.GroundFertility < req.MinFertility) return "Soil not fertile enough.";

            if (harshClimate && !ctx.InGreenhouse)

            {

                if (ctx.Temperature > req.MaxTemp) return "Too hot.";

                if (ctx.Temperature < req.MinTemp) return "Too cold.";

            }

            return null;

        }



        public static float Score(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)

        {

            if (!MeetsSurvivalRequirements(req, ctx, harshClimate)) return 0f;



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



        /// <summary>Can a juvenile be placed here. Parent already proved the area — only physical cell checks.</summary>

        public static bool CanReproduce(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)

        {

            if (ctx.TouchesFluid) return false;

            if (!ctx.GroundSideSolid) return false;

            if (ctx.GroundFertility < req.MinFertility) return false;

            if (ctx.SpaceReplaceable < ReproduceMinReplaceable) return false;

            return true;

        }



        public static string DescribeReproduceFailure(PlantRequirements req, IEnvironmentalContext ctx, bool harshClimate)

        {

            if (ctx.TouchesFluid) return "Underwater or fluid at cell.";

            if (!ctx.GroundSideSolid) return "No solid ground below.";

            if (ctx.GroundFertility < req.MinFertility) return "Soil not fertile enough.";

            if (ctx.SpaceReplaceable < ReproduceMinReplaceable)

            {

                return "Space blocked (replaceable " + ctx.SpaceReplaceable + ", need " + ReproduceMinReplaceable + ").";

            }

            return null;

        }

    }

}


