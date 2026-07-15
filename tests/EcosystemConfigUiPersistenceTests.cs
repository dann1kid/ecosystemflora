using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemConfigUiPersistenceTests
    {
        [Fact]
        public void Clone_CopiesAllSchemaFields()
        {
            var source = new EcosystemConfig
            {
                EcosystemEnabled = false,
                ReproduceAttemptsPerYear = 99,
                EnableCanopyAmbience = false,
                MyceliumZoneRadius = 5,
            };

            EcosystemConfig clone = EcosystemConfigCopier.Clone(source);

            Assert.False(clone.EcosystemEnabled);
            Assert.Equal(99, clone.ReproduceAttemptsPerYear);
            Assert.False(clone.EnableCanopyAmbience);
            Assert.Equal(5, clone.MyceliumZoneRadius);
        }

        [Fact]
        public void JsonRoundTrip_PreservesValues()
        {
            var cfg = new EcosystemConfig
            {
                BalancePreset = EcosystemBalancePresets.Lush,
                MinFitness = 0.33f,
                EnableWildVineEcology = false,
            };

            string json = EcosystemConfigCopier.ToJson(cfg);
            EcosystemConfig restored = EcosystemConfigCopier.FromJson(json);

            Assert.Equal(EcosystemBalancePresets.Lush, restored.BalancePreset);
            Assert.Equal(0.33f, restored.MinFitness);
            Assert.False(restored.EnableWildVineEcology);
        }

        [Fact]
        public void Validator_RejectsOutOfRangeChance()
        {
            var cfg = new EcosystemConfig { ReproduceChance = 2f };
            Assert.False(EcosystemConfigValidator.TryValidate(cfg, out string[] errors));
            Assert.Contains(nameof(EcosystemConfig.ReproduceChance) + ":max", errors);
        }

        [Fact]
        public void Normalize_ClampsOutOfRangeChanceToMax()
        {
            var cfg = new EcosystemConfig { ReproduceChance = 2f };
            int changed = EcosystemConfigValidator.NormalizeInPlace(cfg);
            Assert.True(changed > 0);
            Assert.Equal(1f, cfg.ReproduceChance);
            Assert.True(EcosystemConfigValidator.TryValidate(cfg, out string[] errors));
            Assert.Empty(errors);
        }

        [Fact]
        public void Normalize_MapsUnknownPresetToNearestAllowed()
        {
            var cfg = new EcosystemConfig { BalancePreset = "lushh" };
            EcosystemConfigValidator.NormalizeInPlace(cfg);
            Assert.Equal(EcosystemBalancePresets.Lush, cfg.BalancePreset);
            Assert.True(EcosystemConfigValidator.TryValidate(cfg, out _));
        }

        [Fact]
        public void Normalize_ClampsBelowMinRadius()
        {
            var cfg = new EcosystemConfig { ReproduceRadius = -5 };
            EcosystemConfigValidator.NormalizeInPlace(cfg);
            Assert.Equal(0, cfg.ReproduceRadius);
        }

        [Fact]
        public void Validator_AcceptsFreshDefaults()
        {
            EcosystemConfigValidator.TryValidate(new EcosystemConfig(), out string[] errors);
            Assert.True(errors == null || errors.Length == 0, string.Join(", ", errors ?? System.Array.Empty<string>()));
        }
    }
}
