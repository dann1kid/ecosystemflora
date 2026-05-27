using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    public class EcologyInspectServerSystem : ModSystem
    {
        ICoreServerAPI sapi;
        IServerNetworkChannel channel;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel(EcologyInspectChannel.Name)
                .RegisterMessageType<EcologyInspectRequestPacket>()
                .RegisterMessageType<EcologyInspectReportPacket>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            channel = api.Network.GetChannel(EcologyInspectChannel.Name)
                .SetMessageHandler<EcologyInspectRequestPacket>(OnInspectRequest);
        }

        void OnInspectRequest(IPlayer player, EcologyInspectRequestPacket request)
        {
            if (sapi == null || player == null || request == null) return;

            var pos = new BlockPos(request.X, request.Y, request.Z);

            if (!EcologyInspectService.TryBuildReport(
                sapi,
                player,
                pos,
                out EcologyInspectReportPacket report,
                out string errorKey))
            {
                if (!string.IsNullOrEmpty(errorKey))
                {
                    sapi.SendMessage(
                        player,
                        GlobalConstants.GeneralChatGroup,
                        Lang.Get(errorKey),
                        EnumChatType.Notification);
                }

                return;
            }

            if (player is IServerPlayer sp)
            {
                channel.SendPacket(report, sp);
            }
        }
    }
}
