using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Polls establishing tallgrass until vanilla growth reaches a spread-eligible height.</summary>
    internal sealed class PendingTallgrassPromotion
    {
        readonly List<BlockPos> entries = new List<BlockPos>();
        readonly Dictionary<BlockPos, int> indexByPos = new Dictionary<BlockPos, int>();

        public void Add(BlockPos pos)
        {
            if (pos == null || indexByPos.ContainsKey(pos)) return;

            indexByPos[pos] = entries.Count;
            entries.Add(pos.Copy());
        }

        public void Remove(BlockPos pos)
        {
            if (pos == null || !indexByPos.TryGetValue(pos, out int index)) return;

            int last = entries.Count - 1;
            BlockPos removed = entries[index];
            if (index != last)
            {
                BlockPos moved = entries[last];
                entries[index] = moved;
                indexByPos[moved] = index;
            }

            entries.RemoveAt(last);
            indexByPos.Remove(removed);
        }

        public void Process(ICoreAPI api, EcosystemSystem ecosystem, int maxChecks)
        {
            if (entries.Count == 0 || api == null || ecosystem == null || maxChecks <= 0) return;
            if (!TallgrassSpreadMaturation.UsesMaturation(EcosystemConfig.Loaded)) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            var remove = new List<BlockPos>();
            int checkedCount = 0;

            for (int i = entries.Count - 1; i >= 0 && checkedCount < maxChecks; i--)
            {
                BlockPos pos = entries[i];
                checkedCount++;

                Block block = acc.GetBlock(pos);
                if (block == null || block.Id == 0
                    || PlantCodeHelper.ResolveEcologySpecies(block) != "tallgrass")
                {
                    remove.Add(pos);
                    continue;
                }

                if (!TallgrassSpreadMaturation.CanReproduceFrom(block))
                {
                    continue;
                }

                if (!LandClaimGuard.AllowsEcologyChange(api, pos))
                {
                    continue;
                }

                if (ecosystem.RegistryContains(pos))
                {
                    remove.Add(pos);
                    continue;
                }

                if (!EcosystemParticipant.TryFromBlock(block, out IEcosystemParticipant participant))
                {
                    remove.Add(pos);
                    continue;
                }

                ecosystem.RegisterReproducer(pos, participant, spawnBurst: false);
                ecosystem.InvalidateEnvironmentAround(pos);
                remove.Add(pos);
            }

            for (int r = 0; r < remove.Count; r++)
            {
                Remove(remove[r]);
            }
        }
    }
}
