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
        public const string SetupWizardCommandCode = "ecosetup";
        public const string ConfigCommandCode = "ecoconfig";

        ICoreServerAPI sapi;
        IServerNetworkChannel channel;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel(EcosystemConfigChannel.Name)
                .RegisterMessageType<EcosystemConfigSyncRequestPacket>()
                .RegisterMessageType<EcosystemConfigSyncResponsePacket>()
                .RegisterMessageType<EcosystemConfigSaveRequestPacket>()
                .RegisterMessageType<EcosystemConfigSaveResponsePacket>()
                .RegisterMessageType<EcosystemOpenSetupWizardPacket>()
                .RegisterMessageType<EcosystemOpenConfigDialogPacket>();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            channel = api.Network.GetChannel(EcosystemConfigChannel.Name)
                .SetMessageHandler<EcosystemConfigSyncRequestPacket>(OnSyncRequest)
                .SetMessageHandler<EcosystemConfigSaveRequestPacket>(OnSaveRequest);

            EcosystemWorldConfigStore.OnWorldConfigReady += PushConfigToOnlinePlayers;
            if (EcosystemWorldConfigStore.IsWorldConfigReady)
            {
                PushConfigToOnlinePlayers();
            }

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

            // Chat commands are resolved on the server — client-only registration is invisible in /.
            api.ChatCommands
                .GetOrCreate(SetupWizardCommandCode)
                .WithDescription(Lang.Get("ecosystemflora:setup-wizard-command-desc"))
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args =>
                {
                    if (args.Caller?.Player is not IServerPlayer player)
                    {
                        return TextCommandResult.Error(Lang.Get("ecosystemflora:config-error-noprivilege"));
                    }

                    channel.SendPacket(new EcosystemOpenSetupWizardPacket(), player);
                    return TextCommandResult.Success(Lang.Get("ecosystemflora:setup-wizard-command-opened"));
                });

            api.ChatCommands
                .GetOrCreate(ConfigCommandCode)
                .WithDescription(Lang.Get("ecosystemflora:config-command-desc"))
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(args =>
                {
                    if (args.Caller?.Player is not IServerPlayer player)
                    {
                        return TextCommandResult.Error(Lang.Get("ecosystemflora:config-error-noprivilege"));
                    }

                    channel.SendPacket(new EcosystemOpenConfigDialogPacket(), player);
                    return TextCommandResult.Success(Lang.Get("ecosystemflora:config-command-opened"));
                });
        }

        public override void Dispose()
        {
            EcosystemWorldConfigStore.OnWorldConfigReady -= PushConfigToOnlinePlayers;
            sapi = null;
            base.Dispose();
        }

        void PushConfigToOnlinePlayers()
        {
            if (sapi?.World?.AllOnlinePlayers == null || channel == null) return;
            if (!EcosystemWorldConfigStore.IsWorldConfigReady) return;

            foreach (IPlayer player in sapi.World.AllOnlinePlayers)
            {
                if (player is IServerPlayer sp)
                {
                    SendSyncResponse(sp);
                }
            }
        }

        void OnSyncRequest(IServerPlayer player, EcosystemConfigSyncRequestPacket packet)
        {
            if (player == null) return;
            SendSyncResponse(player);
        }

        void SendSyncResponse(IServerPlayer player)
        {
            if (player == null || channel == null) return;

            if (!EcosystemWorldConfigStore.IsWorldConfigReady)
            {
                channel.SendPacket(new EcosystemConfigSyncResponsePacket
                {
                    WorldConfigReady = false,
                    ConfigJson = null,
                    CanEditConfig = player.HasPrivilege(Privilege.controlserver),
                }, player);
                return;
            }

            channel.SendPacket(new EcosystemConfigSyncResponsePacket
            {
                WorldConfigReady = true,
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
