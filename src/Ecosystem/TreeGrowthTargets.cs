namespace WildFarming.Ecosystem
{
    internal static class TreeGrowthTargets
    {
        public static int MaxTargetTrunkHeight(in WildTreeGrowthProfiles.Profile profile, float scale)
        {
            int height = (int)(profile.MaxTrunkHeight * scale);
            if (height < 2) height = 2;
            return height;
        }

        public static int MaxTargetCrownRadius(in WildTreeGrowthProfiles.Profile profile, float scale)
        {
            int radius = (int)(profile.MaxCrownRadius * scale);
            if (radius < 1) radius = 1;
            return radius;
        }

        public static float TrunkFraction(int trunkHeight, in WildTreeGrowthProfiles.Profile profile)
        {
            if (profile.MaxTrunkHeight <= 0) return 1f;
            return Clamp01(trunkHeight / (float)profile.MaxTrunkHeight);
        }

        public static float CrownFraction(int crownRadius, in WildTreeGrowthProfiles.Profile profile)
        {
            if (profile.MaxCrownRadius <= 0) return 1f;
            return Clamp01(crownRadius / (float)profile.MaxCrownRadius);
        }

        /// <summary>0–1 maturation index: 55% trunk blocks + 45% crown radius vs species max.</summary>
        public static float MaturityFraction(
            int trunkHeight,
            int crownRadius,
            in WildTreeGrowthProfiles.Profile profile)
        {
            float trunk = TrunkFraction(trunkHeight, profile);
            float crown = CrownFraction(crownRadius, profile);
            return Clamp01(trunk * 0.55f + crown * 0.45f);
        }

        public static int MaturityPercent(
            int trunkHeight,
            int crownRadius,
            in WildTreeGrowthProfiles.Profile profile)
        {
            return (int)System.Math.Round(MaturityFraction(trunkHeight, crownRadius, profile) * 100.0);
        }

        static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
