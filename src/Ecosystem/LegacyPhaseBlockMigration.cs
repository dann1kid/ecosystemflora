using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// Remaps fern phase blocks saved with mistaken <c>-free</c> cover suffix (variant-group migration)
    /// back to legacy codes without that suffix.
    /// </summary>
    internal static class LegacyPhaseBlockMigration
    {
        internal const int RemapDelayMs = 250;

        public static void ScheduleRemapColumn(ICoreAPI api, Vec2i chunkCoord)
        {
            if (api?.World?.BlockAccessor == null) return;

            Vec2i coord = chunkCoord.Copy();
            api.Event.RegisterCallback(_ => RemapColumnAt(api, coord), RemapDelayMs);
        }

        static void RemapColumnAt(ICoreAPI api, Vec2i chunkCoord)
        {
            IBlockAccessor acc = api.World?.BlockAccessor;
            if (acc == null) return;

            int cs = GlobalConstants.ChunkSize;
            int baseX = chunkCoord.X * cs;
            int baseZ = chunkCoord.Y * cs;
            int maxY = acc.MapSizeY;

            for (int y = 0; y < maxY; y++)
            {
                for (int lx = 0; lx < cs; lx++)
                {
                    for (int lz = 0; lz < cs; lz++)
                    {
                        var pos = new BlockPos(baseX + lx, y, baseZ + lz);
                        TryRemapAt(acc, pos);
                    }
                }
            }
        }

        static void TryRemapAt(IBlockAccessor acc, BlockPos pos)
        {
            Block block = acc.GetBlock(pos);
            AssetLocation targetCode = ResolveRemapTarget(block?.Code);
            if (targetCode == null) return;

            Block target = acc.GetBlock(targetCode);
            if (target == null || target.Id == 0 || target.Id == block.Id) return;

            acc.SetBlock(target.Id, pos);
        }

        internal static AssetLocation ResolveRemapTarget(AssetLocation code)
        {
            if (code?.Domain != "ecosystemflora" || code.Path == null) return null;
            if (!code.Path.StartsWith("fernphase-")) return null;
            if (!code.Path.EndsWith(JuvenileBlockNaming.FreeSuffix, System.StringComparison.Ordinal)) return null;

            string withoutFree = code.Path.Substring(0, code.Path.Length - JuvenileBlockNaming.FreeSuffix.Length);
            if (!withoutFree.EndsWith("-dormant") && !withoutFree.EndsWith("-dieback")) return null;

            return new AssetLocation(code.Domain, withoutFree);
        }
    }
}
