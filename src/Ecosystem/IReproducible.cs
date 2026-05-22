using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace WildFarming.Ecosystem
{
    /// <summary>Mature plant registered for ecosystem propagation.</summary>
    public interface IReproducible
    {
        BlockPos Origin { get; }
        AssetLocation JuvenileBlockCode { get; }
        PlantRequirements Requirements { get; }
        double NextAttemptHours { get; }
    }
}
