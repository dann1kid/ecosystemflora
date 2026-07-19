using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Testing;
using Xunit;

namespace WildFarming.Tests
{
    /// <summary>Regression tests for F1–F3 performance fixes.</summary>
    public class PerfFindingF1F3Tests
    {
        static ReproducerEntry MakeEntry(int x, int z, string species, double nextAttemptHours)
        {
            return new ReproducerEntry(
                new BlockPos(x, 64, z),
                new AssetLocation("game", "flower-" + species + "-free"),
                new AssetLocation("game", "flower-" + species + "-free"),
                new PlantRequirements { Species = species, Habitat = EcologyHabitat.Terrestrial },
                nextAttemptHours);
        }

        static PendingSpreadIntent MakeIntent(int x, int z, Block block)
        {
            return new PendingSpreadIntent
            {
                TargetPos = new BlockPos(x, 64, z),
                SpreadBlock = block,
                Requirements = new PlantRequirements
                {
                    Species = "catmint",
                    Habitat = EcologyHabitat.Terrestrial,
                },
            };
        }

        static SpreadSolveRequest MakeRequest(BlockPos origin, Block spreadBlock = null)
        {
            return new SpreadSolveRequest
            {
                Origin = origin,
                SpreadBlock = spreadBlock,
                Requirements = new PlantRequirements
                {
                    Species = "catmint",
                    Habitat = EcologyHabitat.Terrestrial,
                },
                MinFitness = 0.01f,
                MaxSpawns = 1,
                RandomSeed = 1,
            };
        }

        [Fact]
        public void F1_TrySubmit_RejectsWhenPendingCapReached()
        {
            using var scanner = new BackgroundSpreadScanner();
            scanner.ConfigureLimits(pendingCap: 8, completedCap: 8);

            int accepted = 0;
            for (int i = 0; i < 40; i++)
            {
                // Distinct origins so coalesce does not reject first.
                if (scanner.TrySubmit(MakeRequest(new BlockPos(i, 64, 0)), out _))
                {
                    accepted++;
                }
            }

            Assert.Equal(8, accepted);
            Assert.Equal(8, scanner.PendingCount);
            Assert.True(scanner.RejectedSubmitCount >= 32);
        }

        [Fact]
        public void F1_TrySubmit_CoalescesSameOrigin()
        {
            using var scanner = new BackgroundSpreadScanner();
            scanner.ConfigureLimits(pendingCap: 64, completedCap: 64);

            var origin = new BlockPos(3, 64, 7);
            Assert.True(scanner.TrySubmit(MakeRequest(origin), out _));
            Assert.False(scanner.TrySubmit(MakeRequest(origin), out _));
            Assert.Equal(1, scanner.PendingCount);
            Assert.Equal(1, scanner.RejectedSubmitCount);
        }

        [Fact]
        public void F1_PollCompleted_RespectsMaxDrain()
        {
            using var scanner = new BackgroundSpreadScanner();
            scanner.ConfigureLimits(pendingCap: 64, completedCap: 64);
            Block[] blocks = EcologyTestBlocks.CreateCatalog();
            scanner.Start(blocks, workerCount: 1);

            for (int i = 0; i < 20; i++)
            {
                Assert.True(scanner.TrySubmit(MakeRequest(new BlockPos(i, 64, 0), blocks[2]), out _));
            }

            var deadline = System.DateTime.UtcNow.AddSeconds(5);
            while (scanner.PendingCount > 0 && System.DateTime.UtcNow < deadline)
            {
                System.Threading.Thread.Sleep(5);
            }

            Assert.True(scanner.CompletedCount >= 10, $"expected completed backlog, got {scanner.CompletedCount}");

            int drained = 0;
            const int maxDrain = 5;
            while (drained < maxDrain && scanner.TryTakeCompleted(out _))
            {
                drained++;
            }

            Assert.Equal(maxDrain, drained);
            Assert.True(scanner.CompletedCount >= 5);
        }

