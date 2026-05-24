using System;

using Vintagestory.API.Common;



namespace WildFarming.Ecosystem

{

    /// <summary>Wild-soil state (not farmland BE): moisture, fertility tier, forest-floor flag.</summary>

    public struct WildSoilComposition

    {

        public float Moisture;

        public SoilFertilityTier FertilityTier;

        /// <summary>Fractional progress toward the next tier (0–1).</summary>

        public float TierProgress;

        public bool IsForestFloor;



        public static WildSoilComposition FromBlock(Block ground, MoistureLevel nicheMoisture)

        {

            WildSoilComposition c = new WildSoilComposition

            {

                FertilityTier = SoilFertilityTierExtensions.FromBlockFertility(ground),

                TierProgress = 0f,

            };



            string path = ground?.Code?.Path ?? "";

            if (path.StartsWith("forestfloor"))

            {

                c.IsForestFloor = true;

            }

            c.Moisture = MoistureLevelToPercent(nicheMoisture);
            if (path == "peat")
            {
                c.Moisture = System.Math.Max(c.Moisture, 85f);
            }

            return c;

        }



        public void ApplyImpact(SoilImpact impact, float strength)

        {

            if (impact.IsForestFloor) IsForestFloor = true;



            Moisture = Clamp(Moisture + impact.MoistureDelta * strength, 0f, 100f);



            if (impact.FertilityTierDelta != 0f)

            {

                float tierValue = (float)FertilityTier + TierProgress + impact.FertilityTierDelta * strength;

                int minTier = (int)SoilFertilityTier.Barren;

                int maxTier = (int)SoilFertilityTier.High;

                int newTier = (int)Math.Floor(tierValue);

                if (newTier < minTier)

                {

                    newTier = minTier;

                    tierValue = newTier;

                }

                else if (newTier > maxTier)

                {

                    newTier = maxTier;

                    tierValue = newTier;

                }



                FertilityTier = (SoilFertilityTier)newTier;

                TierProgress = tierValue - newTier;

                if (TierProgress < 0f) TierProgress = 0f;

                if (TierProgress > 0.999f) TierProgress = 0.999f;

            }

        }



        static float MoistureLevelToPercent(MoistureLevel level)

        {

            switch (level)

            {

                case MoistureLevel.Dry: return 15f;

                case MoistureLevel.Mesic: return 45f;

                case MoistureLevel.Shoreline: return 65f;

                case MoistureLevel.Wet: return 85f;

                default: return 45f;

            }

        }



        static float Clamp(float v, float min, float max)

        {

            if (v < min) return min;

            if (v > max) return max;

            return v;

        }

    }



    public enum SoilFertilityTier

    {

        Barren = 0,

        VeryLow = 1,

        Low = 2,

        Medium = 3,

        Compost = 4,

        High = 5,

    }



    public static class SoilFertilityTierExtensions

    {

        public static SoilFertilityTier FromBlockFertility(Block ground)

        {

            if (ground == null || ground.Id == 0) return SoilFertilityTier.Barren;



            string path = ground.Code?.Path ?? "";

            if (path.StartsWith("forestfloor")) return SoilFertilityTier.Low;

            if (path == "peat") return SoilFertilityTier.VeryLow;



            int f = (int)ground.Fertility;

            if (f <= 0) return SoilFertilityTier.Barren;

            if (f < 120) return SoilFertilityTier.VeryLow;

            if (f < 170) return SoilFertilityTier.Low;

            if (f < 230) return SoilFertilityTier.Medium;

            if (f < 280) return SoilFertilityTier.Compost;

            return SoilFertilityTier.High;

        }



        public static string ToSoilPathSegment(SoilFertilityTier tier)

        {

            switch (tier)

            {

                case SoilFertilityTier.Barren:

                case SoilFertilityTier.VeryLow: return "verylow";

                case SoilFertilityTier.Low: return "low";

                case SoilFertilityTier.Medium: return "medium";

                case SoilFertilityTier.Compost: return "compost";

                case SoilFertilityTier.High: return "high";

                default: return "medium";

            }

        }

    }

}

