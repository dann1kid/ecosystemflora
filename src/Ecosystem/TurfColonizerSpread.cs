namespace WildFarming.Ecosystem
{
    /// <summary>Species that spread onto occupied turf rather than chasing empty cells.</summary>
    internal static class TurfColonizerSpread
    {
        public static bool PrefersOccupiedTurf(string species)
        {
            return EcologyGrassColonizerSpecies.IsKnown(species);
        }
    }
}
