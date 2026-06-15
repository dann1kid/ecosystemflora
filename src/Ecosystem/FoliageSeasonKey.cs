using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Calendar stamp for per-chunk foliage sync (invalidate on month change).</summary>
    internal static class FoliageSeasonKey
    {
        public static int Current(ICoreAPI api)
        {
            IGameCalendar cal = api?.World?.Calendar;
            if (cal == null || cal.DaysPerYear <= 0) return 0;

            int gameYear = (int)(cal.TotalDays / cal.DaysPerYear);
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            int month = ((int)(yearProgress * 12f)) % 12;
            if (month < 0) month += 12;

            return gameYear * 12 + month;
        }
    }
}
