namespace WildFarming.Ecosystem
{
    /// <summary>Global wild-flora spread pacing applied at runtime (ecology tables stay canonical).</summary>
    internal static class WildSpreadBalance
    {
        /// <summary>Runtime multiplier on ecology <see cref="PlantRequirements.SpreadRate"/> (3× slower reproduction).</summary>
        public const float SpeciesSpreadScale = 1f / 3f;

        public static float ScaleSpeciesSpreadRate(string species, float spreadRate)
        {
            if (spreadRate <= 0f || string.IsNullOrEmpty(species)) return spreadRate;
            if (IsExempt(species)) return spreadRate;
            return spreadRate * SpeciesSpreadScale;
        }

        static bool IsExempt(string species) => EcologyShoreSedgeSpecies.IsKnown(species);
    }
}
