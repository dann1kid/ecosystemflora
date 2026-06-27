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
            foreach (string file in Directory.GetFiles(PlantAssetDir, "*.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("fernphase-")
                    || name.StartsWith("tallgrassphase-")
                    || name.StartsWith("juvenile-fern-"))
                {
                    yield return name;
                }
            }
        }

        [Theory]
        [InlineData("en")]
        [InlineData("ru")]
        public void AllFernAndTallgrassSeasonalBlocks_HaveReadableNames(string langCode)
        {
            var lang = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(LangDir, langCode + ".json")));

            foreach (string code in SeasonalBlockCodes())
            {
                string key = "block-" + code;
                Assert.True(lang.ContainsKey(key), $"missing {langCode} name for {key}");
                Assert.False(string.IsNullOrWhiteSpace(lang[key]), key);
                Assert.DoesNotMatch(@"^(fernphase|tallgrassphase|juvenile-fern)-", lang[key]);
            }
        }

        [Fact]
        public void Cinnamonfern_Dieback_HasRussianReadableName()
        {
            var lang = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(LangDir, "ru.json")));

            Assert.Equal("Коричный папоротник (отмирание)", lang["block-fernphase-cinnamonfern-dieback"]);
            Assert.Equal("Высокая трава (покой)", lang["block-tallgrassphase-dormant-free"]);
        }
    }
}
