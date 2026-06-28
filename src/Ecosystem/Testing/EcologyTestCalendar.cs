using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem.Testing
{
    internal sealed class EcologyTestCalendar : IGameCalendar
    {
        public double TotalHours { get; set; } = 100;
        public float HoursPerDay { get; set; } = 24f;
        public int DaysPerYear { get; set; } = 9;
        public int DaysPerMonth { get; set; } = 9;
        public double TotalDays { get; set; }
        public double ElapsedHours { get; set; }
        public double ElapsedDays { get; set; }
        public float CalendarSpeedMul { get; set; } = 1f;
        public int DayOfYear => (int)(TotalDays % DaysPerYear) + 1;
        public float DayOfYearf => DayOfYear;
        public int Month => 1;
        public int Year => (int)(TotalDays / System.Math.Max(1, DaysPerYear));
        public float YearRel => Year;
        public bool Pause { get; set; }
        public float SpeedOfTime { get; set; } = 1f;
        public float Timelapse { get; set; } = 1f;
        public long ElapsedSeconds => 0;
        public EnumMonth MonthName => EnumMonth.January;
        public EnumSeason Season => EnumSeason.Spring;
        public float? SeasonOverride { get; set; }
        public bool IsRunning => true;
        public bool IsPaused => false;
        public bool IsStopped => false;
        public int FullHourOfDay => (int)(TotalHours % HoursPerDay);
        public float HourOfDay => (float)(TotalHours % HoursPerDay);
        public EnumMoonPhase MoonPhase => default;
        public double MoonPhaseExact => 0.5;
        public float MoonPhaseBrightness => 0f;
        public float MoonSize => 0f;

        public HemisphereDelegate OnGetHemisphere { get; set; }
        public GetLatitudeDelegate OnGetLatitude { get; set; }
        public SolarSphericalCoordsDelegate OnGetSolarSphericalCoords { get; set; }

        public float GetDayLightStrength(double posX, double posZ) => 1f;
        public float GetDayLightStrength(BlockPos pos) => 1f;
        public Vec3f GetSunPosition(Vec3d pos, double dt) => new Vec3f();
        public Vec3f GetMoonPosition(Vec3d pos, double dt) => new Vec3f();
        public string PrettyDate() => "test";
        public void SetTimeSpeedModifier(string code, float speed) { }
        public void RemoveTimeSpeedModifier(string code) { }
        public EnumSeason GetSeason(BlockPos pos) => EnumSeason.Spring;
        public float GetSeasonRel(BlockPos pos) => 0.5f;
        public EnumHemisphere GetHemisphere(BlockPos pos) => EnumHemisphere.North;
        public void Add(float hours) => TotalHours += hours;
        public void SetSeasonOverride(float? rel) => SeasonOverride = rel;

        public void SetMonth(int month) { }
        public void SetDayOfYear(int dayOfYear) { }
        public void SetYear(int year) { }
        public void SetTotalHours(double hours) => TotalHours = hours;
        public void SetSpeedOfTime(float speed) => SpeedOfTime = speed;
        public void SetTimelapse(float timelapse) => Timelapse = timelapse;
        public void SetSeason(EnumSeason season) { }
        public void SetMonthName(EnumMonth month) { }
        public void SetPause(bool pause) => Pause = pause;
        public void SetRunning(bool running) { }
        public void SetStopped(bool stopped) { }
        public void SetPaused(bool paused) { }
        public void SetTotalDays(long days) { TotalDays = days; TotalHours = days * HoursPerDay; }
        public void SetElapsedSeconds(long seconds) { }
        public void SetHoursPerDay(float hours) => HoursPerDay = hours;
        public void SetDaysPerYear(int days) => DaysPerYear = days;
    }
}
