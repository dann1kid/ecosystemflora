using Vintagestory.API.Common;

namespace WildFarming
{
    /// <summary>
    /// Legacy block entity kept only so existing saves can deserialize without errors.
    /// New blocks no longer receive this entityClass (patches removed).
    /// On load it removes itself from the chunk so the mod can be cleanly uninstalled.
    /// Plant discovery is handled entirely by <see cref="Ecosystem.ChunkFlowerScanner"/>.
    /// </summary>
    public class EcoSystemLifeBlockEntity : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side != EnumAppSide.Server) return;

            api.Event.RegisterCallback(_ =>
            {
                try { api.World.BlockAccessor.RemoveBlockEntity(Pos); }
                catch { /* chunk may already be unloaded */ }
            }, 50);
        }
    }
}
