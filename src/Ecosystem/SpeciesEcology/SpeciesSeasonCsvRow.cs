namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal sealed class SpeciesSeasonCsvRow
    {
        public string Species;
        public readonly float[] Spread = new float[12];
        public readonly float[] Stress = new float[12];
    }
}
