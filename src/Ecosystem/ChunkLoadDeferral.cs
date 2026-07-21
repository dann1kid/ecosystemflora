using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Spreads per-column deferred work after mass chunk load so strip/remap/mycelium/reg
    /// do not all fire on the same frames (high view-distance hitch).
    /// </summary>
    internal static class ChunkLoadDeferral
    {
        public const int StripBaseMs = 200;
        public const int StripStaggerWindowMs = 1500;

        public const int RemapBaseMs = 250;
        public const int RemapStaggerWindowMs = 2000;

        public const int MyceliumBaseMs = 800;
        public const int MyceliumStaggerWindowMs = 2000;

        public const int RegistrationBaseMs = 500;
        public const int RegistrationStaggerWindowMs = 1500;

        /// <summary>
        /// Stable delay in <c>[baseMs, baseMs + staggerWindowMs)</c> for a chunk column.
        /// </summary>
        public static int DelayMs(Vec2i chunkCoord, int baseMs, int staggerWindowMs)
        {
            if (baseMs < 0) baseMs = 0;
            if (staggerWindowMs <= 0) return baseMs;

            int x = chunkCoord?.X ?? 0;
            int z = chunkCoord?.Y ?? 0;
            // Knuth multiplicative hash — stable across sessions, spreads adjacent columns.
            uint h = unchecked((uint)(x * 73856093) ^ (uint)(z * 19349663));
            return baseMs + (int)(h % (uint)staggerWindowMs);
        }

        public static int StripDelayMs(Vec2i chunkCoord) =>
            DelayMs(chunkCoord, StripBaseMs, StripStaggerWindowMs);

        public static int RemapDelayMs(Vec2i chunkCoord) =>
            DelayMs(chunkCoord, RemapBaseMs, RemapStaggerWindowMs);

        public static int MyceliumDelayMs(Vec2i chunkCoord) =>
            DelayMs(chunkCoord, MyceliumBaseMs, MyceliumStaggerWindowMs);

        public static int RegistrationDelayMs(Vec2i chunkCoord) =>
            DelayMs(chunkCoord, RegistrationBaseMs, RegistrationStaggerWindowMs);

        /// <summary>
        /// Load-time burst only when classify runs sync on main.
        /// With background scan, paced snapshot on the scan tick is enough.
        /// </summary>
        public static bool ShouldBurstOnLoad(EcosystemConfig cfg) =>
            cfg != null
            && cfg.EnableBurstRegistrationNearPlayers
            && !cfg.EnableBackgroundRegistrationScan;
    }
}
