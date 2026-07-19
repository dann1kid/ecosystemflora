using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Per-world settings on disk under <c>ModConfig/ecosystemflora/worlds/&lt;key&gt;/</c>
    /// (category JSON files). Migrates legacy SaveGame blob and flat ModConfig template.
    /// </summary>
    public static class EcosystemWorldConfigStore
    {
        /// <summary>Legacy SaveGame key (migrated once into world folder files).</summary>
        public const string SaveKey = "ecosystemflora:config";

        static ICoreServerAPI boundApi;
        static bool eventsBound;
        static string activeWorldKey;

        public static string ActiveWorldKey => activeWorldKey;

        public static void Bind(ICoreServerAPI sapi)
        {
            if (sapi == null) return;
            boundApi = sapi;
            if (eventsBound) return;

            sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
            sapi.Event.GameWorldSave += OnGameWorldSave;
            eventsBound = true;

            TryLoadOrMigrateNow(sapi);
        }

        public static void Unbind(ICoreServerAPI sapi)
        {
            if (sapi == null || !eventsBound) return;
            if (boundApi != null && !ReferenceEquals(boundApi, sapi)) return;

            sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
            sapi.Event.GameWorldSave -= OnGameWorldSave;
            eventsBound = false;
            boundApi = null;
            activeWorldKey = null;
        }

        static void OnSaveGameLoaded()
        {
            if (boundApi == null) return;
            LoadOrMigrate(boundApi);
            EcosystemSystem.Instance?.TryRefreshTickIntervals();
        }

        static void OnGameWorldSave()
        {
            if (boundApi == null) return;
            Persist(boundApi);
        }

        public static void TryLoadOrMigrateNow(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            LoadOrMigrate(sapi);
        }

        public static void LoadOrMigrate(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;

            EcosystemConfigPaths.MigrateLegacyLayout(sapi);
            activeWorldKey = EcosystemConfigPaths.ResolveWorldKey(sapi);
            string worldDir = EcosystemConfigPaths.GetWorldFolder(sapi, activeWorldKey);
            EcosystemConfigPaths.EnsureDirectory(worldDir);

            if (TryLoadFromWorldFolder(sapi, activeWorldKey, out EcosystemConfig fromFiles))
            {
                EcosystemConfigValidator.NormalizeInPlace(fromFiles);
                EcosystemBalancePresets.TryLoadFilePresets(sapi);
                ApplyKnownPreset(fromFiles);
                EcosystemConfig.Loaded = fromFiles;
                sapi.Logger.Notification(
                    "[ecosystemflora] Loaded world settings from {0}",
                    RelativeWorldPath(activeWorldKey));
                return;
            }

            // Legacy SaveGame blob → export to files once.
            if (TryLoadLegacySaveBlob(sapi, out EcosystemConfig fromBlob))
            {
                EcosystemConfigValidator.NormalizeInPlace(fromBlob);
                EcosystemBalancePresets.TryLoadFilePresets(sapi);
                ApplyKnownPreset(fromBlob);
                EcosystemConfig.Loaded = fromBlob;
                Persist(sapi);
                ClearLegacySaveBlob(sapi);
                sapi.Logger.Notification(
                    "[ecosystemflora] Migrated SaveGame config blob → {0}",
                    RelativeWorldPath(activeWorldKey));
                return;
            }

            // Brand-new world: seed from template, force wizard.
            EcosystemBalancePresets.TryLoadFilePresets(sapi);
            ApplyKnownPreset(EcosystemConfig.Loaded);
            PrepareFreshWorldConfig(EcosystemConfig.Loaded);
            Persist(sapi);
            sapi.Logger.Notification(
                "[ecosystemflora] Seeded world settings at {0}; setup wizard pending.",
                RelativeWorldPath(activeWorldKey));
        }

        public static void Persist(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            if (EcosystemConfig.Loaded == null) return;

            try
            {
                if (string.IsNullOrEmpty(activeWorldKey))
                {
                    activeWorldKey = EcosystemConfigPaths.ResolveWorldKey(sapi);
                }

                WriteWorldFolder(sapi, activeWorldKey, EcosystemConfig.Loaded);
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning("[ecosystemflora] Failed writing world config files: {0}", ex.Message);
            }
        }

        public static bool HasWorldFiles(ICoreAPI api, string worldKey)
        {
            if (api == null || string.IsNullOrEmpty(worldKey)) return false;
            string meta = EcosystemConfigPaths.GetWorldMetaPath(api, worldKey);
            if (File.Exists(meta)) return true;
            foreach (string category in EcosystemConfigPaths.WorldCategoryFiles)
            {
                if (File.Exists(EcosystemConfigPaths.GetWorldCategoryPath(api, worldKey, category)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether a world blob exists (legacy diagnostics).</summary>
        public static bool HasWorldBlob(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return false;
            try
            {
                byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
                return data != null && data.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static void PrepareFreshWorldConfig(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            cfg.SetupWizardCompleted = false;
            cfg.LastAutoTuneTier = "";
            cfg.LastAutoTuneOpsPerMs = 0;
            cfg.LastAutoTuneElapsedMs = 0;
            cfg.LastAutoTuneUtc = "";
        }

        public static EcosystemConfig CloneAsGlobalTemplate(EcosystemConfig source)
        {
            EcosystemConfig clone = EcosystemConfigCopier.Clone(source ?? new EcosystemConfig());
            PrepareFreshWorldConfig(clone);
            return clone;
        }

        public static void WriteTemplateDefaults(ICoreAPI api, EcosystemConfig cfg)
        {
            if (api == null || cfg == null) return;
            EcosystemConfigPaths.MigrateLegacyLayout(api);
            EcosystemConfig template = CloneAsGlobalTemplate(cfg);
            EcosystemConfigFileIO.WriteFullConfig(EcosystemConfigPaths.GetTemplateDefaultsPath(api), template);
        }

        public static EcosystemConfig TryReadTemplateDefaults(ICoreAPI api)
        {
            if (api == null) return null;
            EcosystemConfigPaths.MigrateLegacyLayout(api);
            return EcosystemConfigFileIO.ReadFullConfig(EcosystemConfigPaths.GetTemplateDefaultsPath(api));
        }

        static bool TryLoadFromWorldFolder(ICoreAPI api, string worldKey, out EcosystemConfig cfg)
        {
            cfg = null;
            if (!HasWorldFiles(api, worldKey)) return false;

            cfg = new EcosystemConfig();
            // Start from C# defaults, then overlay each category file that exists.
            foreach (string category in EcosystemConfigPaths.WorldCategoryFiles)
            {
                string path = EcosystemConfigPaths.GetWorldCategoryPath(api, worldKey, category);
                Dictionary<string, object> values = EcosystemConfigFileIO.ReadJsonDict(path);
                if (values != null) EcosystemConfigFileIO.ApplyCategory(cfg, values);
            }

            Dictionary<string, object> meta = EcosystemConfigFileIO.ReadJsonDict(
                EcosystemConfigPaths.GetWorldMetaPath(api, worldKey));
            if (meta != null) EcosystemConfigFileIO.ApplyMeta(cfg, meta);

            return true;
        }

        static void WriteWorldFolder(ICoreAPI api, string worldKey, EcosystemConfig cfg)
        {
            EcosystemConfigPaths.EnsureDirectory(EcosystemConfigPaths.GetWorldFolder(api, worldKey));

            foreach (string category in EcosystemConfigPaths.WorldCategoryFiles)
            {
                Dictionary<string, object> values = EcosystemConfigFileIO.ExtractCategory(cfg, category);
                EcosystemConfigFileIO.WriteJsonDict(
                    EcosystemConfigPaths.GetWorldCategoryPath(api, worldKey, category),
                    values);
            }

            EcosystemConfigFileIO.WriteJsonDict(
                EcosystemConfigPaths.GetWorldMetaPath(api, worldKey),
                EcosystemConfigFileIO.ExtractMeta(cfg));

            var info = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["worldKey"] = worldKey,
                ["updatedUtc"] = DateTime.UtcNow.ToString("o"),
            };

            if (api is ICoreServerAPI sapi && sapi.WorldManager?.SaveGame != null)
            {
                try
                {
                    info["worldName"] = sapi.WorldManager.SaveGame.WorldName ?? "";
                    info["seed"] = sapi.WorldManager.SaveGame.Seed;
                }
                catch
                {
                    // optional
                }
            }

            EcosystemConfigFileIO.WriteJsonDict(
                EcosystemConfigPaths.GetWorldInfoPath(api, worldKey),
                info);
        }

        static bool TryLoadLegacySaveBlob(ICoreServerAPI sapi, out EcosystemConfig cfg)
        {
            cfg = null;
            try
            {
                byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
                if (data == null || data.Length == 0) return false;
                string json = Encoding.UTF8.GetString(data);
                cfg = EcosystemConfigCopier.FromJson(json);
                return cfg != null;
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning("[ecosystemflora] Failed reading legacy world config blob: {0}", ex.Message);
                return false;
            }
        }

        static void ClearLegacySaveBlob(ICoreServerAPI sapi)
        {
            try
            {
                sapi.WorldManager.SaveGame.StoreData(SaveKey, Array.Empty<byte>());
            }
            catch
            {
                // best-effort
            }
        }

        static void ApplyKnownPreset(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
            {
                EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
            }
        }

        static string RelativeWorldPath(string worldKey) =>
            Path.Combine(
                EcosystemConfigPaths.RootFolderName,
                EcosystemConfigPaths.WorldsFolderName,
                worldKey ?? "world");
    }
}
