using System.IO;
using System.Linq;
using Xunit;

namespace WildFarming.Tests
{
    public class BrownsedgeHarvestedVisualPatchTests
    {
        [Fact]
        public void Patch_addsHarvestedShapeAndCollisionBox()
        {
            string patchPath = ResolveRepoPath("assets", "ecosystemflora", "patches", "brownsedge-harvested-visual.json");
            Assert.True(File.Exists(patchPath));
            string json = File.ReadAllText(patchPath);
            Assert.Contains("sedge-harvested", json);
            Assert.Contains("addmerge", json);
            Assert.Contains("collisionboxByType", json);

            string shapePath = ResolveRepoPath(
                "assets", "ecosystemflora", "shapes", "block", "plant", "reedpapyrus", "sedge-harvested.json");
            Assert.True(File.Exists(shapePath));
            string shape = File.ReadAllText(shapePath);
            Assert.Contains("brownsedge-harvested", shape);
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
