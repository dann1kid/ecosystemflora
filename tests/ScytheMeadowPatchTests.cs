using System.IO;
using System.Linq;
using Xunit;

namespace WildFarming.Tests
{
    public class ScytheMeadowPatchTests
    {
        [Fact]
        public void ScythePatch_includesJuvenileMeadowPrefixes()
        {
            string path = ResolveRepoPath("assets", "ecosystemflora", "patches", "scythe-flowers.json");
            Assert.True(File.Exists(path));
            string json = File.ReadAllText(path);
            Assert.Contains("flower-", json);
            Assert.Contains("juvenile-flower-", json);
            Assert.Contains("juvenile-sedge-", json);
            Assert.Contains("tallplant-brownsedge-", json);
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
