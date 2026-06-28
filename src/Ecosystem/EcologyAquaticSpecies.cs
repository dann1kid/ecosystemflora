using System.Collections.Generic;

namespace WildFarming.Ecosystem
{
    /// <summary>Contract aquatic flora ecology keys.</summary>
    public static class EcologyAquaticSpecies
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            "coopersreed", "tule", "papyrus", "waterlily", "watercrowfoot",
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

        public static EcologyHabitat GetHabitat(string species)
        {
            switch (species)
            {
                case "coopersreed":
                case "tule":
                case "papyrus":
                    return EcologyHabitat.ReedNearWater;
                case "waterlily":
                    return EcologyHabitat.WaterSurface;
                case "watercrowfoot":
                    return EcologyHabitat.UnderwaterColumn;
                default:
                    return EcologyHabitat.Terrestrial;
            }
        }
    }
}
