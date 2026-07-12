using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Wild vine columns may hang any length below the top cell; prune when the topmost block
    /// no longer attaches to a structural host block (wall behind or wall one block above the host slot).
    /// </summary>
    internal static class WildVineColumnSupport
    {
        public static bool HasColumnSupportAt(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (WildVineHelper.HasStructuralSupportBehind(acc, world, vinePos, info))
            {
                return true;
            }

            return HasContinuedHostWallAbove(acc, world, vinePos, info);
        }

        /// <summary>Top cell of the column has a host block attachment.</summary>
        public static bool IsColumnTopAnchored(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos anyInColumn,
            in WildVineInfo info)
        {
            if (acc == null || world == null || anyInColumn == null) return false;

            BlockPos top = WildVineHelper.FindHighestColumnCell(acc, anyInColumn, info);
            return HasColumnSupportAt(acc, world, top, info);
        }

        /// <summary>Wall continues one block above the host slot — top may hang from the cliff lip.</summary>
        internal static bool HasContinuedHostWallAbove(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info)
        {
            if (acc == null || world == null || vinePos == null) return false;

            BlockPos hostAbove = WildVineHelper.HostPos(vinePos, info.Facing).UpCopy();
            if (!acc.IsValidPos(hostAbove)) return false;

            Block hostBlock = acc.GetBlock(hostAbove);
            return WildVineHelper.IsStructuralWallHost(hostBlock, info.Facing);
        }

        /// <summary>Removes the whole column when the top block is no longer anchored.</summary>
        public static int PruneUnsupportedColumn(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos anyInColumn,
            System.Action<BlockPos> onCellRemoved = null)
        {
            if (acc == null || world == null || anyInColumn == null) return 0;

            Block start = acc.GetBlock(anyInColumn);
            if (!WildVineHelper.TryParse(start, out WildVineInfo info)) return 0;

            BlockPos top = WildVineHelper.FindHighestColumnCell(acc, anyInColumn, info);
            BlockPos bottom = WildVineHelper.FindLowestColumnCell(acc, anyInColumn, info);

            if (IsColumnTopAnchored(acc, world, top, info)) return 0;

            int removed = 0;
            for (int y = bottom.Y; y <= top.Y; y++)
            {
                BlockPos pos = new BlockPos(top.X, y, top.Z);
                Block block = acc.GetBlock(pos);
                if (!WildVineHelper.MatchesColumn(block, info)) continue;

                acc.SetBlock(0, pos);
                acc.MarkBlockDirty(pos);
                onCellRemoved?.Invoke(pos);
                removed++;
            }

            return removed;
        }

        /// <summary>After a host block changes, revalidate attached vine columns.</summary>
        public static void OnStructuralChange(
            ICoreAPI api,
            BlockPos changedPos,
            System.Action<BlockPos> onCellRemoved = null)
        {
            if (api == null || changedPos == null) return;
            if (!EcosystemConfig.Loaded.EnableWildVineEcology) return;

            IBlockAccessor acc = api.World.BlockAccessor;
            var prunedColumns = new HashSet<(int x, int z, string facing, bool tropical)>();

            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                BlockFacing face = BlockFacing.ALLFACES[i];
                BlockPos vinePos = changedPos.AddCopy(face);
                if (!acc.IsValidPos(vinePos)) continue;

                Block vine = acc.GetBlock(vinePos);
                if (!WildVineHelper.TryParse(vine, out WildVineInfo info)) continue;
                if (!WildVineHelper.HostPos(vinePos, info.Facing).Equals(changedPos)) continue;

                TryPruneColumnOnce(acc, api.World, vinePos, info, prunedColumns, onCellRemoved);
            }
        }

        static void TryPruneColumnOnce(
            IBlockAccessor acc,
            IWorldAccessor world,
            BlockPos vinePos,
            in WildVineInfo info,
            HashSet<(int x, int z, string facing, bool tropical)> prunedColumns,
            System.Action<BlockPos> onCellRemoved = null)
        {
            BlockPos top = WildVineHelper.FindHighestColumnCell(acc, vinePos, info);
            var key = (top.X, top.Z, info.Facing.Code, info.Tropical);
            if (!prunedColumns.Add(key)) return;

            PruneUnsupportedColumn(acc, world, top, onCellRemoved);
        }
    }
}
