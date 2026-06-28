using System.Collections.Generic;
using Vintagestory.API.Common;
using WildFarming.Ecosystem.SpeciesEcology;

namespace WildFarming.Ecosystem
{
    public class PlantRequirements
    {
        public string Species { get; set; }

        public EcologyHabitat Habitat { get; set; } = EcologyHabitat.Terrestrial;

        public int MaxWaterDepth { get; set; } = 1;

        public int MinWaterDepth { get; set; }

        public int VerticalBlocks { get; set; } = 1;

        /// <summary>If &gt;= 0, in-water reeds need exactly this many water blocks above muddy gravel.</summary>
        public int ExactWaterDepth { get; set; } = -1;

        public float MinTemp { get; set; } = -5f;
        public float MaxTemp { get; set; } = 50f;
        public float MinRain { get; set; } = 0f;
        public float MaxRain { get; set; } = 1f;
        public float MinForest { get; set; } = 0f;
        public float MaxForest { get; set; } = 1f;
        public int MinFertility { get; set; } = 100;

        /// <summary>Block.Fertility floor (vanilla soil-high=300, low=150, sand=10).</summary>
        public int MinGroundFertility { get; set; }

        /// <summary>Block.Fertility ceiling; 0 = no cap (poor-soil specialists use max).</summary>
        public int MaxGroundFertility { get; set; }

        public SoilKind AllowedSoilKinds { get; set; } = SoilKind.None;

        /// <summary>Minimum sun light at spread cell; 0 = skip (flowers).</summary>
        public int MinSunlight { get; set; }

        public int MinReplaceable { get; set; } = 9500;

        /// <summary>Relative spread vigor (1 = config baseline).</summary>
        public float SpreadRate { get; set; } = 1f;

        public FloraContextAffinity ContextAffinity { get; set; } = FloraContextAffinity.Open;

        public float ContextBonus { get; set; } = 1f;

        /// <summary>Multiplier in <see cref="FloraContext.ForestInterior"/> when affinity is open.</summary>
        public float ForestInteriorPenalty { get; set; } = 0.35f;

        /// <summary>Incumbent defense when another species tries to displace (low = easy to overrun).</summary>
        public float HoldStrength { get; set; } = 1f;

        /// <summary>When true, <see cref="WildSpeciesNiche"/> supplies moisture/light prefs.</summary>
        public bool HasNicheProfile { get; set; }

        public MoistureLevel PreferredMoisture { get; set; } = MoistureLevel.Mesic;

        public LightLevel PreferredLight { get; set; } = LightLevel.Partial;

        /// <summary>Multiplier when local niche matches species preference.</summary>
        public float NicheBonus { get; set; } = 1f;

        /// <summary>Horizontal spread radius; 0 = use <see cref="EcosystemConfig.ReproduceRadius"/>.</summary>
        public int SpreadRadius { get; set; }

        /// <summary>Min horizontal blocks to same species (0 = allow adjacent clumps).</summary>
        public int SameSpeciesSpacing { get; set; }

        /// <summary>Default min horizontal blocks to other flower species.</summary>
        public int OtherSpeciesSpacing { get; set; }

        /// <summary>Per-other-species minimum distance overrides.</summary>
        public Dictionary<string, int> SpacingFromSpecies { get; set; }

        public SpreadMode SpreadMode { get; set; } = SpreadMode.Independent;

        /// <summary>When true, never promote reed habitat to <see cref="SpreadMode.RhizomeMat"/>.</summary>
        public bool SuppressRhizomeSpread { get; set; }

        public bool UsesRhizomeSpread => SpreadMode == SpreadMode.RhizomeMat;

        public bool UsesSurfaceMatSpread => SpreadMode == SpreadMode.SurfaceMat;

        public bool UsesFernRhizomeSpread => SpreadMode == SpreadMode.FernRhizomeMat;

        public bool UsesBerryColonySpread => SpreadMode == SpreadMode.BerryColonyMat;

        public bool UsesShoreSedgeMatSpread => SpreadMode == SpreadMode.ShoreSedgeMat;

