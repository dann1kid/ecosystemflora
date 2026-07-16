using System.Collections.Generic;

#pragma warning disable CS0618

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>Snapshot of C# ecology tables into flat rows for CSV tuning.</summary>
    internal static class SpeciesEcologyExporter
    {
        public static IReadOnlyList<SpeciesEcologyCsvRow> ExportAll()
        {
            IReadOnlyList<SpeciesEcologyCatalog.Entry> catalog = SpeciesEcologyCatalog.All();
            var rows = new List<SpeciesEcologyCsvRow>(catalog.Count);
            for (int i = 0; i < catalog.Count; i++)
            {
                rows.Add(Export(catalog[i]));
            }

            return rows;
        }

        public static SpeciesEcologyCsvRow Export(SpeciesEcologyCatalog.Entry entry) =>
            Export(entry.Species, entry.Taxon);

        public static SpeciesEcologyCsvRow Export(string species, string taxon)
        {
            var row = new SpeciesEcologyCsvRow
            {
                Species = species,
                Taxon = taxon,
            };

            ApplyClimate(row, species, taxon);
            ApplySpreadExtras(row, species, taxon);
            ApplySoil(row, species);
            ApplyModifiers(row, species);
            ApplyNiche(row, species);
            ApplySeason(row, species);
            ApplyMaturation(row, species, taxon);
            ApplyFlowerPhenologyLife(row, species, taxon);
            ApplyTreeRole(row, species, taxon);
            ApplySoilSuccession(row, species);

            return row;
        }

        static void ApplyFlowerPhenologyLife(SpeciesEcologyCsvRow row, string species, string taxon)
        {
            int cycles = WildFlowerPhenologyLife.ResolveForExport(species, taxon);
            if (cycles > 0) row.FlowerPhenologyLifeCycles = cycles;
        }

        static void ApplyClimate(SpeciesEcologyCsvRow row, string species, string taxon)
        {
            if (WildFlowerClimate.TryGet(species, out WildFlowerClimate.EcologyEntry flower))
            {
                row.MinTemp = flower.MinTemp;
                row.MaxTemp = flower.MaxTemp;
                row.MinRain = flower.MinRain;
                row.MaxRain = flower.MaxRain;
                row.MinForest = flower.MinForest;
                row.MaxForest = flower.MaxForest;
                row.SpreadRate = flower.SpreadRate;
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildFernEcology.TryGet(species, out WildFernEcology.EcologyEntry fern))
            {
                row.MinTemp = fern.MinTemp;
                row.MaxTemp = fern.MaxTemp;
                row.MinRain = fern.MinRain;
                row.MaxRain = fern.MaxRain;
                row.MinForest = fern.MinForest;
                row.MaxForest = fern.MaxForest;
                row.SpreadRate = fern.SpreadRate;
                row.SameSpeciesSpacing = fern.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = fern.OtherSpeciesSpacing;
                row.MinSunlight = fern.MinSunlight;
                ApplySoilProfile(row, fern.Soil);
                row.SpreadMode = SpreadMode.FernRhizomeMat.ToString();
                return;
            }

            if (WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
            {
                row.MinTemp = berry.MinTemp;
                row.MaxTemp = berry.MaxTemp;
                row.MinRain = berry.MinRain;
                row.MaxRain = berry.MaxRain;
                row.MinForest = berry.MinForest;
                row.MaxForest = berry.MaxForest;
                row.SpreadRate = berry.SpreadRate;
                row.SameSpeciesSpacing = berry.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = berry.OtherSpeciesSpacing;
                row.MinSunlight = berry.MinSunlight;
                row.SpreadMode = berry.SpreadMode.ToString();
                row.MatConnectivity = berry.MatConnectivity.ToString();
                row.SeedDispersalChance = berry.SeedDispersalChance;
                row.SeedDispersalRadius = berry.SeedDispersalRadius;
                row.MatSpreadRadius = berry.MatSpreadRadius;
                row.IndependentSpreadRadius = berry.IndependentSpreadRadius;
                ApplySoilProfile(row, berry.Soil);
                return;
            }

            if (WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree))
            {
                row.MinTemp = tree.MinTemp;
                row.MaxTemp = tree.MaxTemp;
                row.MinRain = tree.MinRain;
                row.MaxRain = tree.MaxRain;
                row.MinForest = tree.MinForest;
                row.MaxForest = tree.MaxForest;
                row.SpreadRate = tree.SpreadRate;
                row.SpreadRadius = tree.SpreadRadius;
                row.SameSpeciesSpacing = tree.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = tree.OtherSpeciesSpacing;
                row.MinSunlight = tree.SaplingMinSunlight;
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildTallgrassEcology.TryGet(species, out WildTallgrassEcology.EcologyEntry tallgrass))
            {
                CopyEntry(row, tallgrass.MinTemp, tallgrass.MaxTemp, tallgrass.MinRain, tallgrass.MaxRain,
                    tallgrass.MinForest, tallgrass.MaxForest, tallgrass.SpreadRate,
                    tallgrass.SameSpeciesSpacing, tallgrass.OtherSpeciesSpacing, tallgrass.MinSunlight, tallgrass.Soil);
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildGrassColonizerEcology.TryGet(species, out WildGrassColonizerEcology.EcologyEntry colonizer))
            {
                CopyEntry(row, colonizer.MinTemp, colonizer.MaxTemp, colonizer.MinRain, colonizer.MaxRain,
                    colonizer.MinForest, colonizer.MaxForest, colonizer.SpreadRate,
                    colonizer.SameSpeciesSpacing, colonizer.OtherSpeciesSpacing, colonizer.MinSunlight, colonizer.Soil);
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildShoreSedgeEcology.TryGet(species, out WildShoreSedgeEcology.EcologyEntry sedge))
            {
                CopyEntry(row, sedge.MinTemp, sedge.MaxTemp, sedge.MinRain, sedge.MaxRain,
                    sedge.MinForest, sedge.MaxForest, sedge.SpreadRate,
                    sedge.SameSpeciesSpacing, sedge.OtherSpeciesSpacing, sedge.MinSunlight, sedge.Soil);
                row.SpreadMode = SpreadMode.ShoreSedgeMat.ToString();
                row.SeedDispersalChance = sedge.SeedDispersalChance;
                row.SeedDispersalRadius = sedge.SeedDispersalRadius;
                row.MatSpreadRadius = sedge.MatSpreadRadius;
                return;
            }

            if (WildDesertEcology.TryGet(species, out WildDesertEcology.EcologyEntry desert))
            {
                CopyEntry(row, desert.MinTemp, desert.MaxTemp, desert.MinRain, desert.MaxRain,
                    desert.MinForest, desert.MaxForest, desert.SpreadRate,
                    desert.SameSpeciesSpacing, desert.OtherSpeciesSpacing, desert.MinSunlight, desert.Soil);
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildAquaticEcology.TryGet(species, out WildAquaticEcology.Profile aquatic))
            {
                row.MinTemp = aquatic.MinTemp;
                row.MaxTemp = aquatic.MaxTemp;
                row.MinRain = aquatic.MinRain;
                row.MaxRain = aquatic.MaxRain;
                row.SpreadRate = aquatic.SpreadRate;
                row.SameSpeciesSpacing = aquatic.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = aquatic.OtherSpeciesSpacing;
                row.Habitat = aquatic.Habitat.ToString();
                row.WaterMaxDepth = aquatic.MaxWaterDepth;
                row.WaterMinDepth = aquatic.MinWaterDepth;
                row.WaterVerticalBlocks = aquatic.VerticalBlocks;
                row.WaterExactDepth = aquatic.ExactWaterDepth;
                row.SeedDispersalChance = aquatic.SeedDispersalChance;
                row.SeedDispersalRadius = aquatic.SeedDispersalRadius;
                row.SpreadMode = aquatic.Habitat == EcologyHabitat.WaterSurface
                    ? SpreadMode.SurfaceMat.ToString()
                    : SpreadMode.RhizomeMat.ToString();
                return;
            }

            if (species == WildFerntreeEcology.Species)
            {
                WildFerntreeEcology.Profile ferntree = WildFerntreeEcology.Default;
                row.MinTemp = ferntree.MinTemp;
                row.MaxTemp = ferntree.MaxTemp;
                row.MinRain = ferntree.MinRain;
                row.MaxRain = ferntree.MaxRain;
                row.MinForest = ferntree.MinForest;
                row.MaxForest = ferntree.MaxForest;
                row.SpreadRate = ferntree.SpreadRate;
                row.SpreadRadius = ferntree.SpreadRadius;
                row.SameSpeciesSpacing = ferntree.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = ferntree.OtherSpeciesSpacing;
                row.SpreadMode = SpreadMode.Independent.ToString();
                return;
            }

            if (WildVineEcology.TryGet(species, out WildVineEcology.Profile vine))
            {
                row.MinTemp = vine.MinTemp;
                row.MaxTemp = vine.MaxTemp;
                row.MinRain = vine.MinRain;
                row.MaxRain = vine.MaxRain;
                row.SpreadRate = vine.SpreadRate;
                row.SameSpeciesSpacing = vine.SameSpeciesSpacing;
                row.OtherSpeciesSpacing = vine.OtherSpeciesSpacing;
                row.SpreadMode = SpreadMode.Independent.ToString();
            }
        }

        static void CopyEntry(
            SpeciesEcologyCsvRow row,
            float minTemp, float maxTemp,
            float minRain, float maxRain,
            float minForest, float maxForest,
            float spreadRate,
            int sameSpacing, int otherSpacing,
            int minSunlight,
            WildPlantSoil.Profile soil)
        {
            row.MinTemp = minTemp;
            row.MaxTemp = maxTemp;
            row.MinRain = minRain;
            row.MaxRain = maxRain;
            row.MinForest = minForest;
            row.MaxForest = maxForest;
            row.SpreadRate = spreadRate;
            row.SameSpeciesSpacing = sameSpacing;
            row.OtherSpeciesSpacing = otherSpacing;
            row.MinSunlight = minSunlight;
            ApplySoilProfile(row, soil);
        }

        static void ApplySpreadExtras(SpeciesEcologyCsvRow row, string species, string taxon)
        {
            if (taxon == "flower" && WildFlowerSpacing.TryGet(species, out WildFlowerSpacing.Profile spacing))
            {
                row.SameSpeciesSpacing = spacing.SameSpecies;
                row.OtherSpeciesSpacing = spacing.OtherSpecies;
                if (spacing.FromSpecies != null && spacing.FromSpecies.Count > 0)
                {
                    row.SpacingFromSpecies = SpacingFromSpeciesCodec.Format(spacing.FromSpecies);
                }
            }
        }

        static void ApplySoil(SpeciesEcologyCsvRow row, string species)
        {
            if (!string.IsNullOrEmpty(row.SoilKinds)) return;

            if (WildPlantSoil.TryGet(species, out WildPlantSoil.Profile soil))
            {
                ApplySoilProfile(row, soil);
                return;
            }

            if (WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
            {
                ApplySoilProfile(row, berry.Soil);
            }
        }

        static void ApplySoilProfile(SpeciesEcologyCsvRow row, WildPlantSoil.Profile soil)
        {
            row.SoilKinds = SoilKindFormatter.Format(soil.Allowed);
            row.SoilMinFertility = soil.MinBlockFertility;
            row.SoilMaxFertility = soil.MaxBlockFertility;
        }

        static void ApplyModifiers(SpeciesEcologyCsvRow row, string species)
        {
            if (!WildSpeciesModifiers.TryGet(species, out WildSpeciesModifiers.Profile profile)) return;

            row.ContextAffinity = profile.ContextAffinity.ToString();
            row.ContextBonus = profile.ContextBonus;
            row.ForestInteriorPenalty = profile.ForestInteriorPenalty;
            row.HoldStrength = profile.HoldStrength;
        }

        static void ApplyNiche(SpeciesEcologyCsvRow row, string species)
        {
            if (!WildSpeciesNiche.TryGet(species, out WildSpeciesNiche.Profile profile)) return;

            row.Moisture = profile.PreferredMoisture.ToString();
            row.Light = profile.PreferredLight.ToString();
            row.NicheBonus = profile.NicheBonus;
        }

        static void ApplySeason(SpeciesEcologyCsvRow row, string species)
        {
            row.SeasonExplicit = WildSpeciesSeason.HasExplicitSeasonProfile(species);
        }

        static void ApplyMaturation(SpeciesEcologyCsvRow row, string species, string taxon)
        {
            if (taxon == "flower" && WildFlowerMaturation.TryGetProfile(species, out WildFlowerMaturation.Profile flower))
            {
                row.FlowerMaturationHours = flower.MaturationHours;
                row.FlowerCooldownHours = flower.PostSpreadAttemptCooldownHours;
            }

            if (taxon == "fern" && WildFernSpread.TryGetProfile(species, out WildFernSpread.Profile fern))
            {
                row.FernMaturationHours = fern.MaturationHours;
                row.FernCooldownHours = fern.PostSpreadAttemptCooldownHours;
            }

            if (taxon == "berry" && WildBerryEcology.TryGet(species, out WildBerryEcology.Profile berry))
            {
                row.BerryMaturationHours = 168d / System.Math.Max(0.25f, berry.SpreadRate);
            }
        }

        static void ApplyTreeRole(SpeciesEcologyCsvRow row, string species, string taxon)
        {
            if (taxon != "tree") return;
            if (!WildTreeEcology.TryGet(species, out WildTreeEcology.Profile tree)) return;
            row.TreeSeralRole = tree.SeralRole.ToString();
        }

        static void ApplySoilSuccession(SpeciesEcologyCsvRow row, string species)
        {
            if (!WildSpeciesSoilSuccession.TryGetRole(species, out PlantSoilRole role)) return;
            row.SoilSuccessionRole = role.ToString();
        }
    }
}

#pragma warning restore CS0618
