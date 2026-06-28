using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla wild fruiting bush ecology keys (<c>fruitingbush-wild-*</c>).</summary>
    public static class EcologyBerrySpecies
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "blackcurrant", "redcurrant", "whitecurrant", "blueberry", "cranberry",
            "strawberry", "beautyberry", "cloudberry", "blackberry", "raspberry",
        };

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
