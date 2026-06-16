using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla grass colonizers that use flower block paths but compete with tallgrass matrix.</summary>
    public static class EcologyGrassColonizerSpecies
    {
        public const string Redtopgrass = "redtopgrass";

        public static readonly IReadOnlyList<string> All = new[] { Redtopgrass };

        public static bool IsKnown(string species)
        {
            if (string.IsNullOrEmpty(species)) return false;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] == species) return true;
            }

            return false;
        }
    }
}
