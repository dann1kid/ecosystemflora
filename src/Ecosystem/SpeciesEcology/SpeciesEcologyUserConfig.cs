using System.IO;
using Vintagestory.API.Common;

namespace WildFarming.Ecosystem.SpeciesEcology
{
    /// <summary>ModConfig paths for editable species CSV tables.</summary>
    internal static class SpeciesEcologyUserConfig
    {
        public const string ConfigSubfolder = "ecosystemflora/species";
        public const string EcologyFileName = "ecology.csv";
        public const string SeasonFileName = "season.csv";

        public const string LegacyEcologyFileName = "ecosystemflora.species.csv";
        public const string LegacySeasonFileName = "ecosystemflora.species.season.csv";

        public static string GetSpeciesFolder(ICoreAPI api) =>
            Path.Combine(api.GetOrCreateDataPath("ModConfig"), ConfigSubfolder);

        public static string GetEcologyCsvPath(ICoreAPI api) =>
            Path.Combine(GetSpeciesFolder(api), EcologyFileName);

        public static string GetSeasonCsvPath(ICoreAPI api) =>
            Path.Combine(GetSpeciesFolder(api), SeasonFileName);

        /// <summary>Move flat legacy ModConfig CSV files into <see cref="ConfigSubfolder"/>.</summary>
        public static void MigrateLegacyFiles(ICoreAPI api)
        {
            if (api == null) return;

            string modConfig = api.GetOrCreateDataPath("ModConfig");
            TryMigrate(Path.Combine(modConfig, LegacyEcologyFileName), GetEcologyCsvPath(api));
            TryMigrate(Path.Combine(modConfig, LegacySeasonFileName), GetSeasonCsvPath(api));
        }

        static void TryMigrate(string legacyPath, string newPath)
        {
            if (!File.Exists(legacyPath) || File.Exists(newPath)) return;

            string dir = Path.GetDirectoryName(newPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.Move(legacyPath, newPath);
        }
    }
}
