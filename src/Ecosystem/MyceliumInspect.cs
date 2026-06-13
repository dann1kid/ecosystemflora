using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    internal static class MyceliumInspect
    {
        public static bool IsMushroomBlock(Block block)
        {
            if (block?.Code == null) return false;
            string path = block.Code.Path;
            return !string.IsNullOrEmpty(path)
                && path.StartsWith("mushroom-", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Client-safe: mushroom caps are inspected on the server (BE is server-side).</summary>
        public static bool IsInspectableTarget(Block block)
        {
            return IsMushroomBlock(block);
        }

        /// <summary>
        /// Client-safe heuristic: soil/log blocks where mycelium BE may live.
        /// Server validates with <see cref="TryGetAnchorContext"/>.
        /// </summary>
        public static bool IsPotentialGroundAnchor(Block block)
        {
            if (block == null || block.Id == 0) return false;
            if (WildSoilGroundRules.IsUnplantableGround(block)) return false;
            if (WildSoilGroundRules.IsFarmland(block)) return false;
            if (MyceliumEcology.IsTrunkAnchor(block)) return true;
            return WildSoilGroundRules.IsWildSpreadGround(block);
        }

        public static bool ShouldSendInspectRequest(Block block, bool myceliumEcologyEnabled)
        {
            if (IsMushroomBlock(block)) return true;
            return myceliumEcologyEnabled && IsPotentialGroundAnchor(block);
        }

        public static bool TryGetAnchorContext(
            IBlockAccessor acc,
            BlockPos pos,
            out BlockPos anchorPos,
            out AssetLocation mushroomCode,
            out PlantRequirements requirements)
        {
            anchorPos = null;
            mushroomCode = null;
            requirements = null;
            if (acc == null || pos == null) return false;

            BlockPos ground = pos;
            Block block = acc.GetBlock(pos);

            if (IsMushroomBlock(block))
            {
                ground = pos.DownCopy();
            }

            if (!WildSoilGroundRules.HasActiveMycelium(acc, ground)) return false;

            BlockEntity be = acc.GetBlockEntity(ground);
            if (!MyceliumAnchorReader.TryReadMushroomCode(be, out mushroomCode)) return false;

            Block anchorBlock = acc.GetBlock(ground);
            if (!MyceliumEcology.TryBuildRequirements(mushroomCode, anchorBlock, out requirements)) return false;

            anchorPos = ground;
            return true;
        }
    }
}
