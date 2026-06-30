using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace WildFarming.Tests
{
    public class SeasonalBlockLangTests
    {
        static readonly string PlantAssetDir = Path.Combine(
            FindRepoRoot(),
            "assets",
            "ecosystemflora",
            "blocktypes",
            "plant");

        static readonly string LangDir = Path.Combine(
            FindRepoRoot(),
            "assets",
            "ecosystemflora",
            "lang");

        static readonly string[] SeasonalPrefixes =
        {
            "flowerphase-",
            "fernphase-",
            "tallgrassphase-",
            "sedgephase-",
            "juvenile-flower-",
            "juvenile-fern-",
            "juvenile-sedge-",
        };

        static string FindRepoRoot()
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "wildfarming.sln")))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
            }

            return Directory.GetCurrentDirectory();
        }

        static IEnumerable<string> SeasonalBlockCodes()
        {
            var codes = new HashSet<string>();
            foreach (string file in Directory.GetFiles(PlantAssetDir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (!IsSeasonalAssetName(name)) continue;

                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("code", out JsonElement codeEl)) continue;
                string baseCode = codeEl.GetString();
                if (string.IsNullOrEmpty(baseCode)) continue;

                if (root.TryGetProperty("variantgroups", out JsonElement groups))
                {
                    bool hasCover = false;
                    foreach (JsonElement group in groups.EnumerateArray())
                    {
                        if (group.TryGetProperty("code", out JsonElement gCode)
                            && gCode.GetString() == "cover"
                            && group.TryGetProperty("states", out JsonElement states))
                        {
                            hasCover = true;
                            foreach (JsonElement state in states.EnumerateArray())
                            {
                                codes.Add(baseCode + "-" + state.GetString());
                            }
                        }
                    }

                    if (hasCover) continue;
                }

                if (Regex.IsMatch(baseCode, @"^fernphase-.+-(dormant|dieback|sporulating)$"))
                {
                    codes.Add(baseCode);
                    codes.Add(baseCode + "-snow");
                }
            }

            foreach (string code in codes)
            {
                yield return code;
            }
        }

        static bool IsSeasonalAssetName(string name)
        {
            foreach (string prefix in SeasonalPrefixes)
            {
                if (name.StartsWith(prefix)) return true;
            }

            return false;
        }

        [Theory]
        [InlineData("en")]
        [InlineData("ru")]
        [InlineData("de")]
        public void AllSeasonalCoverBlocks_HaveReadableNames(string langCode)
        {
            var lang = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(LangDir, langCode + ".json")));

            foreach (string code in SeasonalBlockCodes())
            {
                string key = "block-" + code;
                Assert.True(lang.ContainsKey(key), $"missing {langCode} name for {key}");
                Assert.False(string.IsNullOrWhiteSpace(lang[key]), key);
                Assert.DoesNotMatch(
                    @"^(flowerphase|fernphase|tallgrassphase|sedgephase|juvenile-flower|juvenile-fern|juvenile-sedge)-",
                    lang[key]);
            }
        }

        [Fact]
        public void Cinnamonfern_Dieback_HasRussianReadableName()
        {
            var lang = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(LangDir, "ru.json")));

            Assert.Equal("Коричный папоротник (отмирание)", lang["block-fernphase-cinnamonfern-dieback-free"]);
            Assert.Equal("Высокая трава (покой)", lang["block-tallgrassphase-dormant-free"]);
            Assert.Equal("Орляк (активная фаза)", lang["block-fernphase-eaglefern-sporulating-snow"]);
        }
    }
}
