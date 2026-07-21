namespace WildFarming.Ecosystem.Config
{
    /// <summary>First-run wizard play profiles (balance + optional near-player scope).</summary>
    public static class EcosystemSetupProfiles
    {
        public const string Potato = "potato";
        public const string Weak = "weak";
        public const string Balanced = "balanced";
        public const string X3d = "x3d";
        public const string Lush = "lush";
        public const string VanillaSafe = "vanilla-safe";

        public static readonly string[] Codes =
        {
            Potato,
            Weak,
            Balanced,
            X3d,
            Lush,
            VanillaSafe,
        };

        /// <summary>Default "near players only" checkbox for a profile.</summary>
        public static bool DefaultNearPlayers(string profileCode)
        {
            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            return code == Potato || code == Weak;
        }

        /// <summary>Default auto-tune checkbox for a profile.</summary>
        public static bool DefaultAutoTune(string profileCode)
        {
            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            // Potato already ships Super-minimal; Weak may still auto-tune into Super-minimal.
            return code == Weak;
        }

        /// <summary>Apply balance preset + optional Weak/Super-minimal / X3D perf overlay. Does not set SetupWizardCompleted or run auto-tune.</summary>
        public static void ApplyProfile(EcosystemConfig cfg, string profileCode, bool onlyNearPlayers)
        {
            if (cfg == null) return;

            string code = (profileCode ?? Balanced).Trim().ToLowerInvariant();
            switch (code)
            {
                case Potato:
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);
                    cfg.BalancePreset = EcosystemBalancePresets.Custom;
                    EcosystemPerfCalibrator.ApplySuperMinimal(cfg);
                    break;

                case Weak:
                    // Wizard "weak PC" uses Super-minimal so potato machines are quiet out of the box.
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);
                    cfg.BalancePreset = EcosystemBalancePresets.Custom;
                    EcosystemPerfCalibrator.ApplySuperMinimal(cfg);
                    break;

                case X3d:
                    // Strong-ish ecology cadence, then dual-CCD worker clamp.
                    EcosystemBalancePresets.Apply(cfg, EcosystemBalancePresets.Natural);
                    EcosystemPerfCalibrator.ApplyTiers(cfg, EcosystemPerfCalibrator.PerfTier.Strong);
                    EcosystemPerfCalibrator.ApplyX3dOptimize(cfg);
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
            if (code == X3d)
            {
                // Profile intent: LimitSpreadNearPlayers, not full near-player registration gate.
                cfg.OnlyActivateNearPlayers = false;
                cfg.LimitSpreadNearPlayers = true;
            }
        }
    }
}
