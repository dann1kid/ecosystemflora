using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Ephemeral soil state derived from block + one succession event (not stored in RAM).</summary>
    public struct WildSoilComposition
    {
        public int FertilityPoints;
        public float Moisture;

        public SoilFertilityTier FertilityTier =>
            SoilFertilityTierExtensions.FromFertilityPoints(FertilityPoints);

        public static WildSoilComposition FromBlock(ICoreAPI api, Block ground, BlockPos groundPos)
        {
            int points = ground != null && ground.Id != 0 ? Math.Max(0, (int)ground.Fertility) : 100;

            MoistureLevel moisture = MoistureLevel.Mesic;
            if (api != null && groundPos != null)
            {
                NicheSampler niche = EcosystemSystem.Instance?.Niche;
                if (niche != null)
                {
                    moisture = niche.GetNiche(api, groundPos.UpCopy()).Moisture;
                }
            }

            WildSoilComposition c = new WildSoilComposition
            {
                FertilityPoints = points,
                Moisture = MoistureLevelToPercent(moisture),
            };

            string path = ground?.Code?.Path ?? "";
            if (path == "peat")
            {
                c.Moisture = Math.Max(c.Moisture, 85f);
            }

            return c;
        }

        public void ApplyImpact(SoilImpact impact, float strength)
        {
            Moisture = Clamp(Moisture + impact.MoistureDelta * strength, 0f, 100f);

            if (impact.FertilityTierDelta != 0f)
            {
                int deltaPoints = (int)Math.Round(impact.FertilityTierDelta * strength * 25f);
                if (deltaPoints == 0)
                {
                    float d = impact.FertilityTierDelta * strength;
                    if (d > 0.12f) deltaPoints = 1;
                    else if (d < -0.12f) deltaPoints = -1;
                }

                FertilityPoints = ClampInt(FertilityPoints + deltaPoints, 5, 320);
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

        static int ClampInt(int v, int min, int max)
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

            return FromFertilityPoints((int)ground.Fertility);
        }

        public static SoilFertilityTier FromFertilityPoints(int f)
        {
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
