using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>Registered ecology organism: vanilla paths or declared third-party <c>ecologyParticipant</c> blocks.</summary>
    public sealed class EcosystemParticipant : IEcosystemParticipant
    {
        public AssetLocation BlockCode { get; }
        public PlantRequirements Requirements { get; }
        public AssetLocation SpreadBlockCode { get; }
        public AssetLocation MatureBlockCode { get; }

        EcosystemParticipant(Block block, PlantRequirements requirements, AssetLocation spread, AssetLocation mature)
        {
            BlockCode = block.Code;
            Requirements = requirements;
            SpreadBlockCode = spread;
            MatureBlockCode = mature;
        }

        public static bool TryFromBlock(Block block, out IEcosystemParticipant participant)
        {
            participant = null;
            if (block?.Code == null) return false;
            if (block.Attributes != null && !block.Attributes["ecologyReproduce"].AsBool(true)) return false;

            string path = block.Code.Path;
            if (path != null && path.Contains("-harvested-")) return false;

            if (PlantCodeHelper.IsThirdPartyEcologyBlock(block))
            {
                AssetLocation spreadTp = PlantCodeHelper.SpreadBlockCode(block);
                if (spreadTp == null) return false;

                AssetLocation matureTp = PlantCodeHelper.MatureBlockLocation(block) ?? spreadTp;
                participant = new EcosystemParticipant(block, PlantRequirements.FromBlock(block), spreadTp, matureTp);
                return true;
            }

            if (!PlantCodeHelper.IsEcologySpreadParent(block)) return false;

            AssetLocation spread = PlantCodeHelper.SpreadBlockCode(block);
            if (spread == null) return false;

            AssetLocation mature = PlantCodeHelper.MatureBlockLocation(block) ?? spread;
            participant = new EcosystemParticipant(block, PlantRequirements.FromBlock(block), spread, mature);
            return true;
        }
    }
}
