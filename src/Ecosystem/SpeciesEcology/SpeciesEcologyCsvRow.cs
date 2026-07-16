namespace WildFarming.Ecosystem.SpeciesEcology
{
    internal sealed class SpeciesEcologyCsvRow
    {
        public string Species;
        public string Taxon;

        public float MinTemp;
        public float MaxTemp;
        public float MinRain;
        public float MaxRain;
        public float MinForest;
        public float MaxForest;
        public float SpreadRate;

        public string SpreadMode;
        public string MatConnectivity;
        public float SeedDispersalChance;
        public int SeedDispersalRadius;
        public int MatSpreadRadius;
        public int IndependentSpreadRadius;
        public int SpreadRadius;

        public int SameSpeciesSpacing;
        public int OtherSpeciesSpacing;
        public string SpacingFromSpecies;
        public int MinSunlight;

        public string Habitat;
        public int WaterMaxDepth;
        public int WaterMinDepth;
        public int WaterVerticalBlocks;
        public int WaterExactDepth;

        public string SoilKinds;
        public int SoilMinFertility;
        public int SoilMaxFertility;

        public string ContextAffinity;
        public float ContextBonus;
        public float ForestInteriorPenalty;
        public float HoldStrength;

        public string Moisture;
        public string Light;
        public float NicheBonus;

        public bool SeasonExplicit;

        public double FlowerMaturationHours;
        public double FlowerCooldownHours;
        /// <summary>Dieback life-cycles before senescence; 0 = use global MaxFlowerPhenologyLifeCycles.</summary>
        public int FlowerPhenologyLifeCycles;
        public double FernMaturationHours;
        public double FernCooldownHours;
        public double BerryMaturationHours;

        public string TreeSeralRole;
        public string SoilSuccessionRole;
    }
}
