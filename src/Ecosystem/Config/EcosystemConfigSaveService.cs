using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem.Config
{
    public static class EcosystemConfigSaveService
    {
        public static bool TryApplyAndPersist(ICoreAPI api, EcosystemConfig cfg, out string errorCode)
        {
            errorCode = null;
            if (api == null || cfg == null)
            {
                errorCode = "config-null";
                return false;
            }

            EcosystemConfigValidator.NormalizeInPlace(cfg);

            if (!EcosystemConfigValidator.TryValidate(cfg, out _))
            {
                errorCode = "config-invalid";
                return false;
            }

            if (EcosystemBalancePresets.IsKnownPreset(cfg.BalancePreset))
            {
                EcosystemConfigSchema.ReapplyKnownPresetPreservingOverrides(cfg);
            }

            EcosystemConfig.Loaded = EcosystemConfigCopier.Clone(cfg);

            // Server: persist to this world's SaveGame only — never rewrite global ModConfig template.
            if (api is ICoreServerAPI sapi)
            {
                EcosystemWorldConfigStore.Persist(sapi);
                // Ensure the in-memory flag cannot be lost before the next autosave.
                if (cfg.SetupWizardCompleted)
                {
                    EcosystemConfig.Loaded.SetupWizardCompleted = true;
                }
            }

            EcosystemSystem.Instance?.RefreshFootTrafficAnimals();
            EcosystemSystem.Instance?.TryRefreshTickIntervals();
            if (!EcosystemConfig.Loaded.EnableTrampling)
            {
                EcosystemSystem.Instance?.ColumnTraffic?.Clear();
            }

            // Re-sync knife/scythe drops after per-world toggle (AssetsFinalize may have used template).
            FlowerDrygrassDrops.Apply(api);

            return true;
        }

        public static void ReloadFromDisk(ICoreAPI api, bool createDefaultIfMissing)
        {
            if (api is ICoreServerAPI sapi && sapi.WorldManager?.SaveGame != null)
            {
                EcosystemWorldConfigStore.LoadOrMigrate(sapi);
                EcosystemSystem.Instance?.TryRefreshTickIntervals();
                return;
            }

            EcosystemConfig.TryLoadFromDisk(api, createDefaultIfMissing);
        }
    }
}
