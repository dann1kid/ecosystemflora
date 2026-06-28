using System;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;

namespace WildFarming.Tests.Harness
{
    internal sealed class EcosystemSimHost : IDisposable
    {
        readonly EcosystemConfig priorConfig;

        public EcosystemSystem Eco { get; }
        public EcologyTestBlockAccessor Accessor { get; }
        public EcologyTestCalendar Calendar { get; }
        public Block[] Blocks { get; }
        public Mock<ICoreAPI> Api { get; }

        EcosystemSimHost(
            EcosystemSystem eco,
            EcologyTestBlockAccessor accessor,
            EcologyTestCalendar calendar,
            Block[] blocks,
            Mock<ICoreAPI> api,
            EcosystemConfig priorConfig)
        {
            Eco = eco;
            Accessor = accessor;
            Calendar = calendar;
            Blocks = blocks;
            Api = api;
            this.priorConfig = priorConfig;
        }

        public static EcosystemSimHost Create(EcosystemConfig config = null)
        {
            EcosystemConfig prior = EcosystemConfig.Loaded;
            EcosystemConfig cfg = config ?? EcosystemConfig.ForIntegrationTests();
            EcosystemConfig.Loaded = cfg;

            Block[] blocks = EcologyTestBlocks.CreateCatalog();
            var accessor = new EcologyTestBlockAccessor(blocks);
            var calendar = new EcologyTestCalendar();
            var rand = new Random(12345);
            var eventApi = new Mock<IEventAPI>();
            var logger = new Mock<ILogger>();
            var world = new Mock<IWorldAccessor>();
            world.Setup(w => w.BlockAccessor).Returns(accessor);
            world.Setup(w => w.Blocks).Returns(blocks);
            world.Setup(w => w.Rand).Returns(rand);
            world.Setup(w => w.Calendar).Returns(calendar);
            world.Setup(w => w.GetBlock(It.IsAny<AssetLocation>()))
                .Returns((AssetLocation loc) => ResolveBlock(blocks, loc));
            world.Setup(w => w.GetBlock(It.IsAny<int>()))
                .Returns((int id) => id >= 0 && id < blocks.Length ? blocks[id] : blocks[0]);

            var api = new Mock<ICoreAPI>();
            api.Setup(a => a.World).Returns(world.Object);
            api.Setup(a => a.Side).Returns(EnumAppSide.Server);
            api.Setup(a => a.Logger).Returns(logger.Object);
            api.Setup(a => a.Event).Returns(eventApi.Object);
            api.Setup(a => a.LoadModConfig<EcosystemConfig>(It.IsAny<string>()))
                .Returns((EcosystemConfig)null);
            var modLoader = new Mock<IModLoader>();
            modLoader.Setup(m => m.GetModSystem(It.IsAny<string>())).Returns((ModSystem)null);
            api.Setup(a => a.ModLoader).Returns(modLoader.Object);

            var eco = new EcosystemSystem();
            eco.InitForSimulation(api.Object);

            return new EcosystemSimHost(eco, accessor, calendar, blocks, api, prior);
        }

        static Block ResolveBlock(Block[] blocks, AssetLocation loc)
        {
            if (loc == null) return blocks[0];
            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i]?.Code != null && blocks[i].Code.Equals(loc)) return blocks[i];
            }

            return blocks[0];
        }

        public SimWorldBuilder World => new SimWorldBuilder(Accessor);

        public void LoadChunk(Vec2i chunkCoord)
        {
            Eco.Test_EnqueueChunkScan(chunkCoord);
            RunChunkScanUntilIdle(chunkCoord);
        }

        public void RunChunkScanUntilIdle(Vec2i chunkCoord, int maxTicks = 64)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                Eco.Test_ChunkScanTick();
                if (Eco.Test_IsChunkRegistrationFinished(chunkCoord)) return;
            }
        }

        public void TickReproduce(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                Eco.Test_ReproduceTick();
            }
        }

        public void TickStress(int times = 1)
        {
            for (int i = 0; i < times; i++)
            {
                Eco.Test_StressTick();
            }
        }

        public void AdvanceHours(double hours)
        {
            Calendar.TotalHours += hours;
            Calendar.TotalDays = Calendar.TotalDays + (hours / Calendar.HoursPerDay);
        }

        public Block BlockAt(BlockPos pos) => Accessor.GetBlock(pos);

        public string BlockCodeAt(BlockPos pos)
        {
            Block block = Accessor.GetBlock(pos);
            if (block?.Code == null || block.Id == 0) return "game:air";
            return block.Code.Domain + ":" + block.Code.Path;
        }

        public void Dispose()
        {
            EcosystemConfig.Loaded = priorConfig;
        }
    }
}
