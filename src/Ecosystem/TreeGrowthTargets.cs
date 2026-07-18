namespace WildFarming.Ecosystem
{
    internal static class TreeGrowthTargets
    {
        public static float TrunkVsReference(int trunkHeight, in WildTreeGrowthProfiles.Profile profile)
        {
            if (profile.ReferenceTrunkHeight <= 0) return 1f;
            return trunkHeight / (float)profile.ReferenceTrunkHeight;
        }

        public static float CrownVsReference(int crownRadius, in WildTreeGrowthProfiles.Profile profile)
        {
            if (profile.ReferenceCrownRadius <= 0) return 1f;
            return crownRadius / (float)profile.ReferenceCrownRadius;
        }

        /// <summary>Structure vs typical worldgen mature; may exceed 1 — not used for senescence.</summary>
        public static float SizeIndexFraction(
            int trunkHeight,
            int crownRadius,
            in WildTreeGrowthProfiles.Profile profile)
        {
            float trunk = TrunkVsReference(trunkHeight, profile);
            float crown = CrownVsReference(crownRadius, profile);
            return trunk * 0.55f + crown * 0.45f;
        }

        /// <summary>
        /// True when the crown is behind the trunk / species soft target — yearly growth should
        /// favour branchy spread over more height (avoids pine-pole oaks after sapling treegen).
        /// </summary>
        public static bool CrownLagsTrunk(
            float trunkVsReference,
            float crownVsReference)
        {
            if (crownVsReference >= 1f) return false;
            if (trunkVsReference >= 0.55f && crownVsReference < trunkVsReference - 0.1f) return true;
            if (trunkVsReference >= 0.7f && crownVsReference < 0.85f) return true;
            return false;
        }

        public static int SizeIndexPercent(
            int trunkHeight,
            int crownRadius,
            in WildTreeGrowthProfiles.Profile profile)
        {
            return (int)System.Math.Round(SizeIndexFraction(trunkHeight, crownRadius, profile) * 100.0);
        }
    }
}

