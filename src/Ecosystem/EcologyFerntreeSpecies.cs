namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla <c>ferntree-normal-*</c> ecology key.</summary>
    public static class EcologyFerntreeSpecies
    {
        public const string Ferntree = "ferntree";

        public static bool IsKnown(string species) => species == Ferntree;
    }
}
