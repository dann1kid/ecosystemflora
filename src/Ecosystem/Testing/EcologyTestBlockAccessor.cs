using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using BlockUpdate = Vintagestory.API.Common.BlockUpdate;

namespace WildFarming.Ecosystem.Testing
{
    /// <summary>In-memory block grid implementing IBlockAccessor for ecology integration tests.</summary>
    internal sealed class EcologyTestBlockAccessor : IBlockAccessor
    {
        readonly Dictionary<(int x, int y, int z), int> cells = new Dictionary<(int, int, int), int>();
        readonly Dictionary<(int cx, int cz), EcologyTestMapChunk> mapChunks = new Dictionary<(int, int), EcologyTestMapChunk>();
        readonly IList<Block> blocks;
        readonly Block air;

        public float Rainfall { get; set; } = 0.55f;
        public float Temperature { get; set; } = 15f;

        public EcologyTestBlockAccessor(IList<Block> blocks)
        {
            this.blocks = blocks;
            air = blocks != null && blocks.Count > 0 ? blocks[0] : new Block { BlockId = 0 };
        }

        public int MapSizeY { get; set; } = 128;
        public Vec3i MapSize => new Vec3i(0, MapSizeY, 0);
        public int MapSizeX => 0;
        public int MapSizeZ => 0;
        public int ChunkSize => GlobalConstants.ChunkSize;
        public int RegionSize => 0;
        public int RegionMapSizeX => 0;
        public int RegionMapSizeY => 0;
        public int RegionMapSizeZ => 0;
        public bool UpdateSnowAccumMap { get; set; }

        public EcologyTestMapChunk GetOrCreateMapChunk(int chunkX, int chunkZ)
        {
            var key = (chunkX, chunkZ);
            if (!mapChunks.TryGetValue(key, out EcologyTestMapChunk chunk))
            {
                chunk = new EcologyTestMapChunk { YMax = (ushort)(MapSizeY - 1) };
                mapChunks[key] = chunk;
            }

            return chunk;
        }

        public void SetRainHeight(int worldX, int worldZ, int y)
        {
            int cs = GlobalConstants.ChunkSize;
            int cx = worldX / cs;
            int cz = worldZ / cs;
            int lx = worldX - cx * cs;
            int lz = worldZ - cz * cs;
            EcologyTestMapChunk chunk = GetOrCreateMapChunk(cx, cz);
            chunk.RainHeightMap[lx * cs + lz] = (ushort)y;
        }

        public void SetBlockCode(string code, BlockPos pos)
        {
            Block block = ResolveBlock(code);
            SetBlock(block.BlockId, pos);
        }

        Block ResolveBlock(string code)
        {
            if (blocks == null || string.IsNullOrEmpty(code)) return air;
            var loc = new AssetLocation(code);
            for (int i = 0; i < blocks.Count; i++)
            {
                Block b = blocks[i];
                if (b?.Code != null && b.Code.Equals(loc)) return b;
            }

            return air;
        }

        public Block GetBlock(BlockPos pos) => pos == null ? air : GetBlock(pos.X, pos.Y, pos.Z);

        public Block GetBlock(int x, int y, int z)
        {
            if (y < 0 || y >= MapSizeY) return air;
            if (!cells.TryGetValue((x, y, z), out int id)) return air;
            if (id <= 0 || id >= blocks.Count) return air;
            return blocks[id] ?? air;
        }

        public int GetBlockId(int x, int y, int z) => GetBlock(x, y, z).Id;

        public int GetBlockId(BlockPos pos) => GetBlock(pos).Id;

        public void SetBlock(int blockId, BlockPos pos, ItemStack byItemStack = null)
        {
            if (pos == null) return;
            SetBlock(blockId, pos.X, pos.Y, pos.Z, byItemStack);
        }

        public void SetBlock(int blockId, int x, int y, int z, ItemStack byItemStack = null)
        {
            if (y < 0 || y >= MapSizeY) return;
            if (blockId <= 0) cells.Remove((x, y, z));
            else cells[(x, y, z)] = blockId;
        }

        public ClimateCondition SampleClimate() =>
            new ClimateCondition { Rainfall = Rainfall, Temperature = Temperature };

        public ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0) =>
            SampleClimate();

        public ClimateCondition GetClimateAt(int posX, int posY, int posZ, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0) =>
            SampleClimate();

