using Vintagestory.API.Common;
using Vintagestory.API.Server;
using WildFarming.Ecosystem.Config;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    public class EcosystemConfigServerSystem : ModSystem
    {
        IServerNetworkChannel channel;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel(EcosystemConfigChannel.Name)
                .RegisterMessageType<EcosystemConfigSyncRequestPacket>()
                .RegisterMessageType<EcosystemConfigSyncResponsePacket>()
                .RegisterMessageType<EcosystemConfigSaveRequestPacket>()
                .RegisterMessageType<EcosystemConfigSaveResponsePacket>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            channel = api.Network.GetChannel(EcosystemConfigChannel.Name)
                .SetMessageHandler<EcosystemConfigSyncRequestPacket>(OnSyncRequest)
                .SetMessageHandler<EcosystemConfigSaveRequestPacket>(OnSaveRequest);
        }

        void OnSyncRequest(IServerPlayer player, EcosystemConfigSyncRequestPacket packet)
        {
            if (player == null) return;

            channel.SendPacket(new EcosystemConfigSyncResponsePacket
            {
                ConfigJson = EcosystemConfigCopier.ToJson(EcosystemConfig.Loaded),
            }, player);
        }

        void OnSaveRequest(IServerPlayer player, EcosystemConfigSaveRequestPacket packet)
        {
            if (player == null) return;

            if (!player.HasPrivilege(Privilege.controlserver))
            {
                SendSaveResponse(player, false, "ecosystemflora:config-error-noprivilege", null);
                return;
            }

            if (packet == null || string.IsNullOrWhiteSpace(packet.ConfigJson))
            {
                SendSaveResponse(player, false, "ecosystemflora:config-error-empty", null);
                return;
            }

            EcosystemConfig incoming;
            try
            {
                incoming = EcosystemConfigCopier.FromJson(packet.ConfigJson);
            }
            catch
            {
                SendSaveResponse(player, false, "ecosystemflora:config-error-parse", null);
                return;
            }

            if (!EcosystemConfigValidator.TryValidate(incoming, out _))
            {
                SendSaveResponse(player, false, "ecosystemflora:config-error-invalid", null);
                return;
            }

            if (!EcosystemConfigSaveService.TryApplyAndPersist(player.Entity.Api, incoming, out _))
            {
                SendSaveResponse(player, false, "ecosystemflora:config-error-save", null);
                return;
            }

            SendSaveResponse(
                player,
                true,
                null,
                EcosystemConfigCopier.ToJson(EcosystemConfig.Loaded));
        }

        void SendSaveResponse(IServerPlayer player, bool ok, string errorLangKey, string configJson)
        {
            channel.SendPacket(new EcosystemConfigSaveResponsePacket
            {
                Ok = ok,
                ErrorLangKey = errorLangKey,
                ConfigJson = configJson,
            }, player);
        }
    }
}
