using Vintagestory.API.Common;
using WildFarming.Ecosystem;

namespace WildFarming
{
    /// <summary>Server-side ecology on vanilla game plants (e.g. game:flower-*). No mod block replacement.</summary>
    public class EcosystemPlantBlockEntity : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;
            if (!EcologyAttributes.ReproduceEnabled(Block)) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            PlantRequirements requirements = PlantRequirements.FromBlock(Block);
            eco.RegisterReproducer(Pos, Block.Code, requirements, spawnBurst: false);
        }
    }
}
