using System.Collections.Generic;
using System.Text;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SoilKindFormatter
    {
        static readonly (SoilKind Flag, string Name)[] Ordered =
        {
            (SoilKind.HighFert, "HighFert"),
            (SoilKind.MediumFert, "MediumFert"),
            (SoilKind.LowFert, "LowFert"),
            (SoilKind.ForestFloor, "ForestFloor"),
            (SoilKind.Peat, "Peat"),
            (SoilKind.Sand, "Sand"),
            (SoilKind.Clay, "Clay"),
            (SoilKind.Gravel, "Gravel"),
            (SoilKind.Barren, "Barren"),
        };

        public static string Format(SoilKind kind)
        {
            if (kind == SoilKind.None) return string.Empty;

            var parts = new List<string>(Ordered.Length);
            for (int i = 0; i < Ordered.Length; i++)
            {
                SoilKind flag = Ordered[i].Flag;
                if ((kind & flag) == flag)
                {
                    parts.Add(Ordered[i].Name);
                }
            }

            return string.Join("|", parts);
        }
    }
}
