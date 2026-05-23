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
            if (!EcosystemParticipant.TryFromBlock(Block, out IEcosystemParticipant participant)) return;

            EcosystemSystem eco = EcosystemSystem.Instance;
            if (eco == null) return;

            eco.RegisterReproducer(Pos, participant, spawnBurst: false);
        }
    }
}
