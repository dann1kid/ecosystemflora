using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class RegistrationColumnScan
    {
        public static BlockPos GetTreeTrunkBase(IRegistrationColumnView view, BlockPos logPos)
        {
            if (view == null || logPos == null) return logPos?.Copy();

            var scan = logPos.Copy();
            Block at = view.GetBlock(scan.X, scan.Y, scan.Z);
            string wood = PlantCodeHelper.GetTreeWood(at?.Code);
            if (wood == null) return logPos.Copy();

            while (true)
            {
                int bx = scan.X;
                int by = scan.Y - 1;
                int bz = scan.Z;
                if (!view.IsValidPos(bx, by, bz)) break;

                Block belowBlock = view.GetBlock(bx, by, bz);
                if (!PlantCodeHelper.IsTreeLogGrownBlock(belowBlock)) break;
                if (PlantCodeHelper.GetTreeWood(belowBlock) != wood) break;
                scan.Set(bx, by, bz);
            }

            return scan;
        }

        public static BlockPos GetFerntreeTrunkBase(IRegistrationColumnView view, BlockPos pos)
        {
            if (view == null || pos == null) return pos?.Copy();

            var scan = pos.Copy();
            Block at = view.GetBlock(scan.X, scan.Y, scan.Z);
            if (!FerntreeStructure.IsFerntreeBlock(at))
            {
                return pos.Copy();
            }

            if (FerntreeStructure.IsFoliageBlock(at))
            {
                for (int dy = 0; dy <= 24; dy++)
                {
                    int bx = scan.X;
                    int by = scan.Y - dy;
                    int bz = scan.Z;
                    if (!view.IsValidPos(bx, by, bz)) break;

                    Block belowBlock = view.GetBlock(bx, by, bz);
                    if (FerntreeStructure.IsTrunkBlock(belowBlock) || FerntreeStructure.IsTopBlock(belowBlock))
                    {
                        scan.Set(bx, by, bz);
                        break;
                    }
                }
            }

            while (true)
            {
                int bx = scan.X;
                int by = scan.Y - 1;
                int bz = scan.Z;
                if (!view.IsValidPos(bx, by, bz)) break;
                if (!FerntreeStructure.IsTrunkBlock(view.GetBlock(bx, by, bz))) break;
                scan.Set(bx, by, bz);
            }

            return scan;
        }
    }
}
