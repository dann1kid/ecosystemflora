namespace WildFarming.Ecosystem
{
    internal static class WildVineEcology
    {
        public const string TemperateSpecies = WildVineHelper.TemperateSpecies;
        public const string TropicalSpecies = WildVineHelper.TropicalSpecies;

        public readonly struct Profile
        {
            public readonly float MinTemp;
            public readonly float MaxTemp;
            public readonly float MinRain;
            public readonly float MaxRain;
            public readonly float SpreadRate;
            public readonly int SameSpeciesSpacing;
            public readonly int OtherSpeciesSpacing;

            public Profile(
                float minTemp,
                float maxTemp,
                float minRain,
                float maxRain,
                float spreadRate,
                int sameSpacing,
                int otherSpacing)
            {
                MinTemp = minTemp;
                MaxTemp = maxTemp;
                MinRain = minRain;
                MaxRain = maxRain;
                SpreadRate = spreadRate;
                SameSpeciesSpacing = sameSpacing;
                OtherSpeciesSpacing = otherSpacing;
            }
        }

        public static Profile Resolve(bool tropical) =>
            tropical ? Tropical : Temperate;

        public static readonly Profile Temperate = new Profile(
            minTemp: 2f,
            maxTemp: 28f,
            minRain: 0.35f,
            maxRain: 1f,
            spreadRate: 0.85f,
            sameSpacing: 0,
            otherSpacing: 0);

        public static readonly Profile Tropical = new Profile(
            minTemp: 18f,
            maxTemp: 42f,
            minRain: 0.55f,
            maxRain: 1f,
            spreadRate: 0.7f,
            sameSpacing: 0,
            otherSpacing: 0);

        public static bool IsSpecies(string species) =>
            species == TemperateSpecies || species == TropicalSpecies;

        public static bool TryGet(string species, out Profile profile)
        {
            if (species == TemperateSpecies)
            {
                profile = Temperate;
                return true;
            }

            if (species == TropicalSpecies)
            {
                profile = Tropical;
                return true;
            }

            profile = default;
            return false;
        }
    }
}
