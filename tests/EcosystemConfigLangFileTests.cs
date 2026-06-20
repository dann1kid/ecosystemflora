using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Vintagestory.API.Common;
using WildFarming.Ecosystem.Config;
using Xunit;

namespace WildFarming.Tests
{
    public class EcosystemConfigLangFileTests
    {
        static string RepoRoot =>
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));

        static string LangDir => Path.Combine(RepoRoot, "assets", "ecosystemflora", "lang");

        [Fact]
        public void LangBuilder_CoversAllSchemaFields()
        {
            var en = ConfigFieldLangBuilder.BuildLangFile("en");
            var ru = ConfigFieldLangBuilder.BuildLangFile("ru");

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                string titleKey = "ecosystemflora:config-field-" + field.Name;
                string descKey = titleKey + "-desc";
                Assert.True(en.ContainsKey(titleKey), "Missing EN title: " + field.Name);
                Assert.True(en.ContainsKey(descKey), "Missing EN desc: " + field.Name);
                Assert.True(ru.ContainsKey(titleKey), "Missing RU title: " + field.Name);
                Assert.True(ru.ContainsKey(descKey), "Missing RU desc: " + field.Name);
                Assert.False(string.IsNullOrWhiteSpace(en[descKey]));
                Assert.False(string.IsNullOrWhiteSpace(ru[descKey]));
                Assert.True(ConfigFieldDescriptions.TryGet(field.Name, out _),
                    "Missing hand-tuned description: " + field.Name);
                if (field.Kind != ConfigFieldKind.String)
                {
                    Assert.True(HasDirectionHint(en[descKey]), "EN desc lacks direction hint: " + field.Name);
                    Assert.True(HasDirectionHintRu(ru[descKey]), "RU desc lacks direction hint: " + field.Name);
                }
            }
        }

        static bool HasDirectionHint(string desc) =>
            desc.Contains("On:", StringComparison.Ordinal)
            || desc.Contains("Off:", StringComparison.Ordinal)
            || desc.Contains("Higher", StringComparison.Ordinal)
            || desc.Contains("Lower", StringComparison.Ordinal)
            || desc.Contains("chunk =", StringComparison.Ordinal);

        static bool HasDirectionHintRu(string desc) =>
            desc.Contains("Вкл.:", StringComparison.Ordinal)
            || desc.Contains("Выкл.:", StringComparison.Ordinal)
            || desc.Contains("Больше", StringComparison.Ordinal)
            || desc.Contains("Меньше", StringComparison.Ordinal)
            || desc.Contains("chunk =", StringComparison.Ordinal);

        [Fact]
        public void RegenerateLangJsonFiles()
        {
            Directory.CreateDirectory(LangDir);

            var enFields = ConfigFieldLangBuilder.BuildLangFile("en");
            var ruFields = ConfigFieldLangBuilder.BuildLangFile("ru");

            WriteLangFile("en-configfields.json", enFields);
            WriteLangFile("ru-configfields.json", ruFields);
            MergeConfigFieldsIntoMainLang("en.json", enFields);
            MergeConfigFieldsIntoMainLang("ru.json", ruFields);
        }

        static void MergeConfigFieldsIntoMainLang(string fileName, Dictionary<string, string> configFields)
        {
            string path = Path.Combine(LangDir, fileName);
            var main = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                ?? new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> kv in configFields)
            {
                main[kv.Key] = kv.Value;
            }

            var sorted = main.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            File.WriteAllText(path, JsonUtil.ToPrettyString(sorted));
        }

        static void WriteLangFile(string fileName, Dictionary<string, string> entries)
        {
            var sorted = entries.OrderBy(kv => kv.Key, System.StringComparer.Ordinal)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            string json = JsonUtil.ToPrettyString(sorted);
            File.WriteAllText(Path.Combine(LangDir, fileName), json);
        }
    }
}
