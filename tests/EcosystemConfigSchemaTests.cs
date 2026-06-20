using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemConfigSchemaTests
    {
        [Fact]
        public void Schema_DiscoversWritableProperties()
        {
            Assert.True(EcosystemConfigSchema.Fields.Count > 80);
        }

        [Fact]
        public void Schema_HidesLegacyAliasProperties()
        {
            Assert.Null(EcosystemConfigSchema.GetField(nameof(EcosystemConfig.MaxCanopyUpdateOpsPerTick)));
            Assert.Null(EcosystemConfigSchema.GetField(nameof(EcosystemConfig.CanopyBudgetMs)));
            Assert.Null(EcosystemConfigSchema.GetField(nameof(EcosystemConfig.GrowthHoursMultiplier)));
        }

        [Fact]
        public void Schema_HasExpectedCategories()
        {
            Assert.NotEmpty(EcosystemConfigSchema.GetCategoryFields("master"));
            Assert.NotEmpty(EcosystemConfigSchema.GetCategoryFields("spread"));
            Assert.NotEmpty(EcosystemConfigSchema.GetCategoryFields("perf"));
        }

        [Fact]
        public void ApplyPresetSelection_SetsSpreadFields()
        {
            var cfg = new EcosystemConfig();
            EcosystemConfigSchema.ApplyPresetSelection(cfg, EcosystemBalancePresets.Sparse);

            Assert.Equal(EcosystemBalancePresets.Sparse, cfg.BalancePreset);
            Assert.Equal(36, cfg.ReproduceAttemptsPerYear);
            Assert.Equal(2, cfg.DefaultSameSpeciesSpacing);
        }

        [Fact]
        public void MarkCustomIfPresetFieldEdited_SwitchesPreset()
        {
            var cfg = new EcosystemConfig { BalancePreset = EcosystemBalancePresets.Natural };
            EcosystemConfigSchema.MarkCustomIfPresetFieldEdited(cfg, nameof(EcosystemConfig.ReproduceChance));

            Assert.Equal(EcosystemBalancePresets.Custom, cfg.BalancePreset);
        }
    }
}
