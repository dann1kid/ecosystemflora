using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Land-only wetland sedges (<c>tallplant-brownsedge-*</c>).</summary>
    public static class EcologyShoreSedgeSpecies
    {
        public const string Brownsedge = "brownsedge";

        public static readonly IReadOnlyList<string> All = new[] { Brownsedge };

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
