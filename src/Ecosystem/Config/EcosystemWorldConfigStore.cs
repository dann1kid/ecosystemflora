using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem.Config
{
    /// <summary>
    /// Per-world server config in <see cref="ISaveGame"/> (not global ModConfig).
    /// Global <c>ModConfig/ecosystemflora.json</c> is only a template for new worlds / migration.
    /// </summary>
    public static class EcosystemWorldConfigStore
    {
        public const string SaveKey = "ecosystemflora:config";

        static ICoreServerAPI boundApi;
        static bool eventsBound;

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

        /// <summary>Load world blob if present; otherwise seed from current <see cref="EcosystemConfig.Loaded"/> (ModConfig template) and persist once.</summary>
        public static void LoadOrMigrate(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;

            byte[] data = null;
            try
            {
                data = sapi.WorldManager.SaveGame.GetData(SaveKey);
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning("[ecosystemflora] Failed reading world config: {0}", ex.Message);
            }

            if (data != null && data.Length > 0)
            {
                try
                {
                    string json = Encoding.UTF8.GetString(data);
                    EcosystemConfig fromWorld = EcosystemConfigCopier.FromJson(json);
                    EcosystemConfigValidator.NormalizeInPlace(fromWorld);
                    EcosystemBalancePresets.TryLoadFilePresets(sapi);
                    ApplyKnownPreset(fromWorld);
                    EcosystemConfig.Loaded = fromWorld;
                    return;
                }
                catch (Exception ex)
                {
                    sapi.Logger.Warning(
                        "[ecosystemflora] Corrupt world config blob; reseeding from ModConfig template: {0}",
                        ex.Message);
                }
            }

            // First load for this world: seed from in-memory template (already loaded from ModConfig).
            // Always require setup wizard on a brand-new world — never inherit SetupWizardCompleted
            // from a previous world's leaked ModConfig template.
            EcosystemBalancePresets.TryLoadFilePresets(sapi);
            ApplyKnownPreset(EcosystemConfig.Loaded);
            PrepareFreshWorldConfig(EcosystemConfig.Loaded);
            Persist(sapi);
            sapi.Logger.Notification(
                "[ecosystemflora] Seeded per-world config from ModConfig template ({0}); setup wizard pending.",
                SaveKey);
        }

        /// <summary>
        /// Clears first-run / auto-tune world-local flags so a newly created world always shows the setup wizard
        /// (SP owner and server admins with <c>controlserver</c>).
        /// </summary>
        public static void PrepareFreshWorldConfig(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            cfg.SetupWizardCompleted = false;
            cfg.LastAutoTuneTier = "";
            cfg.LastAutoTuneOpsPerMs = 0;
            cfg.LastAutoTuneElapsedMs = 0;
            cfg.LastAutoTuneUtc = "";
        }

        /// <summary>
        /// Clone suitable for writing <c>ModConfig/ecosystemflora.json</c> — never carries
        /// per-world first-run completion into the global template.
        /// </summary>
        public static EcosystemConfig CloneAsGlobalTemplate(EcosystemConfig source)
        {
            EcosystemConfig clone = EcosystemConfigCopier.Clone(source ?? new EcosystemConfig());
            PrepareFreshWorldConfig(clone);
            return clone;
        }

        /// <summary>Best-effort load when SaveGame is already available (before SaveGameLoaded).</summary>
        public static void TryLoadOrMigrateNow(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            LoadOrMigrate(sapi);
        }

        public static void Persist(ICoreServerAPI sapi)
        {
            if (sapi?.WorldManager?.SaveGame == null) return;
            if (EcosystemConfig.Loaded == null) return;

            try
            {
                string json = EcosystemConfigCopier.ToJson(EcosystemConfig.Loaded);
                sapi.WorldManager.SaveGame.StoreData(SaveKey, Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                sapi.Logger.Warning("[ecosystemflora] Failed writing world config: {0}", ex.Message);
            }
        }

        /// <summary>Whether a world blob exists (for tests / diagnostics).</summary>
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

        static void ApplyKnownPreset(EcosystemConfig cfg)
        {
            if (cfg == null) return;
            if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
            {
                EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
            }
        }
    }
}
