using Vintagestory.API.Common;

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
                EcosystemBalancePresets.Apply(cfg, cfg.BalancePreset);
            }

            EcosystemConfig.Loaded = EcosystemConfigCopier.Clone(cfg);
            api.StoreModConfig(EcosystemConfig.Loaded, EcosystemConfig.ConfigFileName);
            EcosystemSystem.Instance?.RefreshFootTrafficAnimals();
            return true;
        }

        public static void ReloadFromDisk(ICoreAPI api, bool createDefaultIfMissing)
        {
            EcosystemConfig.TryLoadFromDisk(api, createDefaultIfMissing);
        }
    }
}
