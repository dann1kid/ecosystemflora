namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Prevents bad/missing <see cref="ReproducerEntry.LastTreeGrowthYear"/> values from burning a
    /// tree's lifespan by "catching up" every year since world creation.
    /// </summary>
    internal static class TreeCalendarCatchUp
    {
        /// <summary>
        /// Snap an impossible growth-year lag forward so at most <paramref name="catchUpLimit"/>
        /// calendar years are processed from here. Young trees with LastGrowthYear≈0 after a long
        /// world age are the usual trigger.
        /// </summary>
        public static int NormalizeLastGrowthYear(
            int lastGrowthYear,
            int gameYear,
            int treeAgeYears,
            int catchUpLimit)
        {
            if (catchUpLimit < 1) catchUpLimit = 1;
            if (treeAgeYears < 0) treeAgeYears = 0;

            if (lastGrowthYear == int.MinValue || lastGrowthYear > gameYear)
            {
                return gameYear - 1;
            }

            int behind = gameYear - lastGrowthYear;
            // A living tree cannot honestly be more calendar-years behind than it has lived,
            // plus one catch-up burst. Larger gaps mean unloaded time / defaulted save fields.
            int maxHonestBehind = treeAgeYears + catchUpLimit;
            if (maxHonestBehind < catchUpLimit) maxHonestBehind = catchUpLimit;

            if (behind > maxHonestBehind)
            {
                return gameYear - catchUpLimit;
            }

            return lastGrowthYear;
        }
    }
}
