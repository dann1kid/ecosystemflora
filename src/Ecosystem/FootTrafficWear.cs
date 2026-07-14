using System;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Foot-traffic coverage tempo: preferred knob is footsteps to full wear;
    /// optional <see cref="EcosystemConfig.FootTrafficSoilWearPressureStep"/> when steps is 0.
    /// </summary>
    internal static class FootTrafficWear
    {
        /// <summary>
        /// Pressure points between coverage stages.
        /// Default: <c>PressurePerStep × StepsToFull / MaxTrafficWearIndex</c> (two stages to verysparse).
        /// </summary>
        public static byte EffectiveWearStep(EcosystemConfig cfg)
        {
            if (cfg == null) return 80;

            int stages = SoilTrafficCoverage.MaxTrafficWearIndex;
            if (stages < 1) stages = 1;

            int perStep = cfg.FootTrafficPressurePerStep;
            if (perStep < 1) perStep = 1;

            int steps = cfg.FootTrafficStepsToFullCoverageWear;
            if (steps > 0)
            {
                int wear = (perStep * steps) / stages;
                if (wear < 1) wear = 1;
                if (wear > 127) wear = 127;
                return (byte)wear;
            }

            byte overrideStep = cfg.FootTrafficSoilWearPressureStep;
            if (overrideStep > 0) return overrideStep > 127 ? (byte)127 : overrideStep;
            return 80;
        }

        /// <summary>Desired wear index 0..MaxTrafficWearIndex from pressure.</summary>
        public static int TargetWearIndex(byte pressure, byte wearStep)
        {
            if (wearStep < 1) wearStep = 1;
            int idx = pressure / wearStep;
            int max = SoilTrafficCoverage.MaxTrafficWearIndex;
            return idx > max ? max : idx;
        }

        public static byte MarkForWearIndex(int wearIndex, byte wearStep)
        {
            if (wearIndex <= 0 || wearStep < 1) return 0;
            int mark = wearIndex * wearStep;
            return mark > 255 ? (byte)255 : (byte)mark;
        }
    }
}
