using Vintagestory.API.Common;

namespace WildFarming.Ecosystem
{
    /// <summary>
    /// A block type that can register for wild reproduction (requirements + spread codes).
    /// </summary>
    public interface IEcosystemParticipant
    {
        AssetLocation BlockCode { get; }
        PlantRequirements Requirements { get; }
        AssetLocation SpreadBlockCode { get; }
        AssetLocation MatureBlockCode { get; }
    }
}
