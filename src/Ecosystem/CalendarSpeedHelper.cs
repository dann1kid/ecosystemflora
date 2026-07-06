using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Scales ecology calendar-hour delays by the server global calendar speed multiplier
    /// (<see cref="IGameCalendar.CalendarSpeedMul"/> / <see cref="IGameCalendar.SpeedOfTime"/>).
    /// </summary>
    internal static class CalendarSpeedHelper
    {
        internal const float MinSpeed = 0.01f;

        public static float GetSpeedMultiplier(IGameCalendar cal)
        {
            if (cal == null) return 1f;

            float mul = cal.CalendarSpeedMul;
            if (mul <= 0f || float.IsNaN(mul) || float.IsInfinity(mul))
            {
                mul = cal.SpeedOfTime;
            }

            if (mul <= 0f || float.IsNaN(mul) || float.IsInfinity(mul))
            {
                return 1f;
            }

            return mul;
        }

        /// <summary>Slower calendar → longer delays (fewer spread steps per game month).</summary>
        public static double ScaleCalendarHours(double hours, IGameCalendar cal)
        {
            if (hours <= 0 || cal == null) return hours;

            float speed = GetSpeedMultiplier(cal);
            if (speed >= 0.999f && speed <= 1.001f) return hours;

            return hours / System.Math.Max(MinSpeed, speed);
        }

        public static double ScaleCalendarHours(double hours, ICoreAPI api) =>
            ScaleCalendarHours(hours, api?.World?.Calendar);
    }
}
