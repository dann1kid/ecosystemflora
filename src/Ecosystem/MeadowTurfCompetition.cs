namespace WildFarming.Ecosystem
{
    /// <summary>Meadow flower ↔ tallgrass matrix displacement tuning (not grass colonizers).</summary>
    internal static class MeadowTurfCompetition
    {
        /// <summary>Multiplier on challenger spread score when a meadow flower displaces tallgrass turf.</summary>
        public const float FlowerVsTallgrassSpreadBonus = 1.32f;

        /// <summary>Multiplier when tallgrass tries to displace an established meadow flower.</summary>
        public const float TallgrassVsFlowerSpreadPenalty = 0.72f;

        public static float AdjustChallengerSpreadScore(
            float score,
            string challengerSpecies,
            string incumbentSpecies)
        {
            if (score <= 0f
                || string.IsNullOrEmpty(challengerSpecies)
                || string.IsNullOrEmpty(incumbentSpecies))
            {
                return score;
            }

            if (incumbentSpecies == "tallgrass"
                && IsMeadowFlower(challengerSpecies))
            {
                return score * FlowerVsTallgrassSpreadBonus;
            }

            if (challengerSpecies == "tallgrass"
                && IsMeadowFlower(incumbentSpecies))
            {
                return score * TallgrassVsFlowerSpreadPenalty;
            }

            return score;
        }

        static bool IsMeadowFlower(string species) =>
            EcologyFlowerSpecies.IsKnownFlower(species)
            && !EcologyGrassColonizerSpecies.IsKnown(species);
    }
}
