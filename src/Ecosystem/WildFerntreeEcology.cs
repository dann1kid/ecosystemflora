namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla <c>ferntree-normal-*</c> — tropical arborescent fern (not log-grown).</summary>
    [EcologyExportTable]
    [System.Obsolete("Export-only default table. Contract species runtime uses SpeciesEcologyRegistry.")]
    internal static class WildFerntreeEcology
    {
        public const string Species = EcologyFerntreeSpecies.Ferntree;

        public readonly struct Profile
        {
            public Profile(
                float minTemp,
                float maxTemp,
                float minRain,
                float maxRain,
                float minForest,
                float maxForest,
                float spreadRate,
                int spreadRadius,
                int sameSpeciesSpacing,
                int otherSpeciesSpacing,
                int senescenceAgeYears,
                int youngTopUntilYears,
                int mediumTopUntilYears,
                int maxTrunkHeight)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                MinForest = minForest;
                MaxForest = maxForest;
                SpreadRate = spreadRate;
                SpreadRadius = spreadRadius;
                SameSpeciesSpacing = sameSpeciesSpacing;
                OtherSpeciesSpacing = otherSpeciesSpacing;
                SenescenceAgeYears = senescenceAgeYears;
                YoungTopUntilYears = youngTopUntilYears;
                MediumTopUntilYears = mediumTopUntilYears;
                MaxTrunkHeight = maxTrunkHeight;
            }

            public float MinTemp { get; }
            public float MaxTemp { get; }
            public float MinRain { get; }
            public float MaxRain { get; }
            public float MinForest { get; }
            public float MaxForest { get; }
            public float SpreadRate { get; }
            public int SpreadRadius { get; }
            public int SameSpeciesSpacing { get; }
            public int OtherSpeciesSpacing { get; }
            public int SenescenceAgeYears { get; }
            public int YoungTopUntilYears { get; }
            public int MediumTopUntilYears { get; }
            public int MaxTrunkHeight { get; }
        }

        public static readonly Profile Default = new Profile(
            minTemp: 18f,
            maxTemp: 45f,
            minRain: 0.55f,
            maxRain: 1f,
            minForest: 0.25f,
            maxForest: 1f,
            spreadRate: 0.22f,
            spreadRadius: 7,
            sameSpeciesSpacing: 8,
            otherSpeciesSpacing: 5,
            senescenceAgeYears: 80,
            youngTopUntilYears: 25,
            mediumTopUntilYears: 55,
            maxTrunkHeight: 12);

        public static Profile Resolve() => Default;

        public static bool IsSpecies(string species) =>
            species == Species;
    }
}
