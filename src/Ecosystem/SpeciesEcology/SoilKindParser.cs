using System;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SoilKindParser
    {
        public static SoilKind Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return SoilKind.None;

            SoilKind result = SoilKind.None;
            string[] parts = text.Split('|');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i]?.Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (Enum.TryParse(part, ignoreCase: true, out SoilKind kind))
                {
                    result |= kind;
                }
            }

            return result;
        }
    }
}
