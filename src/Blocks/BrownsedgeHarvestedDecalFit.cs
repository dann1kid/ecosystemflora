using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace WildFarming.Blocks
{
    /// <summary>Scales break-crack decals to the harvested brown sedge root selection box.</summary>
    internal static class BrownsedgeHarvestedDecalFit
    {
        internal static readonly Cuboidf BreakBox = new Cuboidf(0.25f, 0f, 0.25f, 0.75f, 0.25f, 0.75f);

        internal static void Apply(MeshData mesh)
        {
            if (mesh == null || mesh.VerticesCount <= 0)
                return;

            ComputeBounds(mesh, out float minX, out float minY, out float minZ, out float maxX, out float maxY, out float maxZ);

            float width = Math.Max(maxX - minX, 0.001f);
            float height = Math.Max(maxY - minY, 0.001f);
            float depth = Math.Max(maxZ - minZ, 0.001f);

            float sx = BreakBox.Width / width;
            float sy = BreakBox.Height / height;
            float sz = BreakBox.Length / depth;

            var origin = new Vec3f(minX + width * 0.5f, minY, minZ + depth * 0.5f);
            mesh.Scale(origin, sx, sy, sz);

            ComputeBounds(mesh, out minX, out minY, out minZ, out maxX, out maxY, out maxZ);

            float targetCenterX = BreakBox.X1 + BreakBox.Width * 0.5f;
            float targetCenterZ = BreakBox.Z1 + BreakBox.Length * 0.5f;
            mesh.Translate(
                targetCenterX - (minX + maxX) * 0.5f,
                BreakBox.Y1 - minY,
                targetCenterZ - (minZ + maxZ) * 0.5f);
        }

        static void ComputeBounds(
            MeshData mesh,
            out float minX,
            out float minY,
            out float minZ,
            out float maxX,
            out float maxY,
            out float maxZ)
        {
            minX = minY = minZ = float.MaxValue;
            maxX = maxY = maxZ = float.MinValue;

            float[] xyz = mesh.xyz;
            int count = mesh.VerticesCount;
            for (int i = 0; i < count; i++)
            {
                int o = i * 3;
                float x = xyz[o];
                float y = xyz[o + 1];
                float z = xyz[o + 2];
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