        public ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition asClimate, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0) =>
            SampleClimate();

        public ClimateCondition GetClimateAt(BlockPos pos, int climateMapIndex) => SampleClimate();

        public IMapChunk GetMapChunk(int chunkX, int chunkZ)
        {
            mapChunks.TryGetValue((chunkX, chunkZ), out EcologyTestMapChunk chunk);
            return chunk;
        }

        public IMapChunk GetMapChunk(Vec2i chunkCoord) => GetMapChunk(chunkCoord.X, chunkCoord.Y);

        public bool IsValidPos(int x, int y, int z) => y >= 0 && y < MapSizeY;
        public bool IsValidPos(BlockPos pos) => pos != null && IsValidPos(pos.X, pos.Y, pos.Z);

        public bool IsSideSolid(int x, int y, int z, BlockFacing facing)
        {
            Block block = GetBlock(x, y, z);
            return block != null && block.SideSolid[facing.Index];
        }

        public int GetRainMapHeightAt(int posX, int posZ)
        {
            int cs = GlobalConstants.ChunkSize;
            int cx = posX / cs;
            int cz = posZ / cs;
            IMapChunk chunk = GetMapChunk(cx, cz);
            if (chunk?.RainHeightMap == null) return 0;
            int lx = posX - cx * cs;
            int lz = posZ - cz * cs;
            return chunk.RainHeightMap[lx * cs + lz];
        }

        public int GetRainMapHeightAt(BlockPos pos) => GetRainMapHeightAt(pos.X, pos.Z);

        public void MarkBlockDirty(BlockPos pos) { }
        public void MarkBlockDirty(BlockPos pos, IPlayer byPlayer) { }
        public void MarkBlockDirty(BlockPos pos, Action onComplete) { }
        public void MarkBlockEntityDirty(BlockPos pos) { }
        public void MarkBlockModified(BlockPos pos) { }
        public void MarkChunkDecorsModified(BlockPos pos) { }
        public void MarkAbsorptionChanged(int oldBlockId, int newBlockId, BlockPos pos) { }

        public Block GetBlock(int blockId) =>
            blockId >= 0 && blockId < blocks.Count ? blocks[blockId] : air;

        public Block GetBlock(AssetLocation blockCode) => ResolveBlock(blockCode?.ToString() ?? "game:air");

        public Block GetBlock(BlockPos pos, int layer) => GetBlock(pos);

        public Block GetBlock(int x, int y, int z, int layer) => GetBlock(x, y, z);

        public Block GetBlockOrNull(int x, int y, int z, int layer) => GetBlock(x, y, z);

        public Block GetBlockRaw(int x, int y, int z, int layer) => GetBlock(x, y, z);

        public Block GetMostSolidBlock(BlockPos pos) => GetBlock(pos);
        public Block GetMostSolidBlock(int x, int y, int z) => GetBlock(x, y, z);

        public BlockEntity GetBlockEntity(BlockPos pos) => null;
        public BlockEntity GetBlockEntity(int x, int y, int z) => null;
        public T GetBlockEntity<T>(BlockPos pos) where T : BlockEntity => null;

        public void SetBlock(int blockId, BlockPos pos) => SetBlock(blockId, pos, null);
        public void SetBlock(int blockId, BlockPos pos, int byPlayerId) => SetBlock(blockId, pos, null);
        public void ExchangeBlock(int blockId, BlockPos pos) => SetBlock(blockId, pos);

        public void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f) { }
        public bool BreakDecor(BlockPos pos, BlockFacing facing, int? byPlayerId = null) => false;
        public void DamageBlock(BlockPos pos, BlockFacing facing, float damage) { }
        public void RemoveBlockEntity(BlockPos pos) { }
        public void SpawnBlockEntity(BlockEntity blockEntity) { }
        public void SpawnBlockEntity(string classname, BlockPos pos, ItemStack byItemStack = null) { }

        public Block GetDecor(BlockPos pos, int index) => null;
        public Block[] GetDecors(BlockPos pos) => null;
        public Dictionary<int, Block> GetSubDecors(BlockPos pos) => null;
        public bool SetDecor(Block block, BlockPos pos, BlockFacing onFace) => false;
        public bool SetDecor(Block block, BlockPos pos, int index) => false;

        public int GetLightLevel(BlockPos pos, EnumLightLevelType lightType) => 15;
        public int GetLightLevel(int x, int y, int z, EnumLightLevelType lightType) => 15;
        public Vec4f GetLightRGBs(BlockPos pos) => new Vec4f();
        public Vec4f GetLightRGBs(int x, int y, int z) => new Vec4f();
        public int GetLightRGBsAsInt(int x, int y, int z) => 0;
        public void RemoveBlockLight(byte[] oldLightHsv, BlockPos pos) { }

        public int GetTerrainMapheightAt(BlockPos pos) => GetRainMapHeightAt(pos);

        public int GetDistanceToRainFall(BlockPos pos, int range, int radius) => 0;
        public Vec3d GetWindSpeedAt(BlockPos pos) => new Vec3d();
        public Vec3d GetWindSpeedAt(Vec3d pos) => new Vec3d();

        public bool IsNotTraversable(BlockPos pos) => false;
        public bool IsNotTraversable(double x, double y, double z) => false;
        public bool IsNotTraversable(double x, double y, double z, int dimension) => false;

        public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ) => null;
        public IWorldChunk GetChunk(long index3d) => null;
        public IWorldChunk GetChunkAtBlockPos(BlockPos pos) => null;
        public IWorldChunk GetChunkAtBlockPos(int x, int y, int z) => null;
        public IMapRegion GetMapRegion(int regionX, int regionZ) => null;
        public IMapChunk GetMapChunkAtBlockPos(BlockPos pos) =>
            GetMapChunk(pos.X / GlobalConstants.ChunkSize, pos.Z / GlobalConstants.ChunkSize);

        public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false) { }
        public void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onBlockPos = null) { }
        public void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onBlockPos = null) { }
        public void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure) { }
        public void WalkStructures(BlockPos minPos, BlockPos maxPos, Action<GeneratedStructure> onStructure) { }

        public void BeginMark() { }
        public List<BlockUpdate> Commit() => null;
        public void Rollback() { }
        public void RedrawNeighbouringChunk(BlockPos pos, BlockFacing facing) { }
        public void TriggerNeighbourBlockUpdate(BlockPos pos) { }
        public IMiniDimension CreateMiniDimension(Vec3d pos) => null;
    }
}
