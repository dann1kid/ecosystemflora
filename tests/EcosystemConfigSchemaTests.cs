using System.Collections.Generic;
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
        }

        [Fact]
        public void Schema_ExposesGrowthHoursMultiplier()
        {
            Assert.NotNull(EcosystemConfigSchema.GetField(nameof(EcosystemConfig.GrowthHoursMultiplier)));
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
        public void ReapplyKnownPresetPreservingOverrides_KeepsHandEditedPerf()
        {
            var cfg = new EcosystemConfig
            {
                BalancePreset = EcosystemBalancePresets.Natural,
                ReproduceAttemptsPerYear = 10,
                TickBudgetMs = 99,
                ReproduceTickIntervalMs = 7777,
                MaxFloraRescanColumnsPerTick = 3,
            };

            EcosystemConfigSchema.ReapplyKnownPresetPreservingOverrides(cfg);

            Assert.Equal(72, cfg.ReproduceAttemptsPerYear); // preset field
            Assert.Equal(99, cfg.TickBudgetMs); // non-preset override
            Assert.Equal(7777, cfg.ReproduceTickIntervalMs);
            Assert.Equal(3, cfg.MaxFloraRescanColumnsPerTick);
        }

        [Fact]
        public void ApplyPresetSelection_StillResetsPerfCadence()
        {
            var cfg = new EcosystemConfig
            {
                TickBudgetMs = 99,
                ReproduceTickIntervalMs = 7777,
            };

            EcosystemConfigSchema.ApplyPresetSelection(cfg, EcosystemBalancePresets.Natural);

            Assert.Equal(2, cfg.TickBudgetMs);
            Assert.Equal(3500, cfg.ReproduceTickIntervalMs);
        }

        [Fact]
        public void Schema_ExposesWildVineMaxHangDepthInTrees()
        {
            EcosystemConfigFieldDescriptor field =
                EcosystemConfigSchema.GetField(nameof(EcosystemConfig.WildVineMaxHangDepth));
            Assert.NotNull(field);
            Assert.Equal("trees", field.Category);
        }

        [Fact]
        public void MarkCustomIfPresetFieldEdited_SwitchesPreset()
        {
            var cfg = new EcosystemConfig { BalancePreset = EcosystemBalancePresets.Natural };
            EcosystemConfigSchema.MarkCustomIfPresetFieldEdited(cfg, nameof(EcosystemConfig.ReproduceChance));

            Assert.Equal(EcosystemBalancePresets.Custom, cfg.BalancePreset);
        }

        [Fact]
        public void Schema_ReproduceDebug_IsAdvancedCategory()
        {
            EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(nameof(EcosystemConfig.ReproduceDebug));
            Assert.NotNull(field);
            Assert.Equal("advanced", field.Category);
        }

        [Fact]
        public void Schema_SpreadCategory_OpensWithCoreSpreadTuning()
        {
            IReadOnlyList<EcosystemConfigFieldDescriptor> fields = EcosystemConfigSchema.GetCategoryFields("spread");
            Assert.NotEmpty(fields);
            Assert.Equal(nameof(EcosystemConfig.ReproduceRadius), fields[0].Name);
            Assert.Contains(fields, f => f.Name == nameof(EcosystemConfig.EnableTallgrassSpreadMaturation));
        }

        [Fact]
        public void Schema_MasterCategory_GroupsInspectNearTop()
        {
            IReadOnlyList<EcosystemConfigFieldDescriptor> fields = EcosystemConfigSchema.GetCategoryFields("master");
            int preset = IndexOf(fields, nameof(EcosystemConfig.BalancePreset));
            int inspect = IndexOf(fields, nameof(EcosystemConfig.EnableEcologyInspect));
            Assert.True(preset >= 0);
            Assert.True(inspect > preset);
        }

        static int IndexOf(IReadOnlyList<EcosystemConfigFieldDescriptor> fields, string name)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name == name) return i;
            }

            return -1;
        }
    }
}
