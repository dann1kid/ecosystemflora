using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using WildFarming.Blocks;
using Xunit;

namespace WildFarming.Tests
{
    public class BrownsedgeHarvestedDecalFitTests
    {
        [Fact]
        public void Apply_scalesFullHeightMeshToRootSelectionBox()
        {
            var mesh = new MeshData(8, 24, withNormals: false, withUv: false, withRgba: false);
            mesh.SetVerticesCount(8);
            // Unit cube spanning y=0..2 (vanilla sedge-like height).
            SetVertex(mesh, 0, 0, 0, 0);
            SetVertex(mesh, 1, 1, 0, 0);
            SetVertex(mesh, 2, 0, 2, 0);
            SetVertex(mesh, 3, 1, 2, 0);
            SetVertex(mesh, 4, 0, 0, 1);
            SetVertex(mesh, 5, 1, 0, 1);
            SetVertex(mesh, 6, 0, 2, 1);
            SetVertex(mesh, 7, 1, 2, 1);

            BrownsedgeHarvestedDecalFit.Apply(mesh);

            GetBounds(mesh, out float minY, out float maxY, out float minX, out float maxX, out float minZ, out float maxZ);

            Assert.InRange(maxY - minY, 0.24f, 0.26f);
            Assert.InRange(maxX - minX, 0.49f, 0.51f);
            Assert.InRange(maxZ - minZ, 0.49f, 0.51f);
            Assert.InRange(minY, -0.01f, 0.01f);
        }

        static void SetVertex(MeshData mesh, int index, float x, float y, float z)
        {
            mesh.xyz[index * 3] = x;
            mesh.xyz[index * 3 + 1] = y;
            mesh.xyz[index * 3 + 2] = z;
        }

        static void GetBounds(
            MeshData mesh,
            out float minY,
            out float maxY,
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ)
        {
            minX = minY = minZ = float.MaxValue;
            maxX = maxY = maxZ = float.MinValue;
            for (int i = 0; i < mesh.VerticesCount; i++)
            {
                float x = mesh.xyz[i * 3];
                float y = mesh.xyz[i * 3 + 1];
                float z = mesh.xyz[i * 3 + 2];
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (z < minZ) minZ = z;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
            }
        }
    }
}
