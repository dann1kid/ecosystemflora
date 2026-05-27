using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class EcologyAreaScanner
    {
        public static void Scan(
            EcologySpacingIndex index,
            BlockPos center,
            int radius,
            int verticalSearch,
            out string[] species,
            out int[] counts,
            out int total)
        {
            species = null;
            counts = null;
            total = 0;

            if (index == null || center == null || radius <= 0) return;

            var tally = new Dictionary<string, int>();
            index.CountSpeciesNear(center, radius, verticalSearch, tally);

            foreach (KeyValuePair<string, int> kv in tally)
            {
                total += kv.Value;
            }

            if (total == 0) return;

            var pairs = new List<KeyValuePair<string, int>>(tally);
            pairs.Sort((a, b) => b.Value.CompareTo(a.Value));

            species = new string[pairs.Count];
            counts = new int[pairs.Count];
            for (int i = 0; i < pairs.Count; i++)
            {
                species[i] = pairs[i].Key;
                counts[i] = pairs[i].Value;
            }
        }
    }
}
