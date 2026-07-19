using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemWorldConfigStoreTests
    {
        [Fact]
        public void ConfigCopier_RoundTripsSetupAndVicinityFields()
        {
            var src = new EcosystemConfig
            {
                SetupWizardCompleted = true,
                EnablePlayerVicinityRescan = false,
                PlayerVicinityRescanIntervalMs = 9000,
                ReproduceTickIntervalMs = 4000,
            };

            EcosystemConfig dst = EcosystemConfigCopier.Clone(src);

            Assert.True(dst.SetupWizardCompleted);
            Assert.False(dst.EnablePlayerVicinityRescan);
            Assert.Equal(9000, dst.PlayerVicinityRescanIntervalMs);
            Assert.Equal(4000, dst.ReproduceTickIntervalMs);
        }

        [Fact]
        public void ConfigCopier_CopiesHiddenSetupWizardFlag()
        {
            var src = new EcosystemConfig { SetupWizardCompleted = true };
            var dst = new EcosystemConfig { SetupWizardCompleted = false };
            EcosystemConfigCopier.CopyScope(src, dst, ConfigFieldScope.Server);
            Assert.True(dst.SetupWizardCompleted);
        }

        [Fact]
        public void CopyFields_PreservesSetupWizardCompletedTrue()
        {
            var src = new EcosystemConfig { SetupWizardCompleted = true };
            var dst = new EcosystemConfig { SetupWizardCompleted = false };
            EcosystemConfigCopier.CopyFields(src, dst);
            Assert.True(dst.SetupWizardCompleted);
        }

        [Fact]
        public void CopyFields_DoesNotImplicitlyClearWizardFlagWhenSourceFalse_IsExplicit()
        {
            // Documents current CopyFields behavior: source wins. Callers must guard against
            // stale sync snapshots regressing a completed wizard on SSP shared Loaded.
            var src = new EcosystemConfig { SetupWizardCompleted = false };
            var dst = new EcosystemConfig { SetupWizardCompleted = true };
            EcosystemConfigCopier.CopyFields(src, dst);
            Assert.False(dst.SetupWizardCompleted);
        }

        [Fact]
        public void ConfigCopier_CopiesLastAutoTuneMetadata()
        {
            var src = new EcosystemConfig
            {
                LastAutoTuneTier = "Strong",
                LastAutoTuneOpsPerMs = 300.5,
                LastAutoTuneElapsedMs = 42,
                LastAutoTuneUtc = "2026-07-19T00:00:00Z",
            };
            EcosystemConfig dst = EcosystemConfigCopier.Clone(src);
            Assert.Equal("Strong", dst.LastAutoTuneTier);
            Assert.Equal(300.5, dst.LastAutoTuneOpsPerMs);
            Assert.Equal(42, dst.LastAutoTuneElapsedMs);
            Assert.Equal("2026-07-19T00:00:00Z", dst.LastAutoTuneUtc);
        }

        [Fact]
        public void EnsureWizardPendingUnlessRecorded_MissingFlag_AlreadyPending_StillNeedsPersist()
        {
            var cfg = new EcosystemConfig { SetupWizardCompleted = false };
            Assert.True(EcosystemWorldConfigStore.EnsureWizardPendingUnlessRecorded(cfg, completionFlagPresent: false));
            Assert.False(cfg.SetupWizardCompleted);
        }

        [Fact]
        public void EnsureWizardPendingUnlessRecorded_CompletedWithoutMetaKey_NotDowngraded()
        {
            var cfg = new EcosystemConfig { SetupWizardCompleted = true };
            Assert.False(EcosystemWorldConfigStore.EnsureWizardPendingUnlessRecorded(cfg, completionFlagPresent: false));
            Assert.True(cfg.SetupWizardCompleted);
        }

        [Fact]
        public void EnsureWizardPendingUnlessRecorded_RecordedTrue_LeftAlone()
        {
            var cfg = new EcosystemConfig { SetupWizardCompleted = true };
            Assert.False(EcosystemWorldConfigStore.EnsureWizardPendingUnlessRecorded(cfg, completionFlagPresent: true));
            Assert.True(cfg.SetupWizardCompleted);
        }

        [Fact]
        public void EnsureWizardPendingUnlessRecorded_RecordedFalse_NoPersistNeeded()
        {
            var cfg = new EcosystemConfig { SetupWizardCompleted = false };
            Assert.False(EcosystemWorldConfigStore.EnsureWizardPendingUnlessRecorded(cfg, completionFlagPresent: true));
            Assert.False(cfg.SetupWizardCompleted);
        }

        [Fact]
        public void JsonMentionsWizardCompletionFlag_DetectsPropertyName()
        {
            Assert.True(EcosystemWorldConfigStore.JsonMentionsWizardCompletionFlag(
                "{\"SetupWizardCompleted\":false}"));
            Assert.False(EcosystemWorldConfigStore.JsonMentionsWizardCompletionFlag("{\"TickBudgetMs\":2}"));
        }

        [Fact]
        public void MetaContainsWizardCompletionFlag_RequiresKey()
        {
            var with = new System.Collections.Generic.Dictionary<string, object>
            {
                [nameof(EcosystemConfig.SetupWizardCompleted)] = false,
            };
            var without = new System.Collections.Generic.Dictionary<string, object>
            {
                ["LastAutoTuneTier"] = "Weak",
            };
            Assert.True(EcosystemWorldConfigStore.MetaContainsWizardCompletionFlag(with));
            Assert.False(EcosystemWorldConfigStore.MetaContainsWizardCompletionFlag(without));
            Assert.False(EcosystemWorldConfigStore.MetaContainsWizardCompletionFlag(null));
        }

        [Fact]
        public void ApplyMeta_ReadsWizardFlagCaseInsensitive()
        {
            var cfg = new EcosystemConfig { SetupWizardCompleted = false };
            var meta = new System.Collections.Generic.Dictionary<string, object>
            {
                ["setupWizardCompleted"] = true,
            };

            EcosystemConfigFileIO.ApplyMeta(cfg, meta);
            Assert.True(cfg.SetupWizardCompleted);
            Assert.True(EcosystemWorldConfigStore.MetaContainsWizardCompletionFlag(meta));
        }

        [Fact]
        public void TryReadWizardCompletedFromMetaText_DetectsTrue()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "eco-meta-test.json");
            System.IO.File.WriteAllText(path, "{\"SetupWizardCompleted\":true,\"LastAutoTuneTier\":\"\"}");
            try
            {
                Assert.True(EcosystemWorldConfigStore.TryReadWizardCompletedFromMetaText(path));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void TryReadWizardCompletedFromMetaText_DetectsFalse()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "eco-meta-test-false.json");
            System.IO.File.WriteAllText(path, "{\"SetupWizardCompleted\":false}");
            try
            {
                Assert.False(EcosystemWorldConfigStore.TryReadWizardCompletedFromMetaText(path));
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void PrepareFreshWorldConfig_ForcesWizardPending()
        {
            var cfg = new EcosystemConfig
            {
                SetupWizardCompleted = true,
                LastAutoTuneTier = "Strong",
                LastAutoTuneOpsPerMs = 400,
                LastAutoTuneElapsedMs = 10,
                LastAutoTuneUtc = "2026-01-01T00:00:00Z",
            };

            EcosystemWorldConfigStore.PrepareFreshWorldConfig(cfg);

            Assert.False(cfg.SetupWizardCompleted);
            Assert.Equal("", cfg.LastAutoTuneTier);
            Assert.Equal(0, cfg.LastAutoTuneOpsPerMs);
            Assert.Equal(0, cfg.LastAutoTuneElapsedMs);
            Assert.Equal("", cfg.LastAutoTuneUtc);
        }

        [Fact]
        public void CloneAsGlobalTemplate_DoesNotLeakWizardCompletion()
        {
            var src = new EcosystemConfig { SetupWizardCompleted = true, LastAutoTuneTier = "Weak" };
            EcosystemConfig template = EcosystemWorldConfigStore.CloneAsGlobalTemplate(src);

            Assert.True(src.SetupWizardCompleted);
            Assert.False(template.SetupWizardCompleted);
            Assert.Equal("", template.LastAutoTuneTier);
        }

        [Fact]
        public void WorldConfigSaveKey_IsStableLegacyConstant()
        {
            Assert.Equal("ecosystemflora:config", EcosystemWorldConfigStore.SaveKey);
        }

        [Fact]
        public void SanitizeFolderName_StripsInvalidChars()
        {
            Assert.Equal("My_World", EcosystemConfigPaths.SanitizeFolderName("My World"));
            Assert.Equal("a_b", EcosystemConfigPaths.SanitizeFolderName("a/b"));
            Assert.Equal("world", EcosystemConfigPaths.SanitizeFolderName("???"));
        }

        [Fact]
        public void CategoryExtractApply_RoundTripsPerfFields()
        {
            var src = new EcosystemConfig
            {
                ReproduceTickIntervalMs = 9999,
                MaxFloraRescanColumnsPerTick = 2,
                PlayerVicinityRescanIntervalMs = 12345,
            };

            var dict = EcosystemConfigFileIO.ExtractCategory(src, "perf");
            var dst = new EcosystemConfig();
            EcosystemConfigFileIO.ApplyCategory(dst, dict);

            Assert.Equal(9999, dst.ReproduceTickIntervalMs);
            Assert.Equal(2, dst.MaxFloraRescanColumnsPerTick);
            Assert.Equal(12345, dst.PlayerVicinityRescanIntervalMs);
        }

        [Fact]
        public void MetaExtractApply_RoundTripsWizardFlags()
        {
            var src = new EcosystemConfig
            {
                SetupWizardCompleted = true,
                LastAutoTuneTier = "Balanced",
                LastAutoTuneOpsPerMs = 12.5,
                LastAutoTuneElapsedMs = 7,
                LastAutoTuneUtc = "2026-07-19T00:00:00Z",
            };

            var dict = EcosystemConfigFileIO.ExtractMeta(src);
            var dst = new EcosystemConfig();
            EcosystemConfigFileIO.ApplyMeta(dst, dict);

            Assert.True(dst.SetupWizardCompleted);
            Assert.Equal("Balanced", dst.LastAutoTuneTier);
            Assert.Equal(12.5, dst.LastAutoTuneOpsPerMs);
            Assert.Equal(7, dst.LastAutoTuneElapsedMs);
            Assert.Equal("2026-07-19T00:00:00Z", dst.LastAutoTuneUtc);
        }
    }

    public class EcosystemPerfCalibratorTests
    {
        [Theory]
        [InlineData(50.0, EcosystemPerfCalibrator.PerfTier.Weak)]
        [InlineData(100.0, EcosystemPerfCalibrator.PerfTier.Balanced)]
        [InlineData(300.0, EcosystemPerfCalibrator.PerfTier.Strong)]
        public void Classify_MapsOpsPerMsToTier(double opsPerMs, EcosystemPerfCalibrator.PerfTier expected)
        {
            Assert.Equal(expected, EcosystemPerfCalibrator.Classify(opsPerMs));
        }

        [Fact]
        public void ApplyTier_Weak_SetsConservativeScanCadence()
        {
            var cfg = new EcosystemConfig();
            EcosystemPerfCalibrator.ApplyTiers(cfg, EcosystemPerfCalibrator.PerfTier.Weak);

            Assert.True(cfg.ReproduceTickIntervalMs >= 5000);
            Assert.True(cfg.PlayerVicinityRescanIntervalMs >= 8000);
            Assert.True(cfg.MaxFloraRescanColumnsPerTick <= 4);
            Assert.True(cfg.EnablePlayerVicinityRescan);
        }

        [Fact]
        public void Run_CompletesWithPositiveOps()
        {
            EcosystemPerfCalibrator.CalibrationResult result = EcosystemPerfCalibrator.Run(
                poolSize: 2048,
                operationCount: 20000);

            Assert.True(result.ElapsedMs >= 1);
            Assert.True(result.OpsPerMs > 0);
            Assert.Equal(20000, result.Operations);
        }

        [Fact]
        public void SetupProfiles_Weak_UsesCustomPresetAndNearPlayersDefault()
        {
            var cfg = new EcosystemConfig();
            EcosystemSetupProfiles.ApplyProfile(cfg, EcosystemSetupProfiles.Weak, onlyNearPlayers: true);

            Assert.Equal(EcosystemBalancePresets.Custom, cfg.BalancePreset);
            Assert.True(cfg.OnlyActivateNearPlayers);
            Assert.True(EcosystemSetupProfiles.DefaultAutoTune(EcosystemSetupProfiles.Weak));
            Assert.True(EcosystemSetupProfiles.DefaultNearPlayers(EcosystemSetupProfiles.Weak));
        }

        [Fact]
        public void SetupProfiles_VanillaSafe_DisablesPhenology()
        {
            var cfg = new EcosystemConfig();
            EcosystemSetupProfiles.ApplyProfile(cfg, EcosystemSetupProfiles.VanillaSafe, onlyNearPlayers: false);

            Assert.Equal(EcosystemBalancePresets.VanillaMinimal, cfg.BalancePreset);
            Assert.False(cfg.EnableFlowerPhenology);
            Assert.False(cfg.OnlyActivateNearPlayers);
        }

        [Fact]
        public void RunAndApply_RecordsLastAutoTuneMetadata()
        {
            var cfg = new EcosystemConfig();
            EcosystemPerfCalibrator.CalibrationResult result = EcosystemPerfCalibrator.RunAndApply(cfg);

            Assert.Equal(result.Tier.ToString(), cfg.LastAutoTuneTier);
            Assert.Equal(result.OpsPerMs, cfg.LastAutoTuneOpsPerMs);
            Assert.True(cfg.LastAutoTuneElapsedMs > 0);
            Assert.False(string.IsNullOrWhiteSpace(cfg.LastAutoTuneUtc));
        }

        [Fact]
        public void ApplySuperMinimal_IsSlowerThanWeak()
        {
            var weak = new EcosystemConfig();
            var min = new EcosystemConfig();
            EcosystemPerfCalibrator.ApplyTiers(weak, EcosystemPerfCalibrator.PerfTier.Weak);
            EcosystemPerfCalibrator.ApplySuperMinimal(min);

            Assert.True(min.ReproduceTickIntervalMs > weak.ReproduceTickIntervalMs);
            Assert.True(min.MaxFloraRescanColumnsPerTick < weak.MaxFloraRescanColumnsPerTick);
            Assert.True(min.OnlyActivateNearPlayers);
            Assert.True(min.MaxReproduceAttemptsPerTick <= 3);
        }

        [Fact]
        public void WizardEditableFields_CoverHotPerfKnobs()
        {
            Assert.Contains(nameof(EcosystemConfig.PriorityRegistrationBudgetMs), EcosystemPerfCalibrator.WizardEditableFields);
            Assert.Contains(nameof(EcosystemConfig.MaxRegistrationsPerTick), EcosystemPerfCalibrator.WizardEditableFields);
            Assert.Contains(nameof(EcosystemConfig.EnableBackgroundRegistrationScan), EcosystemPerfCalibrator.WizardEditableFields);
            Assert.Contains(nameof(EcosystemConfig.OnlyActivateNearPlayers), EcosystemPerfCalibrator.WizardEditableFields);
            Assert.True(EcosystemPerfCalibrator.WizardEditableFields.Length >= 40);
        }

        [Fact]
        public void WizardEditableFields_HavePerfHints()
        {
            foreach (string name in EcosystemPerfCalibrator.WizardEditableFields)
            {
                // Every wizard field should resolve (Neutral is allowed for mixed toggles).
                PerfKnobHint hint = EcosystemPerfKnobHints.Get(name);
                Assert.True(
                    hint == PerfKnobHint.HigherHeavier
                    || hint == PerfKnobHint.HigherLighter
                    || hint == PerfKnobHint.OnHeavier
                    || hint == PerfKnobHint.OnLighter
                    || hint == PerfKnobHint.Neutral,
                    name);
            }

            Assert.Equal(PerfKnobHint.HigherLighter, EcosystemPerfKnobHints.Get(nameof(EcosystemConfig.ReproduceTickIntervalMs)));
            Assert.Equal(PerfKnobHint.HigherHeavier, EcosystemPerfKnobHints.Get(nameof(EcosystemConfig.TickBudgetMs)));
            Assert.Equal(PerfKnobHint.OnLighter, EcosystemPerfKnobHints.Get(nameof(EcosystemConfig.OnlyActivateNearPlayers)));
            Assert.Equal("Tick budget  val→ CPU↑", EcosystemPerfKnobHints.FormatTitleWithHint("Tick budget", nameof(EcosystemConfig.TickBudgetMs)));
            Assert.Equal("Interval  val→ CPU↓", EcosystemPerfKnobHints.FormatTitleWithHint("Interval", nameof(EcosystemConfig.ReproduceTickIntervalMs)));
            Assert.Equal(EcosystemPerfKnobHints.MarkerOnLighter, EcosystemPerfKnobHints.MarkerForField(nameof(EcosystemConfig.OnlyActivateNearPlayers)));
        }

        [Fact]
        public void ApplyTiers_Weak_SetsRegistrationAndPriorityCaps()
        {
            var cfg = new EcosystemConfig();
            EcosystemPerfCalibrator.ApplyTiers(cfg, EcosystemPerfCalibrator.PerfTier.Weak);
            Assert.True(cfg.MaxRegistrationsPerTick > 0);
            Assert.True(cfg.PriorityRegistrationBudgetMs > 0);
            Assert.True(cfg.MaxPriorityChunkScansPerTick > 0);
            Assert.False(cfg.OnlyActivateNearPlayers);
        }

        [Fact]
        public void WizardEditableFields_AreAllKnownSchemaFields()
        {
            foreach (string name in EcosystemPerfCalibrator.WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(name);
                Assert.NotNull(field);
                Assert.True(
                    field.Kind == ConfigFieldKind.Integer || field.Kind == ConfigFieldKind.Boolean,
                    name);
            }
        }
    }

    public class EcosystemVicinityConfigTests
    {
        [Fact]
        public void VicinityRescan_DefaultsEnabledAt5s()
        {
            var cfg = new EcosystemConfig();
            Assert.True(cfg.EnablePlayerVicinityRescan);
            Assert.Equal(5000, cfg.PlayerVicinityRescanIntervalMs);
            Assert.False(cfg.SetupWizardCompleted);
        }
    }
}