        /// <summary>When true, never promote water-surface habitat to <see cref="SpreadMode.SurfaceMat"/>.</summary>
        public bool SuppressSurfaceMatSpread { get; set; }

        /// <summary>Per attempt, chance to use seed/fragment dispersal (mat spread).</summary>
        public float SeedDispersalChance { get; set; }

        public int SeedDispersalRadius { get; set; }

        public int GetRequiredSpacingTo(string otherSpecies, EcosystemConfig cfg)
        {
            if (string.IsNullOrEmpty(otherSpecies)) return 0;

            int required;
            if (otherSpecies == Species)
            {
                // 0 = patch-forming species allows tight clumps (colonizers).
                return SameSpeciesSpacing;
            }

            if (SpacingFromSpecies != null && SpacingFromSpecies.TryGetValue(otherSpecies, out int specific))
            {
                return specific;
            }

            required = OtherSpeciesSpacing;
            if (required <= 0 && cfg != null) required = cfg.DefaultOtherSpeciesSpacing;

            return required;
        }

        public int GetSpacingSearchRadius(EcosystemConfig cfg)
        {
            if (cfg == null || !cfg.PlantSpacingEnabled) return 0;

            int max = SameSpeciesSpacing;
            int otherBase = OtherSpeciesSpacing > 0 ? OtherSpeciesSpacing : cfg.DefaultOtherSpeciesSpacing;
            if (otherBase > max) max = otherBase;

            if (SpacingFromSpecies != null)
            {
                foreach (KeyValuePair<string, int> pair in SpacingFromSpecies)
                {
                    if (pair.Value > max) max = pair.Value;
                }
            }

            return max;
        }

