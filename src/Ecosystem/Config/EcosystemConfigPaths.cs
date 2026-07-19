using System;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Layout under <c>ModConfig/ecosystemflora/</c>:
    /// <list type="bullet">
    /// <item><c>template/defaults.json</c> — seed for brand-new worlds</item>
    /// <item><c>presets/*.json</c> — shared balance presets</item>
    /// <item><c>species/*.csv</c> — ecology tables (existing)</item>
    /// <item><c>worlds/&lt;worldKey&gt;/*.json</c> — per-world U/perf settings by category</item>
    /// </list>
    /// </summary>
    public static class EcosystemConfigPaths
    {
        public const string RootFolderName = "ecosystemflora";
        public const string TemplateFolderName = "template";
        public const string TemplateFileName = "defaults.json";
        public const string PresetsFolderName = "presets";
        public const string WorldsFolderName = "worlds";
        public const string SpeciesFolderName = "species";
        public const string MetaFileName = "meta.json";
        public const string WorldInfoFileName = "world.json";
        /// <summary>Presence = setup wizard finished for this world (authoritative; survives sync races).</summary>
        public const string WizardDoneFileName = "setup-wizard.done";

        public const string LegacyFlatConfigFileName = "ecosystemflora.json";
        public const string LegacyPresetsFolderName = "ecosystemflora.presets";

        /// <summary>Category JSON files written for each world (UI categories + meta).</summary>
        public static readonly string[] WorldCategoryFiles =
        {
            "master",
            "spread",
            "aquatic",
            "competition",
            "stress",
            "trees",
            "canopy",
            "mycelium",
            "soil",
            "harvest",
            "scope",
            "perf",
            "advanced",
        };

        public static string GetModConfigRoot(ICoreAPI api) =>
            api.GetOrCreateDataPath("ModConfig");

        public static string GetEcosystemRoot(ICoreAPI api) =>
            Path.Combine(GetModConfigRoot(api), RootFolderName);

        public static string GetTemplateFolder(ICoreAPI api) =>
            Path.Combine(GetEcosystemRoot(api), TemplateFolderName);

        public static string GetTemplateDefaultsPath(ICoreAPI api) =>
            Path.Combine(GetTemplateFolder(api), TemplateFileName);

        public static string GetPresetsFolder(ICoreAPI api) =>
            Path.Combine(GetEcosystemRoot(api), PresetsFolderName);

        public static string GetWorldsRoot(ICoreAPI api) =>
            Path.Combine(GetEcosystemRoot(api), WorldsFolderName);

        public static string GetWorldFolder(ICoreAPI api, string worldKey) =>
            Path.Combine(GetWorldsRoot(api), worldKey ?? "world");

        public static string GetWorldCategoryPath(ICoreAPI api, string worldKey, string category) =>
            Path.Combine(GetWorldFolder(api, worldKey), category + ".json");

        public static string GetWorldMetaPath(ICoreAPI api, string worldKey) =>
            Path.Combine(GetWorldFolder(api, worldKey), MetaFileName);

        public static string GetWizardDonePath(ICoreAPI api, string worldKey) =>
            Path.Combine(GetWorldFolder(api, worldKey), WizardDoneFileName);

        public static string GetWorldInfoPath(ICoreAPI api, string worldKey) =>
            Path.Combine(GetWorldFolder(api, worldKey), WorldInfoFileName);

        public static string ResolveWorldKey(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return "world";

            var sg = sapi.WorldManager.SaveGame;
            string name = string.IsNullOrWhiteSpace(sg.WorldName) ? "world" : sg.WorldName.Trim();
            long seed = 0;
            try { seed = sg.Seed; }
            catch { /* older API */ }

            return SanitizeFolderName(name) + "_" + seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "world";

            var sb = new StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append('_');
                }
            }

            string result = sb.ToString().Trim('_', '.');
            if (string.IsNullOrEmpty(result)) result = "world";
            if (result.Length > 80) result = result.Substring(0, 80);
            return result;
        }

        public static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            Directory.CreateDirectory(path);
        }

        /// <summary>Move legacy flat ModConfig files into the ecosystemflora tree (idempotent).</summary>
        public static void MigrateLegacyLayout(ICoreAPI api)
        {
            if (api == null) return;

            string modConfig = GetModConfigRoot(api);
            EnsureDirectory(GetEcosystemRoot(api));
            EnsureDirectory(GetTemplateFolder(api));
            EnsureDirectory(GetPresetsFolder(api));
            EnsureDirectory(GetWorldsRoot(api));

            string legacyFlat = Path.Combine(modConfig, LegacyFlatConfigFileName);
            string templatePath = GetTemplateDefaultsPath(api);
            if (File.Exists(legacyFlat) && !File.Exists(templatePath))
            {
                try
                {
                    File.Copy(legacyFlat, templatePath, overwrite: false);
                    api.Logger.Notification(
                        "[ecosystemflora] Migrated {0} → {1}",
                        LegacyFlatConfigFileName,
                        Path.Combine(RootFolderName, TemplateFolderName, TemplateFileName));
                }
                catch (Exception ex)
                {
                    api.Logger.Warning("[ecosystemflora] Template migrate failed: {0}", ex.Message);
                }
            }

            string legacyPresets = Path.Combine(modConfig, LegacyPresetsFolderName);
            string newPresets = GetPresetsFolder(api);
            if (Directory.Exists(legacyPresets))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(legacyPresets, "*.json"))
                    {
                        string dest = Path.Combine(newPresets, Path.GetFileName(file));
                        if (!File.Exists(dest))
                        {
                            File.Copy(file, dest, overwrite: false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    api.Logger.Warning("[ecosystemflora] Presets migrate failed: {0}", ex.Message);
                }
            }
        }
    }
}