        [Fact]
        public void F2_DenseChunk_ScansNearAttempts_NotFullList()
        {
            var registry = new ReproducerRegistry();
            var cfg = new EcosystemConfig
            {
                EnableEventDrivenSpread = false,
                EnableChunkFairSpread = true,
                MaxSpreadAttemptsPerChunkPerTick = 2,
                MaxSpreadChunksVisitedPerTick = 4,
            };

            const int density = 200;
            for (int i = 0; i < density; i++)
            {
                registry.Add(MakeEntry(i % 32, i / 32 % 32, "catmint", nextAttemptHours: 0));
            }

            var scheduler = new SpreadChunkScheduler();
            int processed = scheduler.Process(
                registry,
                cfg,
                now: 10,
                maxTotalAttempts: 2,
                scopeChunks: null,
                _ => 24,
                entry =>
                {
                    entry.NextAttemptHours = 1000;
                    return true;
                },
                budgetTicks: 0,
                budgetWatch: null,
                out _,
                out int chunksVisited,
                out int maxAttemptsInChunk);

            Assert.Equal(2, processed);
            Assert.Equal(1, chunksVisited);
            Assert.Equal(2, maxAttemptsInChunk);
            Assert.Equal(2, scheduler.LastAttemptsProcessed);
            // Cursor RR: when all entries are due, scan count ≈ attempt count.
            Assert.True(
                scheduler.LastEntriesScanned <= 8,
                $"expected ≤8 scans for 2 attempts, got {scheduler.LastEntriesScanned}");
            Assert.True(scheduler.LastEntriesScanned >= processed);
        }

        [Fact]
        public void F3_LargePending_LookupsStayNearCommits()
        {
            Block flower = EcologyTestBlocks.CreateCatalog()[2];
            var queue = new PendingSpreadQueue();
            var cfg = new EcosystemConfig
            {
                MaxSpreadCommitChunksVisitedPerTick = 32,
                MaxSpreadCommitsPerChunkPerTick = 2,
            };

            const int chunks = 32;
            const int perChunk = 16;
            for (int c = 0; c < chunks; c++)
            {
                int cx = c * 32;
                for (int i = 0; i < perChunk; i++)
                {
                    queue.Enqueue(MakeIntent(cx + (i % 16), i / 16, flower));
                }
            }

            Assert.Equal(chunks * perChunk, queue.Count);

            int committed = queue.ProcessCommit(
                api: null,
                cfg,
                onCommitted: null,
                maxCommits: 64,
                budgetTicks: 0,
                budgetWatch: null,
                logFailures: false,
                onDropped: null,
                tryCommit: _ => true);

            Assert.Equal(64, committed);
            // Per-chunk deques: lookups scale with commits/chunks visited, not pending size.
            Assert.True(
                queue.LastIntentLookups < committed * 4,
                $"lookups {queue.LastIntentLookups} should stay near commits {committed}");
            Assert.Equal(chunks * perChunk - committed, queue.Count);
        }

        [Fact]
        public void F3_LookupsDoNotScaleWithPendingSize()
        {
            Block flower = EcologyTestBlocks.CreateCatalog()[2];
            var cfg = new EcosystemConfig
            {
                MaxSpreadCommitChunksVisitedPerTick = 16,
                MaxSpreadCommitsPerChunkPerTick = 2,
            };

            int LookupsForPending(int pendingCount)
            {
                var queue = new PendingSpreadQueue();
                for (int i = 0; i < pendingCount; i++)
                {
                    queue.Enqueue(MakeIntent((i % 64) * 32, (i / 64) * 32, flower));
                }

                queue.ProcessCommit(
                    api: null,
                    cfg,
                    onCommitted: null,
                    maxCommits: 16,
                    budgetTicks: 0,
                    budgetWatch: null,
                    logFailures: false,
                    onDropped: null,
                    tryCommit: _ => true);

                return queue.LastIntentLookups;
            }

            int small = LookupsForPending(64);
            int large = LookupsForPending(512);

            // Same commit/visit caps → lookups should stay in the same ballpark.
            Assert.True(
                large < small * 2.5,
                $"lookups should not grow with pending size: small={small}, large={large}");
        }
    }
}
