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
        static bool worldConfigReady;

        public static string ActiveWorldKey => activeWorldKey;

        /// <summary>True after <see cref="LoadOrMigrate"/> has applied this world's files (not just the global template).</summary>
        public static bool IsWorldConfigReady => worldConfigReady;

        /// <summary>Fired on the server after per-world settings are loaded into <see cref="EcosystemConfig.Loaded"/>.</summary>
        public static event Action OnWorldConfigReady;

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
            worldConfigReady = false;
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

            if (TryLoadFromWorldFolder(sapi, activeWorldKey, out EcosystemConfig fromFiles, out bool wizardFlagPresent))
            {
                EcosystemConfigValidator.NormalizeInPlace(fromFiles);
                EcosystemBalancePresets.TryLoadFilePresets(sapi);
                ApplyKnownPreset(fromFiles);
                bool upgraded = EnsureWizardPendingUnlessRecorded(fromFiles, wizardFlagPresent);
                if (fromFiles.SetupWizardCompleted)
                {
                    WriteWizardDoneMarker(sapi, activeWorldKey, completed: true);
                }

                EcosystemConfig.Loaded = fromFiles;
                if (upgraded)
                {
                    Persist(sapi);
                    sapi.Logger.Notification(
                        "[ecosystemflora] Loaded world settings from {0}; setup wizard pending (upgrade).",
                        RelativeWorldPath(activeWorldKey));
                }
                else
                {
                    sapi.Logger.Notification(
                        "[ecosystemflora] Loaded world settings from {0} (wizardCompleted={1})",
                        RelativeWorldPath(activeWorldKey),
                        fromFiles.SetupWizardCompleted);
                }

                MarkWorldConfigReady();
                return;
            }

            // Legacy SaveGame blob → export to files once.
            if (TryLoadLegacySaveBlob(sapi, out EcosystemConfig fromBlob, out bool blobWizardFlagPresent))
            {
                EcosystemConfigValidator.NormalizeInPlace(fromBlob);
                EcosystemBalancePresets.TryLoadFilePresets(sapi);
                ApplyKnownPreset(fromBlob);
                EnsureWizardPendingUnlessRecorded(fromBlob, blobWizardFlagPresent);
                EcosystemConfig.Loaded = fromBlob;
                Persist(sapi);
                ClearLegacySaveBlob(sapi);
                sapi.Logger.Notification(
                    "[ecosystemflora] Migrated SaveGame config blob → {0}",
                    RelativeWorldPath(activeWorldKey));
                MarkWorldConfigReady();
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
            MarkWorldConfigReady();
        }

        static void MarkWorldConfigReady()
        {
            bool firstReady = !worldConfigReady;
            worldConfigReady = true;
            if (!firstReady) return;

            try
            {
                OnWorldConfigReady?.Invoke();
            }
            catch (Exception ex)
            {
                boundApi?.Logger.Warning("[ecosystemflora] OnWorldConfigReady handler failed: {0}", ex.Message);
            }
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

        /// <summary>
        /// Worlds that never recorded <see cref="EcosystemConfig.SetupWizardCompleted"/>
        /// (pre-wizard saves / missing meta key) stay pending so the client can prompt.
        /// Never downgrades an already-completed flag.
        /// </summary>
        public static bool EnsureWizardPendingUnlessRecorded(EcosystemConfig cfg, bool completionFlagPresent)
        {
            if (cfg == null) return false;
            // Already completed (meta or in-memory) — do not re-prompt.
            if (cfg.SetupWizardCompleted) return false;
            // Explicit false already stored in meta.
            if (completionFlagPresent) return false;
            // Missing key: keep pending and persist an explicit false once.
            return true;
        }

        public static bool MetaContainsWizardCompletionFlag(Dictionary<string, object> meta)
        {
            if (meta == null) return false;
            foreach (string key in meta.Keys)
            {
                if (string.Equals(key, nameof(EcosystemConfig.SetupWizardCompleted), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool JsonMentionsWizardCompletionFlag(string json) =>
            !string.IsNullOrEmpty(json)
            && json.IndexOf(nameof(EcosystemConfig.SetupWizardCompleted), StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Disk is the source of truth for wizard completion (marker file and/or meta.json text).
        /// Used to defeat SSP sync races that briefly expose template defaults.
        /// </summary>
        public static bool IsWizardCompletedOnDisk(ICoreAPI api, string worldKey = null)
        {
            if (api == null) return false;
            string key = worldKey;
            if (string.IsNullOrEmpty(key))
            {
                key = activeWorldKey;
            }

            if (string.IsNullOrEmpty(key) && api is ICoreServerAPI sapi)
            {
                try { key = EcosystemConfigPaths.ResolveWorldKey(sapi); }
                catch { /* ignore */ }
            }

            if (string.IsNullOrEmpty(key)) return false;

            string donePath = EcosystemConfigPaths.GetWizardDonePath(api, key);
            if (File.Exists(donePath)) return true;

            return TryReadWizardCompletedFromMetaText(
                EcosystemConfigPaths.GetWorldMetaPath(api, key));
        }

        public static void WriteWizardDoneMarker(ICoreAPI api, string worldKey, bool completed)
        {
            if (api == null || string.IsNullOrEmpty(worldKey)) return;
            string path = EcosystemConfigPaths.GetWizardDonePath(api, worldKey);
            try
            {
                if (completed)
                {
                    EcosystemConfigPaths.EnsureDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, DateTime.UtcNow.ToString("o"), Encoding.UTF8);
                }
                else if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best-effort
            }
        }

        /// <summary>Parse meta.json without JsonUtil Dictionary boxing quirks.</summary>
        public static bool TryReadWizardCompletedFromMetaText(string metaPath)
        {
            if (string.IsNullOrEmpty(metaPath) || !File.Exists(metaPath)) return false;
            try
            {
                string text = File.ReadAllText(metaPath, Encoding.UTF8);
                if (string.IsNullOrEmpty(text)) return false;

                // "SetupWizardCompleted": true / True / 1
                int idx = text.IndexOf("SetupWizardCompleted", StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return false;
                int colon = text.IndexOf(':', idx);
                if (colon < 0 || colon + 1 >= text.Length) return false;

                string tail = text.Substring(colon + 1).TrimStart();
                if (tail.StartsWith("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (tail.StartsWith("1") && (tail.Length == 1 || !char.IsDigit(tail[1]))) return true;
                return false;
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

        static bool TryLoadFromWorldFolder(
            ICoreAPI api,
            string worldKey,
            out EcosystemConfig cfg,
            out bool wizardFlagPresent)
        {
            cfg = null;
            wizardFlagPresent = false;
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
            if (meta != null)
            {
                wizardFlagPresent = MetaContainsWizardCompletionFlag(meta);
                EcosystemConfigFileIO.ApplyMeta(cfg, meta);
            }

            // Disk marker / raw meta text beat flaky Dictionary bool boxing.
            if (IsWizardCompletedOnDisk(api, worldKey))
            {
                cfg.SetupWizardCompleted = true;
                wizardFlagPresent = true;
            }

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

            WriteWizardDoneMarker(api, worldKey, cfg != null && cfg.SetupWizardCompleted);

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

        static bool TryLoadLegacySaveBlob(
            ICoreServerAPI sapi,
            out EcosystemConfig cfg,
            out bool wizardFlagPresent)
        {
            cfg = null;
            wizardFlagPresent = false;
            try
            {
                byte[] data = sapi.WorldManager.SaveGame.GetData(SaveKey);
                if (data == null || data.Length == 0) return false;
                string json = Encoding.UTF8.GetString(data);
                wizardFlagPresent = JsonMentionsWizardCompletionFlag(json);
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
            EcosystemConfigSchema.ReapplyKnownPresetPreservingOverrides(cfg);
        }

        static string RelativeWorldPath(string worldKey) =>
            Path.Combine(
                EcosystemConfigPaths.RootFolderName,
                EcosystemConfigPaths.WorldsFolderName,
                worldKey ?? "world");
    }
}
