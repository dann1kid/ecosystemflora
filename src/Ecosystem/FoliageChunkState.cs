namespace WildFarming.Ecosystem
{
    internal struct FoliageChunkState
    {
        public int SyncedSeasonKey;
        public int ResumeLx;
        public int ResumeLz;
        /// <summary>-1 = start column pass from map top.</summary>
        public int ResumeY;
        public bool Completed;
        public int LastIndexed;
        public int LastChanged;
        /// <summary>Calendar hours when active fire was last seen in this chunk.</summary>
        public double FireTouchedAtHours;
        /// <summary>Schedule an extra column pass to prune orphan foliage after fire.</summary>
        public bool PendingOrphanPrune;
    }
}
