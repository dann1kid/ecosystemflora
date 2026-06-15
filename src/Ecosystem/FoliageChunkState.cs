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
    }
}
