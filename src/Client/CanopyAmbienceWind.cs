using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Client
{
    /// <summary>Normalised client wind at player for leaf drift tuning.</summary>
    internal readonly struct CanopyAmbienceWind
    {
        public readonly float Strength;
        public readonly Vec3f Raw;
        public readonly Vec3f HorizontalDir;
        public readonly float HorizontalSpeed;

        CanopyAmbienceWind(float strength, Vec3f raw, Vec3f horizontalDir, float horizontalSpeed)
        {
            Strength = strength;
            Raw = raw;
            HorizontalDir = horizontalDir;
            HorizontalSpeed = horizontalSpeed;
        }

        public bool IsCalm => Strength < 0.12f;

        public static CanopyAmbienceWind Sample()
        {
            Vec3f raw = GlobalConstants.CurrentWindSpeedClient ?? new Vec3f();
            float horiz = GameMath.Sqrt(raw.X * raw.X + raw.Z * raw.Z);
            float strength = GameMath.Clamp(horiz / 0.22f, 0f, 1f);

            Vec3f dir = new Vec3f();
            if (horiz > 0.0005f)
            {
                dir.Set(raw.X / horiz, 0f, raw.Z / horiz);
            }

            return new CanopyAmbienceWind(strength, raw, dir, horiz);
        }
    }
}