        public static PlantRequirements FromBlock(Block block)
        {
            if (block == null) return new PlantRequirements();

            var attrs = block.Attributes;
            float minTemp = attrs != null ? attrs["minTemp"].AsFloat(float.NaN) : float.NaN;
            float maxTemp = attrs != null ? attrs["maxTemp"].AsFloat(float.NaN) : float.NaN;
            float minRain = attrs != null ? attrs["minRain"].AsFloat(float.NaN) : float.NaN;
            float maxRain = attrs != null ? attrs["maxRain"].AsFloat(float.NaN) : float.NaN;
            float minForest = attrs != null ? attrs["minForest"].AsFloat(float.NaN) : float.NaN;
            float maxForest = attrs != null ? attrs["maxForest"].AsFloat(float.NaN) : float.NaN;
            float spreadRate = attrs != null ? attrs["ecologySpreadRate"].AsFloat(float.NaN) : float.NaN;
            bool thirdPartyParticipant = PlantCodeHelper.IsThirdPartyEcologyBlock(block);
            string species = PlantCodeHelper.ResolveEcologySpecies(block);
            EcologyHabitat habitat = EcologyHabitat.Terrestrial;
            int maxWaterDepth = 0;
            int minWaterDepth = 0;
            int verticalBlocks = 1;
            int exactWaterDepth = -1;
            int sameSpacing = -1;
            int otherSpacing = -1;
            Dictionary<string, int> spacingFrom = null;
            int minFertility = attrs != null ? attrs["minFertility"].AsInt(100) : 100;
            int minGroundFertility = attrs != null ? attrs["ecologyMinGroundFertility"].AsInt(0) : 0;
            int maxGroundFertility = attrs != null ? attrs["ecologyMaxGroundFertility"].AsInt(0) : 0;
            SoilKind allowedSoils = SoilKind.None;
            int minSunlight = attrs != null ? attrs["ecologyMinSunlight"].AsInt(0) : 0;
            SpreadMode spreadMode = SpreadMode.Independent;
            bool suppressRhizomeSpread = false;
            bool suppressSurfaceMatSpread = false;
            float seedDispersalChance = attrs != null ? attrs["ecologySeedDispersalChance"].AsFloat(0f) : 0f;
            int seedDispersalRadius = attrs != null ? attrs["ecologySeedDispersalRadius"].AsInt(0) : 0;
            int spreadRadius = attrs != null ? attrs["ecologySpreadRadius"].AsInt(0) : 0;

            if (attrs != null)
            {
                string spreadModeAttr = attrs["ecologySpreadMode"].AsString(null);
                if (!string.IsNullOrEmpty(spreadModeAttr))
                {
                    if (string.Equals(spreadModeAttr, "rhizome", System.StringComparison.OrdinalIgnoreCase))
                    {
                        spreadMode = SpreadMode.RhizomeMat;
                    }
                    else if (string.Equals(spreadModeAttr, "surfacemat", System.StringComparison.OrdinalIgnoreCase))
                    {
                        spreadMode = SpreadMode.SurfaceMat;
                    }
                    else if (string.Equals(spreadModeAttr, "independent", System.StringComparison.OrdinalIgnoreCase))
                    {
                        suppressRhizomeSpread = true;
                        suppressSurfaceMatSpread = true;
                    }
                }
            }

            if (thirdPartyParticipant && attrs != null)
            {
                habitat = PlantCodeHelper.ParseEcologyHabitat(attrs["ecologyHabitat"].AsString("Terrestrial"));

                switch (habitat)
                {
                    case EcologyHabitat.ReedNearWater:
                        maxWaterDepth = attrs["ecologyMaxWaterDepth"].AsInt(1);
                        minWaterDepth = attrs["ecologyMinWaterDepth"].AsInt(0);
                        verticalBlocks = attrs["ecologyVerticalBlocks"].AsInt(1);
                        exactWaterDepth = attrs["ecologyExactWaterDepth"].AsInt(-1);
                        minForest = 0f;
                        maxForest = 1f;
                        minFertility = 0;
                        sameSpacing = attrs["ecologySameSpeciesSpacing"].AsInt(0);
                        otherSpacing = attrs["ecologyOtherSpeciesSpacing"].AsInt(1);
                        if (float.IsNaN(minTemp)) minTemp = 3f;
                        if (float.IsNaN(maxTemp)) maxTemp = 23f;
                        if (float.IsNaN(minRain)) minRain = 0.4f;
                        if (float.IsNaN(maxRain)) maxRain = 1f;
                        if (float.IsNaN(spreadRate)) spreadRate = 1.0f;
                        break;

                    case EcologyHabitat.WaterSurface:
                        maxWaterDepth = attrs["ecologyMaxWaterDepth"].AsInt(2);
                        minWaterDepth = attrs["ecologyMinWaterDepth"].AsInt(1);
                        verticalBlocks = attrs["ecologyVerticalBlocks"].AsInt(1);
                        minForest = 0f;
                        maxForest = 1f;
                        minFertility = 0;
                        sameSpacing = attrs["ecologySameSpeciesSpacing"].AsInt(1);
                        otherSpacing = attrs["ecologyOtherSpeciesSpacing"].AsInt(1);
                        if (float.IsNaN(minTemp)) minTemp = 10f;
                        if (float.IsNaN(maxTemp)) maxTemp = 40f;
                        if (float.IsNaN(minRain)) minRain = 0.5f;
                        if (float.IsNaN(maxRain)) maxRain = 1f;
                        if (float.IsNaN(spreadRate)) spreadRate = 2.2f;
                        break;

                    case EcologyHabitat.UnderwaterColumn:
                        maxWaterDepth = attrs["ecologyMaxWaterDepth"].AsInt(8);
                        minWaterDepth = attrs["ecologyMinWaterDepth"].AsInt(2);
                        verticalBlocks = attrs["ecologyVerticalBlocks"].AsInt(1);
                        minForest = 0f;
                        maxForest = 1f;
                        minFertility = 0;
                        sameSpacing = attrs["ecologySameSpeciesSpacing"].AsInt(1);
                        otherSpacing = attrs["ecologyOtherSpeciesSpacing"].AsInt(1);
                        if (float.IsNaN(minTemp)) minTemp = -10f;
                        if (float.IsNaN(maxTemp)) maxTemp = 40f;
                        if (float.IsNaN(minRain)) minRain = 0.5f;
                        if (float.IsNaN(maxRain)) maxRain = 1f;
                        if (float.IsNaN(spreadRate)) spreadRate = 2f;
                        break;

                    case EcologyHabitat.TerrestrialTree:
                    case EcologyHabitat.Ferntree:
                        sameSpacing = attrs["ecologySameSpeciesSpacing"].AsInt(-1);
                        otherSpacing = attrs["ecologyOtherSpeciesSpacing"].AsInt(-1);
                        minSunlight = attrs["ecologyMinSunlight"].AsInt(11);
                        if (float.IsNaN(minTemp)) minTemp = -5f;
                        if (float.IsNaN(maxTemp)) maxTemp = 35f;
                        break;

                    default:
                        sameSpacing = attrs["ecologySameSpeciesSpacing"].AsInt(-1);
                        otherSpacing = attrs["ecologyOtherSpeciesSpacing"].AsInt(-1);
                        break;
                }
            }
            else if (!thirdPartyParticipant
                && !string.IsNullOrEmpty(species)
                && SpeciesEcologyRegistry.IsLoaded
                && SpeciesEcologyRegistry.TryGet(species, out SpeciesEcologyCsvRow registryRow))
            {
                return PlantRequirementsRegistryBuild.Build(
                    block,
                    species,
                    registryRow,
                    attrs,
                    minTemp,
                    maxTemp,
                    minRain,
                    maxRain,
                    minForest,
                    maxForest,
                    spreadRate,
                    minFertility,
                    minGroundFertility,
                    maxGroundFertility,
                    minSunlight,
                    spreadMode,
                    suppressRhizomeSpread,
                    suppressSurfaceMatSpread,
                    seedDispersalChance,
                    seedDispersalRadius,
                    spreadRadius);
            }
#pragma warning disable CS0618
            else if (!string.IsNullOrEmpty(species) && WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic))
            {
                habitat = aquatic.Habitat;
                maxWaterDepth = aquatic.MaxWaterDepth;
                minWaterDepth = aquatic.MinWaterDepth;
                verticalBlocks = aquatic.VerticalBlocks;
                exactWaterDepth = aquatic.ExactWaterDepth;
                if (float.IsNaN(minTemp)) minTemp = aquatic.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = aquatic.MaxTemp;
                if (float.IsNaN(minRain)) minRain = aquatic.MinRain;
                if (float.IsNaN(maxRain)) maxRain = aquatic.MaxRain;
                if (float.IsNaN(spreadRate)) spreadRate = aquatic.SpreadRate;
                minForest = 0f;
                maxForest = 1f;
                minFertility = 0;
                sameSpacing = aquatic.SameSpeciesSpacing;
                otherSpacing = aquatic.OtherSpeciesSpacing;
                if (seedDispersalChance <= 0f) seedDispersalChance = aquatic.SeedDispersalChance;
                if (seedDispersalRadius <= 0) seedDispersalRadius = aquatic.SeedDispersalRadius;
            }
            else if (!string.IsNullOrEmpty(species) && WildFernEcology.TryGet(species, out WildFernEcology.EcologyEntry fern))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = fern.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = fern.MaxTemp;
                if (float.IsNaN(minRain)) minRain = fern.MinRain;
                if (float.IsNaN(maxRain)) maxRain = fern.MaxRain;
                if (float.IsNaN(minForest)) minForest = fern.MinForest;
                if (float.IsNaN(maxForest)) maxForest = fern.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = fern.SpreadRate;
                sameSpacing = fern.SameSpeciesSpacing;
                otherSpacing = fern.OtherSpeciesSpacing;
                minSunlight = fern.MinSunlight;
                allowedSoils = fern.Soil.Allowed;
                minGroundFertility = fern.Soil.MinBlockFertility;
                maxGroundFertility = fern.Soil.MaxBlockFertility;
            }
            else if (!string.IsNullOrEmpty(species) && WildShoreSedgeEcology.TryGet(species, out WildShoreSedgeEcology.EcologyEntry shoreSedge))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = shoreSedge.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = shoreSedge.MaxTemp;
                if (float.IsNaN(minRain)) minRain = shoreSedge.MinRain;
                if (float.IsNaN(maxRain)) maxRain = shoreSedge.MaxRain;
                if (float.IsNaN(minForest)) minForest = shoreSedge.MinForest;
                if (float.IsNaN(maxForest)) maxForest = shoreSedge.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = shoreSedge.SpreadRate;
                sameSpacing = shoreSedge.SameSpeciesSpacing;
                otherSpacing = shoreSedge.OtherSpeciesSpacing;
                minSunlight = shoreSedge.MinSunlight;
                allowedSoils = shoreSedge.Soil.Allowed;
                minGroundFertility = shoreSedge.Soil.MinBlockFertility;
                maxGroundFertility = shoreSedge.Soil.MaxBlockFertility;
                if (seedDispersalChance <= 0f) seedDispersalChance = shoreSedge.SeedDispersalChance;
                if (seedDispersalRadius <= 0) seedDispersalRadius = shoreSedge.SeedDispersalRadius;
                if (spreadRadius <= 0 && shoreSedge.MatSpreadRadius > 0) spreadRadius = shoreSedge.MatSpreadRadius;
            }
            else if (!string.IsNullOrEmpty(species) && WildDesertEcology.TryGet(species, out WildDesertEcology.EcologyEntry desert))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = desert.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = desert.MaxTemp;
                if (float.IsNaN(minRain)) minRain = desert.MinRain;
                if (float.IsNaN(maxRain)) maxRain = desert.MaxRain;
                if (float.IsNaN(minForest)) minForest = desert.MinForest;
                if (float.IsNaN(maxForest)) maxForest = desert.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = desert.SpreadRate;
                sameSpacing = desert.SameSpeciesSpacing;
                otherSpacing = desert.OtherSpeciesSpacing;
                minSunlight = desert.MinSunlight;
                allowedSoils = desert.Soil.Allowed;
                minGroundFertility = desert.Soil.MinBlockFertility;
                maxGroundFertility = desert.Soil.MaxBlockFertility;
            }
            else if (!string.IsNullOrEmpty(species) && WildGrassColonizerEcology.TryGet(species, out WildGrassColonizerEcology.EcologyEntry colonizer))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = colonizer.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = colonizer.MaxTemp;
                if (float.IsNaN(minRain)) minRain = colonizer.MinRain;
                if (float.IsNaN(maxRain)) maxRain = colonizer.MaxRain;
                if (float.IsNaN(minForest)) minForest = colonizer.MinForest;
                if (float.IsNaN(maxForest)) maxForest = colonizer.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = colonizer.SpreadRate;
                sameSpacing = colonizer.SameSpeciesSpacing;
                otherSpacing = colonizer.OtherSpeciesSpacing;
                minSunlight = colonizer.MinSunlight;
                allowedSoils = colonizer.Soil.Allowed;
                minGroundFertility = colonizer.Soil.MinBlockFertility;
                maxGroundFertility = colonizer.Soil.MaxBlockFertility;
            }
            else if (!string.IsNullOrEmpty(species) && WildTallgrassEcology.TryGet(species, out WildTallgrassEcology.EcologyEntry grass))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = grass.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = grass.MaxTemp;
                if (float.IsNaN(minRain)) minRain = grass.MinRain;
                if (float.IsNaN(maxRain)) maxRain = grass.MaxRain;
                if (float.IsNaN(minForest)) minForest = grass.MinForest;
                if (float.IsNaN(maxForest)) maxForest = grass.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = grass.SpreadRate;
                sameSpacing = grass.SameSpeciesSpacing;
                otherSpacing = grass.OtherSpeciesSpacing;
                minSunlight = grass.MinSunlight;
                allowedSoils = grass.Soil.Allowed;
                minGroundFertility = grass.Soil.MinBlockFertility;
                maxGroundFertility = grass.Soil.MaxBlockFertility;
            }
            else if (!string.IsNullOrEmpty(species) && WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
            {
                habitat = EcologyHabitat.Terrestrial;
                if (float.IsNaN(minTemp)) minTemp = berry.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = berry.MaxTemp;
                if (float.IsNaN(minRain)) minRain = berry.MinRain;
                if (float.IsNaN(maxRain)) maxRain = berry.MaxRain;
                if (float.IsNaN(minForest)) minForest = berry.MinForest;
                if (float.IsNaN(maxForest)) maxForest = berry.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = berry.SpreadRate;
                sameSpacing = berry.SameSpeciesSpacing;
                otherSpacing = berry.OtherSpeciesSpacing;
                minSunlight = berry.MinSunlight;
                allowedSoils = berry.Soil.Allowed;
                minGroundFertility = berry.Soil.MinBlockFertility;
                maxGroundFertility = berry.Soil.MaxBlockFertility;
                spreadMode = berry.SpreadMode;
                seedDispersalChance = berry.SeedDispersalChance;
                seedDispersalRadius = berry.SeedDispersalRadius;
                if (berry.MatSpreadRadius > 0) spreadRadius = berry.MatSpreadRadius;
                else if (berry.IndependentSpreadRadius > 0) spreadRadius = berry.IndependentSpreadRadius;
            }
            else if (!string.IsNullOrEmpty(species) && WildFerntreeEcology.IsSpecies(species))
            {
                WildFerntreeEcology.Profile ferntree = WildFerntreeEcology.Resolve();
                habitat = EcologyHabitat.Ferntree;
                if (float.IsNaN(minTemp)) minTemp = ferntree.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = ferntree.MaxTemp;
                if (float.IsNaN(minRain)) minRain = ferntree.MinRain;
                if (float.IsNaN(maxRain)) maxRain = ferntree.MaxRain;
                if (float.IsNaN(minForest)) minForest = ferntree.MinForest;
                if (float.IsNaN(maxForest)) maxForest = ferntree.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = ferntree.SpreadRate;
                sameSpacing = ferntree.SameSpeciesSpacing;
                otherSpacing = ferntree.OtherSpeciesSpacing;
                minSunlight = 10;
            }
            else if (!string.IsNullOrEmpty(species) && WildVineEcology.TryGet(species, out WildVineEcology.Profile vine))
            {
                habitat = EcologyHabitat.WildVine;
                if (float.IsNaN(minTemp)) minTemp = vine.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = vine.MaxTemp;
                if (float.IsNaN(minRain)) minRain = vine.MinRain;
                if (float.IsNaN(maxRain)) maxRain = vine.MaxRain;
                if (float.IsNaN(spreadRate)) spreadRate = vine.SpreadRate;
                sameSpacing = vine.SameSpeciesSpacing;
                otherSpacing = vine.OtherSpeciesSpacing;
                minForest = 0f;
                maxForest = 1f;
                minSunlight = 6;
            }
            else if (!string.IsNullOrEmpty(species) && WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree))
            {
                habitat = EcologyHabitat.TerrestrialTree;
                if (float.IsNaN(minTemp)) minTemp = tree.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = tree.MaxTemp;
                if (float.IsNaN(minRain)) minRain = tree.MinRain;
                if (float.IsNaN(maxRain)) maxRain = tree.MaxRain;
                if (float.IsNaN(minForest)) minForest = tree.MinForest;
                if (float.IsNaN(maxForest)) maxForest = tree.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = tree.SpreadRate;
                sameSpacing = tree.SameSpeciesSpacing;
                otherSpacing = tree.OtherSpeciesSpacing;
                minSunlight = tree.SaplingMinSunlight;
            }
            else if (!string.IsNullOrEmpty(species) && WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry ecology))
            {
                if (float.IsNaN(minTemp)) minTemp = ecology.MinTemp;
                if (float.IsNaN(maxTemp)) maxTemp = ecology.MaxTemp;
                if (float.IsNaN(minRain)) minRain = ecology.MinRain;
                if (float.IsNaN(maxRain)) maxRain = ecology.MaxRain;
                if (float.IsNaN(minForest)) minForest = ecology.MinForest;
                if (float.IsNaN(maxForest)) maxForest = ecology.MaxForest;
                if (float.IsNaN(spreadRate)) spreadRate = ecology.SpreadRate;

                if (WildFlowerSpacing.TryGet(species, out WildFlowerSpacing.Profile spacing))
                {
                    sameSpacing = spacing.SameSpecies;
                    otherSpacing = spacing.OtherSpecies;
                    if (spacing.FromSpecies != null && spacing.FromSpecies.Count > 0)
                    {
                        spacingFrom = new Dictionary<string, int>(spacing.FromSpecies);
                    }
                }
            }

