using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem
{
    internal enum FerntreeTopMaturity
    {
        None = 0,
        Young = 1,
        Medium = 2,
        Old = 3,
    }

    /// <summary>Column structure for vanilla <c>game:ferntree-normal-*</c> blocks.</summary>
    internal static class FerntreeStructure
    {
        const string Prefix = "ferntree-normal-";
        const string TrunkPath = Prefix + "trunk";
        const string FoliagePath = Prefix + "foliage";

        static readonly string[] TopPaths =
        {
            Prefix + "trunk-top-young",
            Prefix + "trunk-top-medium",
            Prefix + "trunk-top-old",
        };

        public static bool IsFerntreeBlock(Block block)
        {
            string path = block?.Code?.Path;
            return !string.IsNullOrEmpty(path) && path.StartsWith(Prefix);
        }

        public static bool IsTrunkBlock(Block block) =>
            block?.Code?.Path == TrunkPath;

        public static bool IsFoliageBlock(Block block) =>
            block?.Code?.Path == FoliagePath;

        public static bool IsTopBlock(Block block)
        {
            string path = block?.Code?.Path;
            if (string.IsNullOrEmpty(path)) return false;

            for (int i = 0; i < TopPaths.Length; i++)
            {
                if (path == TopPaths[i]) return true;
            }

            return false;
        }

        public static FerntreeTopMaturity ParseTopMaturity(Block block)
        {
            string path = block?.Code?.Path;
            if (path == TopPaths[0]) return FerntreeTopMaturity.Young;
            if (path == TopPaths[1]) return FerntreeTopMaturity.Medium;
            if (path == TopPaths[2]) return FerntreeTopMaturity.Old;
            return FerntreeTopMaturity.None;
        }

        public static Block ResolveTopBlock(IBlockAccessor acc, FerntreeTopMaturity maturity)
        {
            if (acc == null) return null;

            int index = (int)maturity - 1;
            if (index < 0 || index >= TopPaths.Length) return null;

            Block block = acc.GetBlock(new AssetLocation("game:" + TopPaths[index]));
            return block != null && block.Id != 0 ? block : null;
        }

        public static BlockPos GetTrunkBase(IBlockAccessor acc, BlockPos pos)
        {
            if (acc == null || pos == null) return pos?.Copy();

            BlockPos scan = pos.Copy();
            Block at = acc.GetBlock(scan);
            if (!IsFerntreeBlock(at))
            {
                return pos.Copy();
            }

            if (IsFoliageBlock(at))
            {
                for (int dy = 0; dy <= 24; dy++)
                {
                    BlockPos below = new BlockPos(scan.X, scan.Y - dy, scan.Z, scan.dimension);
                    Block belowBlock = acc.GetBlock(below);
                    if (IsTrunkBlock(belowBlock) || IsTopBlock(belowBlock))
                    {
                        scan = below;
                        break;
                    }
                }
            }

            while (true)
            {
                BlockPos below = scan.DownCopy();
                if (!acc.IsValidPos(below)) break;
                if (!IsTrunkBlock(acc.GetBlock(below))) break;
                scan.Set(below);
            }

            return scan;
        }

        public static int MeasureTrunkSegmentCount(IBlockAccessor acc, BlockPos basePos)
        {
            if (acc == null || basePos == null) return 0;

            int count = 0;
            BlockPos scan = basePos.Copy();
            while (acc.IsValidPos(scan))
            {
                Block block = acc.GetBlock(scan);
                if (IsTrunkBlock(block))
                {
                    count++;
                    scan.Up();
                    continue;
                }

                if (IsTopBlock(block) && count > 0)
                {
                    count++;
                }

                break;
            }

            return count;
        }

        public static BlockPos FindTopPos(IBlockAccessor acc, BlockPos basePos)
        {
            if (acc == null || basePos == null) return basePos?.Copy();

            BlockPos scan = basePos.Copy();
            BlockPos last = basePos.Copy();

            while (acc.IsValidPos(scan))
            {
                Block block = acc.GetBlock(scan);
                if (IsTrunkBlock(block) || IsTopBlock(block))
                {
                    last = scan.Copy();
                    scan.Up();
                    continue;
                }

                break;
            }

            return last;
        }

        public static void CollectColumnBlocks(IBlockAccessor acc, BlockPos basePos, List<BlockPos> output)
        {
            output.Clear();
            if (acc == null || basePos == null) return;

            BlockPos top = FindTopPos(acc, basePos);
            for (int y = basePos.Y; y <= top.Y; y++)
            {
                var p = new BlockPos(basePos.X, y, basePos.Z, basePos.dimension);
                Block block = acc.GetBlock(p);
                if (IsTrunkBlock(block) || IsTopBlock(block))
                {
                    output.Add(p.Copy());
                }
            }

            for (int y = basePos.Y; y <= top.Y; y++)
            {
                for (int i = 0; i < 4; i++)
                {
                    int dx = (i == 0) ? 1 : (i == 1) ? -1 : 0;
                    int dz = (i == 2) ? 1 : (i == 3) ? -1 : 0;
                    var p = new BlockPos(basePos.X + dx, y, basePos.Z + dz, basePos.dimension);
                    if (IsFoliageBlock(acc.GetBlock(p)))
                    {
                        output.Add(p.Copy());
                    }
                }
            }
        }

        public static int RemoveColumn(IBlockAccessor acc, BlockPos basePos)
        {
            if (acc == null || basePos == null) return 0;

            var blocks = new List<BlockPos>(32);
            CollectColumnBlocks(acc, basePos, blocks);
            int removed = 0;

            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                BlockPos pos = blocks[i];
                if (!acc.IsValidPos(pos)) continue;
                if (acc.GetBlock(pos).Id == 0) continue;

                acc.SetBlock(0, pos);
                acc.MarkBlockDirty(pos);
                removed++;
            }

            return removed;
        }

        public static bool TrySetTopMaturity(IBlockAccessor acc, BlockPos basePos, FerntreeTopMaturity maturity)
        {
            if (acc == null || basePos == null || maturity == FerntreeTopMaturity.None) return false;

            Block topBlock = ResolveTopBlock(acc, maturity);
            if (topBlock == null) return false;

            BlockPos topPos = FindTopPos(acc, basePos);
            Block atTop = acc.GetBlock(topPos);
            if (!IsTopBlock(atTop) && !IsTrunkBlock(atTop)) return false;

            if (IsTopBlock(atTop) && ParseTopMaturity(atTop) == maturity) return false;

            acc.SetBlock(topBlock.BlockId, topPos);
            acc.MarkBlockDirty(topPos);
            return true;
        }

        public static FerntreeTopMaturity MaturityForAge(int ageYears, WildFerntreeEcology.Profile profile)
        {
            if (profile.YoungTopUntilYears > 0 && ageYears < profile.YoungTopUntilYears)
            {
                return FerntreeTopMaturity.Young;
            }

            if (profile.MediumTopUntilYears > 0 && ageYears < profile.MediumTopUntilYears)
            {
                return FerntreeTopMaturity.Medium;
            }

            return FerntreeTopMaturity.Old;
        }

        public static bool TryPlaceYoung(
            IBlockAccessor acc,
            BlockPos basePos,
            int segmentCount,
            System.Random rand)
        {
            if (acc == null || basePos == null) return false;
            if (segmentCount < 2) segmentCount = 2;

            Block trunk = acc.GetBlock(new AssetLocation("game:" + TrunkPath));
            Block top = ResolveTopBlock(acc, FerntreeTopMaturity.Young);
            Block foliage = acc.GetBlock(new AssetLocation("game:" + FoliagePath));
            if (trunk == null || trunk.Id == 0 || top == null || top.Id == 0) return false;

            int topY = basePos.Y + segmentCount - 1;
            for (int y = basePos.Y; y <= topY; y++)
            {
                var p = new BlockPos(basePos.X, y, basePos.Z, basePos.dimension);
                if (!acc.IsValidPos(p)) return false;
                if (acc.GetBlock(p).Id != 0) return false;
            }

            for (int y = basePos.Y; y < topY; y++)
            {
                var p = new BlockPos(basePos.X, y, basePos.Z, basePos.dimension);
                acc.SetBlock(trunk.BlockId, p);
                acc.MarkBlockDirty(p);
            }

            var topPos = new BlockPos(basePos.X, topY, basePos.Z, basePos.dimension);
            acc.SetBlock(top.BlockId, topPos);
            acc.MarkBlockDirty(topPos);

            if (foliage != null && foliage.Id != 0 && rand != null)
            {
                int foliageCount = 2 + rand.Next(3);
                for (int n = 0; n < foliageCount; n++)
                {
                    int y = basePos.Y + rand.Next(segmentCount);
                    int dir = rand.Next(4);
                    int dx = dir == 0 ? 1 : dir == 1 ? -1 : 0;
                    int dz = dir == 2 ? 1 : dir == 3 ? -1 : 0;
                    var fp = new BlockPos(basePos.X + dx, y, basePos.Z + dz, basePos.dimension);
                    if (!acc.IsValidPos(fp)) continue;
                    if (acc.GetBlock(fp).Id != 0) continue;
                    acc.SetBlock(foliage.BlockId, fp);
                    acc.MarkBlockDirty(fp);
                }
            }

            return IsTrunkBlock(acc.GetBlock(basePos));
        }

        public static bool TryGrowOneSegment(IBlockAccessor acc, BlockPos basePos, int maxSegments)
        {
            if (acc == null || basePos == null || maxSegments <= 0) return false;

            int segments = MeasureTrunkSegmentCount(acc, basePos);
            if (segments >= maxSegments) return false;

            Block trunk = acc.GetBlock(new AssetLocation("game:" + TrunkPath));
            if (trunk == null || trunk.Id == 0) return false;

            BlockPos topPos = FindTopPos(acc, basePos);
            Block topBlock = acc.GetBlock(topPos);
            FerntreeTopMaturity maturity = ParseTopMaturity(topBlock);
            if (maturity == FerntreeTopMaturity.None && IsTrunkBlock(topBlock))
            {
                maturity = FerntreeTopMaturity.Young;
            }

            BlockPos newTopPos = topPos.UpCopy();
            if (!acc.IsValidPos(newTopPos)) return false;
            if (acc.GetBlock(newTopPos).Id != 0) return false;

            if (IsTopBlock(topBlock))
            {
                acc.SetBlock(trunk.BlockId, topPos);
                acc.MarkBlockDirty(topPos);
            }

            Block newTop = ResolveTopBlock(acc, maturity);
            if (newTop == null) return false;

            acc.SetBlock(newTop.BlockId, newTopPos);
            acc.MarkBlockDirty(newTopPos);
            return true;
        }

        public static bool HasFerntreeInColumn(IBlockAccessor acc, BlockPos near, int below = 4, int above = 32)
        {
            if (acc == null || near == null) return false;

            for (int dy = -below; dy <= above; dy++)
            {
                var p = new BlockPos(near.X, near.Y + dy, near.Z, near.dimension);
                if (!acc.IsValidPos(p)) continue;
                if (IsTrunkBlock(acc.GetBlock(p))) return true;
            }

            return false;
        }
    }
}
