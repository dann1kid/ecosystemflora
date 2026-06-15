using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal enum CanopySeasonPhase
    {
        Idle = 0,
        Autumn = 1,
        Spring = 2,
    }

    /// <summary>Season activity, climate, and attempt rolls for canopy strip/bud.</summary>
    internal static class CanopyEcology
    {
        public static CanopySeasonPhase ResolvePhase(
            ICoreAPI api,
            BlockPos basePos,
            string wood,
            out float activity)
        {
            activity = 0f;
            if (api?.World?.Calendar == null || basePos == null || string.IsNullOrEmpty(wood)) return CanopySeasonPhase.Idle;
            if (!CanopyBlockHelper.IsDeciduousTreeWood(wood)) return CanopySeasonPhase.Idle;

            EcosystemConfig cfg = EcosystemConfig.Loaded;
            if (!cfg.EnableSeasonalFoliage) return CanopySeasonPhase.Idle;

            IGameCalendar cal = api.World.Calendar;
            float yearProgress = cal.DayOfYearf / cal.DaysPerYear;
            WildCanopySeason.Profile profile = WildCanopySeason.Resolve(wood);

            float defol = profile.DefoliateInterpolated(yearProgress) * cfg.CanopyActivityScale;
            float bud = profile.BudInterpolated(yearProgress) * cfg.CanopyActivityScale;

            float temp = SampleTemperature(api, basePos);
            float latMult = LatitudeMultiplier(api, basePos, cfg.CanopyLatitudeInfluence);
            defol *= latMult;
            bud *= latMult * TemperatureBudMultiplier(temp, cfg.CanopyBudMinTemperature);

            if (defol > 0.02f && defol > bud)
            {
                activity = defol;
                return CanopySeasonPhase.Autumn;
            }

            if (bud > 0.02f)
            {
                activity = bud;
                return CanopySeasonPhase.Spring;
            }

            return CanopySeasonPhase.Idle;
        }

        public static bool RollStripAttempt(ICoreAPI api, BlockPos pos, string wood, float activity, int gameYear) =>
            RollAttempt(api, pos, wood, activity, gameYear, isBud: false, stripScale: 0.48f);

        public static bool RollBranchyStripAttempt(ICoreAPI api, BlockPos pos, string wood, float activity, int gameYear) =>
            RollAttempt(api, pos, wood, activity, gameYear, isBud: false, stripScale: 0.22f);

        public static bool RollSkeletonRestoreAttempt(ICoreAPI api, BlockPos pos, string wood, float activity, int gameYear) =>
            RollAttempt(api, pos, wood, activity, gameYear, isBud: false, stripScale: 0.14f);

        public static bool RollBudAttempt(ICoreAPI api, BlockPos pos, string wood, float activity, int gameYear) =>
            RollAttempt(api, pos, wood, activity, gameYear, isBud: true, stripScale: 0.48f);

        public static bool RollCatchUpBudAttempt(ICoreAPI api, BlockPos pos, string wood, float activity, int gameYear) =>
            RollAttempt(api, pos, wood, activity, gameYear, isBud: true, stripScale: 0.52f);

        static bool RollAttempt(
            ICoreAPI api,
            BlockPos pos,
            string wood,
            float activity,
            int gameYear,
            bool isBud,
            float stripScale = 0.48f)
        {
            if (activity <= 0f || api == null || pos == null) return false;
            float noise = 0.55f + CanopyBlockHelper.DeterministicNoise(pos, wood, gameYear) * 0.45f;
            float scale = isBud ? 0.78f : stripScale;
            float chance = Clamp01(activity * noise * scale);
            return api.World.Rand.NextDouble() < chance;
        }

        public static int GameYear(IGameCalendar cal)
        {
            if (cal == null || cal.DaysPerYear <= 0) return 0;
            return (int)(cal.TotalDays / cal.DaysPerYear);
        }

        static float SampleTemperature(ICoreAPI api, BlockPos pos)
        {
            ClimateCondition now = api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays);
            return now?.Temperature ?? 0f;
        }

        static float LatitudeMultiplier(ICoreAPI api, BlockPos pos, float influence)
        {
            if (influence <= 0f || pos == null) return 1f;
            int mapSizeZ = api.World.BlockAccessor.MapSizeZ;
            if (mapSizeZ <= 0) return 1f;
            float latNorm = Math.Abs(pos.Z / (float)mapSizeZ - 0.5f) * 2f;
            return 1f + influence * (0.35f - latNorm);
        }

        static float TemperatureBudMultiplier(float temp, float minTemp)
        {
            if (temp >= minTemp + 8f) return 1.15f;
            if (temp <= minTemp - 6f) return 0.05f;
            return Clamp01((temp - (minTemp - 6f)) / 14f);
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        internal static float LatitudeMultiplierForCatchUp(ICoreAPI api, BlockPos pos, float influence) =>
            LatitudeMultiplier(api, pos, influence);
    }
}
