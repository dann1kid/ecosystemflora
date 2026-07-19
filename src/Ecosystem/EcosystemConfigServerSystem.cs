using System.Globalization;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WildFarming.Ecosystem.Config;
using WildFarming.Network;

namespace WildFarming.Ecosystem
{
    public class EcosystemConfigServerSystem : ModSystem
    {
        public const string AutoTuneCommandCode = "ecoautotune";

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

            api.ChatCommands
                .GetOrCreate(AutoTuneCommandCode)
                .WithDescription(Lang.Get("ecosystemflora:autotune-command-desc"))
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args =>
                {
                    EcosystemConfig cfg = EcosystemConfigCopier.Clone(EcosystemConfig.Loaded);
                    EcosystemPerfCalibrator.CalibrationResult result =
                        EcosystemPerfCalibrator.RunAndApply(cfg);
                    cfg.BalancePreset = EcosystemBalancePresets.Custom;

                    if (!EcosystemConfigSaveService.TryApplyAndPersist(api, cfg, out string errorCode))
                    {
                        return TextCommandResult.Error(
                            Lang.Get("ecosystemflora:config-error-save")
                            + (string.IsNullOrEmpty(errorCode) ? "" : " (" + errorCode + ")"));
                    }

                    string msg = Lang.Get(
                        "ecosystemflora:setup-wizard-autotune-result",
                        result.Tier.ToString(),
                        result.OpsPerMs.ToString("0.0", CultureInfo.InvariantCulture),
                        result.ElapsedMs);
                    api.Logger.Notification("[ecosystemflora] {0}", msg);
                    return TextCommandResult.Success(msg);
                });
        }

        void OnSyncRequest(IServerPlayer player, EcosystemConfigSyncRequestPacket packet)
        {
            if (player == null) return;

            channel.SendPacket(new EcosystemConfigSyncResponsePacket
            {
                ConfigJson = EcosystemConfigCopier.ToJson(EcosystemConfig.Loaded),
                CanEditConfig = player.HasPrivilege(Privilege.controlserver),
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
