namespace WildFarming.Ecosystem
{
    internal static class TreeGrowthTargets
    {
        /// <summary>
        /// Maps structure fill (0–1 of species max) to age. Exponent &gt; 1 so typical worldgen trees
        /// (~60–70% of max size) read as young-mature (~30–45 y), not near MaxAgeYears.
        /// </summary>
        const float StructureToAgeExponent = 2.15f;

        public static int TargetTrunkHeight(int ageYears, in WildTreeGrowthProfiles.Profile profile, float scale)
        {
            float t = GrowthFraction(ageYears, profile.MaxAgeYears);
            int height = (int)(profile.MaxTrunkHeight * t * scale);
            if (height < 2) height = 2;
            return height;
        }

        public static int TargetCrownRadius(int ageYears, in WildTreeGrowthProfiles.Profile profile, float scale)
        {
            float t = GrowthFraction(ageYears, profile.MaxAgeYears);
            int radius = (int)(profile.MaxCrownRadius * t * scale);
            if (radius < 1) radius = 1;
            return radius;
        }

        public static int EstimateAgeYears(
            int trunkHeight,
            int crownRadius,
            in WildTreeGrowthProfiles.Profile profile)
        {
            if (profile.MaxTrunkHeight <= 0 || profile.MaxAgeYears <= 0) return 1;

            float heightFrac = trunkHeight / (float)profile.MaxTrunkHeight;
            float spreadFrac = profile.MaxCrownRadius > 0
                ? crownRadius / (float)profile.MaxCrownRadius
                : heightFrac;

            if (heightFrac < 0f) heightFrac = 0f;
            if (heightFrac > 1f) heightFrac = 1f;
            if (spreadFrac < 0f) spreadFrac = 0f;
            if (spreadFrac > 1f) spreadFrac = 1f;

            float maturity = heightFrac * 0.55f + spreadFrac * 0.45f;
            float ageFraction = (float)System.Math.Pow(maturity, StructureToAgeExponent);
            int age = (int)(ageFraction * profile.MaxAgeYears + 0.5f);
            if (age < 1) age = 1;
            if (age > profile.MaxAgeYears) age = profile.MaxAgeYears;
            return age;
        }

        public static float GrowthFraction(int ageYears, int maxAgeYears)
        {
            if (maxAgeYears <= 0) return 1f;
            float t = ageYears / (float)maxAgeYears;
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return t * t * (3f - 2f * t);
        }
    }
}
