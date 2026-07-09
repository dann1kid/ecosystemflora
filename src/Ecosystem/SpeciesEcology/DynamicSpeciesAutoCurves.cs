using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using WildFarming.Ecosystem;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>
    /// B+: discover non-contract species at runtime and ensure they exist in user curve tables (ecology.csv + season.csv).
    /// </summary>
    internal static class DynamicSpeciesAutoCurves
    {
        public static void Apply(ICoreAPI api)
        {
            if (api == null || api.Side != EnumAppSide.Server) return;

            var discovered = DiscoverNonContractSpecies(api);
            if (discovered.Count == 0) return;

            // Persist discovered ids so user CSV validation accepts them on next boot.
            DiscoveredSpeciesStore.AddAndPersist(api, discovered);
            SpeciesEcologyCatalogIndex.SeedDiscoveredFromStore(DiscoveredSpeciesStore.All());

            EnsureEcologyRows(api, discovered);
            EnsureSeasonRows(api, discovered);

            // Refresh in-memory registries immediately (so /I inspect sees the new rows).
            SpeciesEcologyLoadService.TryReload(api, out _, out _);
        }

        static HashSet<string> DiscoverNonContractSpecies(ICoreAPI api)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (api?.World?.Blocks == null) return set;

            IList<Block> blocks = api.World.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                Block b = blocks[i];
                if (b?.Code == null) continue;
                if (b.Code.Path != null && b.Code.Path.Contains("-harvested-", StringComparison.Ordinal)) continue;
                if (!PlantCodeHelper.IsEcologySpreadParent(b)) continue;

                string species = PlantCodeHelper.ResolveEcologySpecies(b);
                if (string.IsNullOrWhiteSpace(species)) continue;
                if (SpeciesEcologyCatalogIndex.IsContractSpecies(species)) continue;

                set.Add(species.Trim());
            }

            return set;
        }

        static void EnsureEcologyRows(ICoreAPI api, IReadOnlyCollection<string> species)
        {
            string path = SpeciesEcologyUserConfig.GetEcologyCsvPath(api);
            Directory.CreateDirectory(SpeciesEcologyUserConfig.GetSpeciesFolder(api));

            HashSet<string> present = SpeciesEcologyCsvReader.ReadSpeciesKeys(path);
            var missing = new List<SpeciesEcologyCsvRow>();

            foreach (string s in species)
            {
                if (present.Contains(s)) continue;
                SpeciesEcologyCsvRow row = BuildEcologyRowSeed(api, s);
                if (row != null) missing.Add(row);
            }

            AppendEcologyRows(path, missing);
        }

        static void EnsureSeasonRows(ICoreAPI api, IReadOnlyCollection<string> species)
        {
            string path = SpeciesEcologyUserConfig.GetSeasonCsvPath(api);
            Directory.CreateDirectory(SpeciesEcologyUserConfig.GetSpeciesFolder(api));

            HashSet<string> present = SpeciesEcologyCsvReader.ReadSpeciesKeys(path);
            var missing = new List<SpeciesSeasonCsvRow>();

            foreach (string s in species)
            {
                if (present.Contains(s)) continue;
                missing.Add(BuildSeasonRowSeed(api, s));
            }

            AppendSeasonRows(path, missing);
        }

        static SpeciesEcologyCsvRow BuildEcologyRowSeed(ICoreAPI api, string species)
        {
            // Find any representative block for this species to derive defaults.
            Block representative = FindRepresentativeBlock(api, species);
            PlantRequirements req = representative != null ? PlantRequirements.FromBlock(representative) : null;

            var row = new SpeciesEcologyCsvRow { Species = species };
            if (req == null)
            {
                // Generic fallback (keeps simulation functional until the user tunes).
                row.Taxon = "flower";
                row.Habitat = EcologyHabitat.Terrestrial.ToString();
                row.MinTemp = -5f;
                row.MaxTemp = 50f;
                row.MinRain = 0f;
                row.MaxRain = 1f;
                row.MinForest = 0f;
                row.MaxForest = 1f;
                row.SpreadRate = 1f;
                row.SpreadMode = SpreadMode.Independent.ToString();
                row.SameSpeciesSpacing = 0;
                row.OtherSpeciesSpacing = 3;
                return row;
            }

            row.Habitat = req.Habitat.ToString();
            row.MinTemp = req.MinTemp;
            row.MaxTemp = req.MaxTemp;
            row.MinRain = req.MinRain;
            row.MaxRain = req.MaxRain;
            row.MinForest = req.MinForest;
            row.MaxForest = req.MaxForest;
            row.SpreadRate = req.SpreadRate;
            row.SpreadMode = req.SpreadMode.ToString();
            row.SeedDispersalChance = req.SeedDispersalChance;
            row.SeedDispersalRadius = req.SeedDispersalRadius;
            row.SpreadRadius = req.SpreadRadius;
            row.SameSpeciesSpacing = req.SameSpeciesSpacing < 0 ? 0 : req.SameSpeciesSpacing;
            row.OtherSpeciesSpacing = req.OtherSpeciesSpacing < 0 ? 3 : req.OtherSpeciesSpacing;
            row.MinSunlight = req.MinSunlight;

            row.Taxon = InferTaxon(req);
            if (row.Taxon == "tree")
            {
                row.TreeSeralRole = "Mid";
            }

            return row;
        }

        static string InferTaxon(PlantRequirements req)
        {
            if (req == null) return "flower";
            if (req.Habitat == EcologyHabitat.TerrestrialTree) return "tree";
            if (req.UsesBerryColonySpread) return "berry";
            if (req.UsesFernRhizomeSpread) return "fern";
            if (req.Habitat == EcologyHabitat.WaterSurface || req.Habitat == EcologyHabitat.UnderwaterColumn) return "aquatic";
            if (req.Habitat == EcologyHabitat.ReedNearWater) return "shore_sedge";
            return "flower";
        }

        static Block FindRepresentativeBlock(ICoreAPI api, string species)
        {
            if (api?.World?.Blocks == null) return null;
            IList<Block> blocks = api.World.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                Block b = blocks[i];
                if (b?.Code == null) continue;
                if (!PlantCodeHelper.IsEcologySpreadParent(b)) continue;
                string s = PlantCodeHelper.ResolveEcologySpecies(b);
                if (string.Equals(s, species, StringComparison.Ordinal)) return b;
            }
            return null;
        }

        static SpeciesSeasonCsvRow BuildSeasonRowSeed(ICoreAPI api, string species)
        {
            Block representative = FindRepresentativeBlock(api, species);
            PlantRequirements req = representative != null ? PlantRequirements.FromBlock(representative) : null;
            string taxon = InferTaxon(req);

            var row = new SpeciesSeasonCsvRow { Species = species };
            FillSeasonCurve(row, taxon, req);
            return row;
        }

        static void FillSeasonCurve(SpeciesSeasonCsvRow row, string taxon, PlantRequirements req)
        {
            // Months: Jan..Dec (0..11)
            float[] spread;
            float[] stress;

            switch (taxon)
            {
                case "tree":
                    // Trees: burst spring/early summer; near-dormant winter.
                    spread = new[] { 0.05f, 0.08f, 0.18f, 0.45f, 0.85f, 1.00f, 0.90f, 0.70f, 0.35f, 0.15f, 0.08f, 0.05f };
                    stress = new[] { 0.08f, 0.06f, 0.03f, 0.01f, 0.00f, 0.00f, 0.00f, 0.01f, 0.02f, 0.04f, 0.06f, 0.08f };
                    break;

                case "berry":
                    // Berry mats: strongest summer; stress in winter/fall.
                    spread = new[] { 0.02f, 0.05f, 0.15f, 0.50f, 0.90f, 1.10f, 1.15f, 0.95f, 0.55f, 0.25f, 0.10f, 0.03f };
                    stress = new[] { 0.10f, 0.08f, 0.04f, 0.02f, 0.00f, 0.00f, 0.00f, 0.01f, 0.03f, 0.06f, 0.08f, 0.10f };
                    break;

                case "fern":
                    // Ferns: spring/summer; a bit more winter stress.
                    spread = new[] { 0.03f, 0.06f, 0.20f, 0.60f, 1.00f, 1.10f, 1.05f, 0.85f, 0.45f, 0.18f, 0.08f, 0.04f };
                    stress = new[] { 0.12f, 0.10f, 0.05f, 0.02f, 0.00f, 0.00f, 0.00f, 0.01f, 0.03f, 0.06f, 0.10f, 0.12f };
                    break;

                case "aquatic":
                case "shore_sedge":
                    // Water plants: less seasonal; low stress (water buffers temperature).
                    spread = new[] { 0.35f, 0.40f, 0.55f, 0.75f, 0.95f, 1.05f, 1.05f, 0.95f, 0.75f, 0.55f, 0.40f, 0.35f };
                    stress = new[] { 0.03f, 0.03f, 0.02f, 0.01f, 0.00f, 0.00f, 0.00f, 0.00f, 0.01f, 0.02f, 0.03f, 0.03f };
                    break;

                default:
                    // Flowers / unknown: broad growing season, mild stress.
                    spread = new[] { 0.05f, 0.10f, 0.25f, 0.65f, 1.00f, 1.15f, 1.10f, 0.90f, 0.55f, 0.25f, 0.10f, 0.05f };
                    stress = new[] { 0.06f, 0.05f, 0.03f, 0.01f, 0.00f, 0.00f, 0.00f, 0.00f, 0.01f, 0.03f, 0.05f, 0.06f };
                    break;
            }

            float winterStressScale = WinterStressScale(req);
            for (int i = 0; i < 12; i++)
            {
                row.Spread[i] = spread[i];

                // Scale only winter-heavy months; keep shoulder months stable.
                bool winterish = (i == 11 || i == 0 || i == 1);
                row.Stress[i] = winterish ? Clamp01(stress[i] * winterStressScale) : stress[i];
            }
        }

        static float WinterStressScale(PlantRequirements req)
        {
            // minTemp: more cold-tolerant → lower winter stress.
            if (req == null) return 1f;

            float t = req.MinTemp;
            // Map [-30..+10] → [0.55..1.35]
            float x = (t + 30f) / 40f; // 0 at -30, 1 at +10
            float scale = 1.35f - (0.80f * Clamp01(x));
            return Clamp(scale, 0.55f, 1.35f);
        }

        static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        static void AppendEcologyRows(string userCsvPath, List<SpeciesEcologyCsvRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            bool writeHeader = !File.Exists(userCsvPath) || new FileInfo(userCsvPath).Length == 0;
            using (var writer = new StreamWriter(userCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine(string.Join(",", SpeciesEcologyCsvSchema.Columns));
                }

                rows.Sort((a, b) => string.CompareOrdinal(a.Species, b.Species));
                for (int i = 0; i < rows.Count; i++)
                {
                    writer.WriteLine(SpeciesEcologyCsvWriter.FormatRowLine(rows[i]));
                }
            }
        }

        static void AppendSeasonRows(string userCsvPath, List<SpeciesSeasonCsvRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            bool writeHeader = !File.Exists(userCsvPath) || new FileInfo(userCsvPath).Length == 0;
            using (var writer = new StreamWriter(userCsvPath, append: true))
            {
                if (writeHeader)
                {
                    writer.WriteLine(string.Join(",", SpeciesSeasonCsvSchema.Columns));
                }

                rows.Sort((a, b) => string.CompareOrdinal(a.Species, b.Species));
                for (int i = 0; i < rows.Count; i++)
                {
                    writer.WriteLine(SpeciesSeasonCsvWriter.FormatRowLine(rows[i]));
                }
            }
        }
    }
}

