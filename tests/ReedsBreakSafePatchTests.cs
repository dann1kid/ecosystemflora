using System.IO;
using System.Linq;
using Xunit;

namespace WildFarming.Tests
{
    public class ReedsBreakSafePatchTests
    {
        [Fact]
        public void ReedsPatch_replacesBlockReedsClass()
        {
            string path = ResolveRepoPath("assets", "ecosystemflora", "patches", "reeds-break-safe.json");
            Assert.True(File.Exists(path));
            string json = File.ReadAllText(path);
            Assert.Contains("reedpapyrus.json", json);
            Assert.Contains("BlockReedsSafe", json);
        }

        static string ResolveRepoPath(params string[] parts)
        {
            string dir = Directory.GetCurrentDirectory();
            while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "wildfarming.sln")))
            {
                dir = Directory.GetParent(dir)?.FullName;
            }

            return Path.Combine(new[] { dir ?? Directory.GetCurrentDirectory() }.Concat(parts).ToArray());
        }
    }
}
