namespace WildFarming.Ecosystem
{
    /// <summary>Global wild-flora spread pacing applied at runtime (ecology tables stay canonical).</summary>
    internal static class WildSpreadBalance
    {
        /// <summary>Default when <see cref="EcosystemConfig.SpeciesSpreadRateScale"/> is unset on a fresh config instance.</summary>
        public const float DefaultSpeciesSpreadRateScale = 1f / 3f;

        public static float ScaleSpeciesSpreadRate(string species, float spreadRate, EcosystemConfig cfg = null)
        {
            if (spreadRate <= 0f || string.IsNullOrEmpty(species)) return spreadRate;

            cfg ??= EcosystemConfig.Loaded;
            float scale = cfg?.SpeciesSpreadRateScale ?? DefaultSpeciesSpreadRateScale;
            if (scale <= 0f) return 0f;

            return spreadRate * scale;
        }
    }
}
