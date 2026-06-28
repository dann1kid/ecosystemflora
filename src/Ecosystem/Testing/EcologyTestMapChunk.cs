using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace WildFarming.Ecosystem.Testing
{
    /// <summary>Minimal map chunk for in-process ecology simulation tests.</summary>
    internal sealed class EcologyTestMapChunk : IMapChunk
    {
        public ushort[] RainHeightMap { get; set; } = new ushort[32 * 32];
        public byte[] CaveHeightDistort { get; set; }
        public EnumWorldGenPass CurrentPass { get; set; }
        public IMapRegion MapRegion { get; set; }
        public ushort[] SedimentaryThicknessMap { get; set; }
        public float[] SnowAccum { get; set; }
        public int[] TopRockIdMap { get; set; }
        public ushort[] WorldGenTerrainHeightMap { get; set; }
        public ushort YMax { get; set; } = 128;

        public byte[] GetData(string key) => null;
        public void SetData(string key, byte[] data) { }
        public byte[] GetModdata(string key) => null;
        public void SetModdata(string key, byte[] data) { }
        public void RemoveModdata(string key) { }
        public void SetModdata<T>(string key, T data) { }
        public T GetModdata<T>(string key, T defaultValue = default) => defaultValue;
        public void MarkFresh() { }
        public void MarkDirty() { }
    }
}
