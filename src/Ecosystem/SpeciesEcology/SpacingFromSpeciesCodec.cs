using System;
using System.Collections.Generic;
using System.Text;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal static class SpacingFromSpeciesCodec
    {
        public static string Format(IReadOnlyDictionary<string, int> map)
        {
            if (map == null || map.Count == 0) return string.Empty;

            var keys = new List<string>(map.Count);
            foreach (KeyValuePair<string, int> pair in map)
            {
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                {
                    keys.Add(pair.Key);
                }
            }

            keys.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(keys[i]);
                sb.Append('=');
                sb.Append(map[keys[i]]);
            }

            return sb.ToString();
        }

        public static Dictionary<string, int> Parse(string text)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(text)) return map;

            string[] pairs = text.Split('|');
            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i]?.Trim();
                if (string.IsNullOrEmpty(pair)) continue;

                int eq = pair.IndexOf('=');
                if (eq <= 0 || eq >= pair.Length - 1) continue;

                string other = pair.Substring(0, eq).Trim();
                if (string.IsNullOrEmpty(other)) continue;

                if (!int.TryParse(pair.Substring(eq + 1), out int blocks) || blocks <= 0) continue;
                map[other] = blocks;
            }

            return map;
        }
    }
}
