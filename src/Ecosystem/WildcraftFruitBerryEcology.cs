using System;
using Vintagestory.API.Common;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wild <c>wildcraftfruit:*</c> berry bushes (Herbarium / PricklyBerryBush / shrub types).
    /// Used when JSON <c>ecologyParticipant</c> attrs are missing on resolved blocks.
    /// </summary>
    internal static class WildcraftFruitBerryEcology
    {
        /// <summary>Longest match first.</summary>
        static readonly string[] WildBushCodes =
        {
            "bottompricklybush",
            "toppricklybush",
            "pricklyshortbush",
            "pricklyberrybush",
            "bottomberrybush",
            "topberrybush",
            "shortberrybush",
            "groundberryplant",
            "shrubberrybush",
            "berrybush",
        };

        public readonly struct InjectionProfile
        {
            public float MinTemp { get; init; }
            public float MaxTemp { get; init; }
            public float MinRain { get; init; }
            public float MaxRain { get; init; }
            public float MinForest { get; init; }
            public float MaxForest { get; init; }
            public float SpreadRate { get; init; }
            public int SameSpeciesSpacing { get; init; }
            public int OtherSpeciesSpacing { get; init; }
        }

        static readonly InjectionProfile DefaultProfile = new InjectionProfile
        {
            MinTemp = -2f,
            MaxTemp = 24f,
            MinRain = 0.35f,
            MaxRain = 1f,
            MinForest = 0f,
            MaxForest = 0.85f,
            SpreadRate = 0.65f,
            SameSpeciesSpacing = 1,
            OtherSpeciesSpacing = 2,
        };

        public static bool IsWildBerryBlock(Block block) => TryGetEcologySpecies(block, out _);

        public static bool TryGetEcologySpecies(Block block, out string species)
        {
            species = null;
            return TryParse(block, out _, out string berryType, out _)
                && TryMapBerryType(berryType, out species);
        }

        public static bool TryGetSpreadBlock(Block block, out AssetLocation spreadBlock)
        {
            spreadBlock = null;
            if (!TryParse(block, out string bushCode, out string berryType, out _)) return false;
            if (!TryMapBerryType(berryType, out _)) return false;

            string spreadCode = ResolveSpreadBushCode(bushCode) + "-" + berryType + "-empty";
            spreadBlock = new AssetLocation("wildcraftfruit", spreadCode);
            return true;
        }

        public static bool TryGetInjectionProfile(string species, out InjectionProfile profile)
        {
            profile = default;
            if (string.IsNullOrEmpty(species)) return false;

#pragma warning disable CS0618
            if (WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
            {
                profile = new InjectionProfile
                {
                    MinTemp = berry.MinTemp,
                    MaxTemp = berry.MaxTemp,
                    MinRain = berry.MinRain,
                    MaxRain = berry.MaxRain,
                    MinForest = berry.MinForest,
                    MaxForest = berry.MaxForest,
                    SpreadRate = berry.SpreadRate,
                    SameSpeciesSpacing = berry.SameSpeciesSpacing,
                    OtherSpeciesSpacing = berry.OtherSpeciesSpacing,
                };
                return true;
            }
#pragma warning restore CS0618

            if (SpeciesEcologyRegistry.IsLoaded
                && SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow row))
            {
                profile = new InjectionProfile
                {
                    MinTemp = row.MinTemp,
                    MaxTemp = row.MaxTemp,
                    MinRain = row.MinRain,
                    MaxRain = row.MaxRain,
                    MinForest = row.MinForest,
                    MaxForest = row.MaxForest,
                    SpreadRate = row.SpreadRate,
                    SameSpeciesSpacing = row.SameSpeciesSpacing,
                    OtherSpeciesSpacing = row.OtherSpeciesSpacing,
                };
                return true;
            }

            profile = DefaultProfile;
            return true;
        }

        static bool TryParse(Block block, out string bushCode, out string berryType, out string state)
        {
            bushCode = null;
            berryType = null;
            state = null;

            if (block?.Code == null || block.Code.Domain != "wildcraftfruit") return false;

            string path = block.Code.Path;
            if (string.IsNullOrEmpty(path)) return false;

            for (int i = 0; i < WildBushCodes.Length; i++)
            {
                string candidate = WildBushCodes[i];
                string lead = candidate + "-";
                if (!path.StartsWith(lead, StringComparison.Ordinal)) continue;

                string rest = path.Substring(lead.Length);
                int lastDash = rest.LastIndexOf('-');
                if (lastDash <= 0) return false;

                string growthState = rest.Substring(lastDash + 1);
                if (!IsGrowthState(growthState)) return false;

                string type = rest.Substring(0, lastDash);
                if (string.IsNullOrEmpty(type)) return false;

                bushCode = candidate;
                berryType = type;
                state = growthState;
                return true;
            }

            return false;
        }

        static string ResolveSpreadBushCode(string bushCode)
        {
            switch (bushCode)
            {
                case "toppricklybush":
                case "bottompricklybush":
                    return "pricklyberrybush";
                case "topberrybush":
                case "bottomberrybush":
                    return "berrybush";
                default:
                    return bushCode;
            }
        }

        static bool IsGrowthState(string state) =>
            state == "empty" || state == "flowering" || state == "ripe";

        static bool TryMapBerryType(string berryType, out string species)
        {
            species = null;
            if (string.IsNullOrEmpty(berryType)) return false;

            if (string.Equals(berryType, "brambleberry", StringComparison.OrdinalIgnoreCase))
            {
                species = "blackberry";
                return true;
            }

            if (EcologyBerrySpecies.IsKnown(berryType))
            {
                species = berryType;
                return true;
            }

            species = berryType;
            return true;
        }
    }
}
