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
        public void Validator_AcceptsFreshDefaults()
        {
            EcosystemConfigValidator.TryValidate(new EcosystemConfig(), out string[] errors);
            Assert.True(errors == null || errors.Length == 0, string.Join(", ", errors ?? System.Array.Empty<string>()));
        }
    }
}
