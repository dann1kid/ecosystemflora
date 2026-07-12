using System;

namespace WildFarming.Ecosystem
{
    /// <summary>Normalize vanilla <c>game:fern-*</c> block paths to ecology species ids.</summary>
    internal static class VanillaFernSpeciesParser
    {
        public static string TryParseSpeciesFromPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("fern-", StringComparison.Ordinal))
            {
                return null;
            }

            string rest = path.Substring("fern-".Length);
            string best = null;

            for (int i = 0; i < EcologyFernSpecies.All.Count; i++)
            {
                string species = EcologyFernSpecies.All[i];
                if (rest == species || rest.StartsWith(species + "-", StringComparison.Ordinal))
                {
                    if (best == null || species.Length > best.Length)
                    {
                        best = species;
                    }
                }
            }

            return best;
        }
    }
}
