namespace WildFarming.Ecosystem.Config
{
    /// <summary>First-run wizard play profiles (balance + optional near-player scope).</summary>
    public static class EcosystemSetupProfiles
    {
        public const string Weak = "weak";
        public const string Balanced = "balanced";
        public const string Lush = "lush";
        public const string VanillaSafe = "vanilla-safe";

        public static readonly string[] Codes =
        {
            Weak,
            Balanced,
            Lush,
            VanillaSafe,
        };

        /// <summary>Default "near players only" checkbox for a profile.</summary>
        public static bool DefaultNearPlayers(string profileCode)
        {
            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            return code == Weak;
        }

        /// <summary>Default auto-tune checkbox for a profile.</summary>
        public static bool DefaultAutoTune(string profileCode)
        {
            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            return code == Weak;
        }

        /// <summary>Apply balance preset + Weak perf overlay. Does not set SetupWizardCompleted or run auto-tune.</summary>
        public static void ApplyProfile(EcosystemConfig cfg, string profileCode, bool onlyNearPlayers)
        {
            if (cfg == null) return;

            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            switch (code)
            {
                case Weak:
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);
                    cfg.BalancePreset = EcosystemBalancePresets.Custom;
                    EcosystemPerfCalibrator.ApplyTiers(cfg, EcosystemPerfCalibrator.PerfTier.Weak);
                    break;

                case Lush:
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Lush);
                    cfg.BalancePreset = EcosystemBalancePresets.Lush;
                    break;

                case VanillaSafe:
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.VanillaMinimal);
                    cfg.BalancePreset = EcosystemBalancePresets.VanillaMinimal;
                    break;

                default:
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);
                    cfg.BalancePreset = EcosystemBalancePresets.Natural;
                    break;
            }

            cfg.OnlyActivateNearPlayers = onlyNearPlayers;
        }
    }
}
