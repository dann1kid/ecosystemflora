namespace WildFarming.Ecosystem
{
    /// <summary>Mutable per-chunk foliage pass counters (main thread only).</summary>
    internal sealed class FoliageChunkPassState
    {
        public bool OrphanPruneOnly;
        public int OrphanChecksRemaining;
        public bool FireSeenInChunk;
    }
}
