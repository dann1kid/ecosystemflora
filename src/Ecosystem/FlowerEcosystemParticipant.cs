using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Vanilla game:flower-* with worldgen-aligned ecology requirements.</summary>
    public sealed class FlowerEcosystemParticipant : IEcosystemParticipant
    {
        public AssetLocation BlockCode { get; }
        public PlantRequirements Requirements { get; }
        public AssetLocation SpreadBlockCode { get; }
        public AssetLocation MatureBlockCode { get; }

        FlowerEcosystemParticipant(Block block, PlantRequirements requirements, AssetLocation spread, AssetLocation mature)
        {
            BlockCode = block.Code;
            Requirements = requirements;
            SpreadBlockCode = spread;
            MatureBlockCode = mature;
        }

        public static bool TryFromBlock(Block block, out IEcosystemParticipant participant)
        {
            participant = null;
            if (block?.Code == null || !PlantCodeHelper.IsVanillaEcologyPlant(block)) return false;
            if (block.Attributes != null && !block.Attributes["ecologyReproduce"].AsBool(true)) return false;

            AssetLocation spread = PlantCodeHelper.SpreadBlockCode(block);
            if (spread == null) return false;

            AssetLocation mature = PlantCodeHelper.MatureBlockLocation(block) ?? spread;
            participant = new FlowerEcosystemParticipant(block, PlantRequirements.FromBlock(block), spread, mature);
            return true;
        }
    }
}
