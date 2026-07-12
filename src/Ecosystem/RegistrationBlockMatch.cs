using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Whether a live block still matches a deferred registration snapshot.</summary>
    internal static class RegistrationBlockMatch
    {
        public static bool MatchesSnapshot(Block block, AssetLocation snapshotCode)
        {
            if (block == null || block.BlockId == 0) return false;
            if (snapshotCode == null || block.Code == null) return true;
            if (block.Code.Equals(snapshotCode)) return true;
            return PlantCodeHelper.SameEcologySpecies(block.Code, snapshotCode);
        }
    }
}