#pragma warning restore CS0618

            if (attrs != null)
            {
                int attrSame = attrs["ecologySameSpeciesSpacing"].AsInt(-1);
                if (attrSame >= 0) sameSpacing = attrSame;
                int attrOther = attrs["ecologyOtherSpeciesSpacing"].AsInt(-1);
                if (attrOther >= 0) otherSpacing = attrOther;
            }

            if (float.IsNaN(minTemp)) minTemp = 10f;
            if (float.IsNaN(maxTemp)) maxTemp = 22f;
            if (float.IsNaN(minRain)) minRain = 0f;
            if (float.IsNaN(maxRain)) maxRain = 1f;
            if (float.IsNaN(minForest)) minForest = 0f;
            if (float.IsNaN(maxForest)) maxForest = 1f;
            if (float.IsNaN(spreadRate)) spreadRate = 1f;

            if (spreadRadius <= 0 && !string.IsNullOrEmpty(species)
                && SpeciesEcologyLegacyAccess.TryGetTreeSpreadRadius(species, out int treeRadius))
            {
                spreadRadius = treeRadius;
            }

            if (spreadRadius <= 0 && EcologyFerntreeSpecies.IsKnown(species))
            {
                spreadRadius = SpeciesEcologyLegacyAccess.ResolveFerntreeSpreadRadius();
            }

            var requirements = new PlantRequirements
            {
                Species = species,
                Habitat = habitat,
                SpreadRadius = spreadRadius,
                MaxWaterDepth = maxWaterDepth > 0 ? maxWaterDepth : 1,
                MinWaterDepth = minWaterDepth,
                VerticalBlocks = verticalBlocks > 0 ? verticalBlocks : 1,
                ExactWaterDepth = exactWaterDepth,
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                MinRain = minRain,
                MaxRain = maxRain,
                MinForest = minForest,
                MaxForest = maxForest,
                SpreadRate = spreadRate,
                SameSpeciesSpacing = sameSpacing < 0 ? 0 : sameSpacing,
                OtherSpeciesSpacing = otherSpacing < 0 ? 0 : otherSpacing,
                SpacingFromSpecies = spacingFrom,
                MinFertility = minFertility,
                MinGroundFertility = minGroundFertility,
                MaxGroundFertility = maxGroundFertility,
                AllowedSoilKinds = allowedSoils,
                MinSunlight = minSunlight,
                MinReplaceable = attrs != null ? attrs["minReplaceable"].AsInt(9500) : 9500,
                SpreadMode = spreadMode,
                SuppressRhizomeSpread = suppressRhizomeSpread,
                SuppressSurfaceMatSpread = suppressSurfaceMatSpread,
                SeedDispersalChance = seedDispersalChance,
                SeedDispersalRadius = seedDispersalRadius,
            };

            WildPlantSoil.ApplyTo(requirements);
            WildSpeciesModifiers.ApplyTo(requirements);
            WildSpeciesNiche.ApplyTo(requirements);
            RhizomeSpread.ApplyTo(requirements);
            SurfaceMatSpread.ApplyTo(requirements);
            FernRhizomeSpread.ApplyTo(requirements);
            BerryColonySpread.ApplyTo(requirements);
            ShoreSedgeMatSpread.ApplyTo(requirements);
            return requirements;
        }
    }
}
