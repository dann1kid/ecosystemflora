using System;

namespace WildFarming.Ecosystem
{
    /// <summary>Client ambience rates and colours by calendar month (0=Jan..11=Dec).</summary>
    public static class CanopyAmbienceSeasonCurves
    {
        static readonly float[] MoteByMonth =
        {
            0f, 0f, 0.2f, 0.5f, 0.8f, 1.0f, 1.0f, 0.9f, 0.6f, 0.3f, 0.1f, 0f,
        };

        static readonly float[] DriftByMonth =
        {
            0f, 0f, 0f, 0f, 0f, 0.1f, 0.1f, 0.1f, 0.3f, 1.0f, 1.0f, 0.4f,
        };

        public static int MonthFromYearProgress(float yearProgress)
        {
            int month = (int)(yearProgress * 12f);
            month %= 12;
            if (month < 0) month += 12;
            return month;
        }

        public static float MoteRate(int month)
        {
            if (month < 0 || month > 11) return 0f;
            return MoteByMonth[month];
        }

        public static float DriftRate(int month)
        {
            if (month < 0 || month > 11) return 0f;
            return DriftByMonth[month];
        }

        public static int ResolveMoteColor(int month, Random rand)
        {
            int jitter = rand.Next(-5, 6);
            if (month <= 3)
            {
                return ColorArgb(160, 95 + jitter, 145 + jitter, 55 + jitter);
            }

            return ColorArgb(140, 70 + jitter, 130 + jitter, 45 + jitter);
        }

        public static int ResolveDriftColor(string wood, Random rand)
        {
            int jitter = rand.Next(-6, 7);
            if (string.IsNullOrEmpty(wood))
            {
                return ColorArgb(210, 168 + jitter, 98 + jitter, 32 + jitter);
            }

            switch (wood)
            {
                case "birch":
                case "maple":
                    return ColorArgb(220, 215 + jitter, 175 + jitter, 45 + jitter);
                case "crimsonkingmaple":
                    return ColorArgb(220, 195 + jitter, 75 + jitter, 35 + jitter);
                case "oak":
                case "walnut":
                    return ColorArgb(220, 172 + jitter, 92 + jitter, 28 + jitter);
                case "acacia":
                case "kapok":
                    return ColorArgb(210, 188 + jitter, 118 + jitter, 38 + jitter);
                default:
                    return ColorArgb(210, 168 + jitter, 98 + jitter, 32 + jitter);
            }
        }

        public static float WeatherAttenuation(float rainfall, bool suppressInRain)
        {
            if (!suppressInRain) return 1f;
            if (rainfall >= 0.15f) return 0f;
            return 1f;
        }

        static int ColorArgb(int a, int r, int g, int b)
        {
            return (ClampByte(a) << 24) | (ClampByte(r) << 16) | (ClampByte(g) << 8) | ClampByte(b);
        }

        static int ClampByte(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }
    }
}
