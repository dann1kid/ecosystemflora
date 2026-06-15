namespace WildFarming.Ecosystem
{
    /// <summary>How deciduous canopy foliage is synchronized to the game calendar.</summary>
    public enum FoliageSyncMode
    {
        /// <summary>Chunk column pass with resume cursor (default v3.4).</summary>
        Chunk = 0,

        /// <summary>Chunk sync plus small random tick for live transitions.</summary>
        Hybrid = 1,

        /// <summary>Legacy v3.3 global index + random tick + patchy catch-up.</summary>
        Random = 2,
    }

    internal static class FoliageSyncModeHelper
    {
        public static FoliageSyncMode Resolve(EcosystemConfig cfg)
        {
            if (cfg == null) return FoliageSyncMode.Chunk;

            string raw = cfg.FoliageSyncMode;
            if (string.IsNullOrWhiteSpace(raw)) return FoliageSyncMode.Chunk;

            switch (raw.Trim().ToLowerInvariant())
            {
                case "chunk":
                case "chunksync":
                    return FoliageSyncMode.Chunk;
                case "hybrid":
                    return FoliageSyncMode.Hybrid;
                case "random":
                case "legacy":
                    return FoliageSyncMode.Random;
                default:
                    return FoliageSyncMode.Chunk;
            }
        }

        public static bool UsesChunkSync(EcosystemConfig cfg) =>
            Resolve(cfg) != FoliageSyncMode.Random;

        public static bool UsesRandomTick(EcosystemConfig cfg) =>
            Resolve(cfg) == FoliageSyncMode.Random
            || (Resolve(cfg) == FoliageSyncMode.Hybrid && cfg.MaxFoliageCellsTickedPerTick > 0);
    }
}
