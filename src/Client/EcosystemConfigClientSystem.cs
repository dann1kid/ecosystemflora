using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;
using WildFarming.Network;

namespace WildFarming.Client
{
    public class EcosystemConfigClientSystem : ModSystem
    {
        public const string HotkeyCode = "ecosystemflora-config";
        public const string CommandCode = "ecoconfig";

        ICoreClientAPI capi;
        IClientNetworkChannel channel;
        EcosystemConfigDialog dialog;
        bool awaitingServerSave;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel(EcosystemConfigChannel.Name)
                .RegisterMessageType<EcosystemConfigSyncRequestPacket>()
                .RegisterMessageType<EcosystemConfigSyncResponsePacket>()
                .RegisterMessageType<EcosystemConfigSaveRequestPacket>()
                .RegisterMessageType<EcosystemConfigSaveResponsePacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;

            channel = api.Network.GetChannel(EcosystemConfigChannel.Name)
                .SetMessageHandler<EcosystemConfigSyncResponsePacket>(OnSyncResponse)
                .SetMessageHandler<EcosystemConfigSaveResponsePacket>(OnSaveResponse);

            api.Input.RegisterHotKey(
                HotkeyCode,
                Lang.Get("ecosystemflora:config-hotkey-name"),
                GlKeys.U,
                HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler(HotkeyCode, _ =>
            {
                OpenConfigDialog();
                return true;
            });

            api.ChatCommands
                .GetOrCreate(CommandCode)
                .WithDescription(Lang.Get("ecosystemflora:config-command-desc"))
                .HandleWith(_ =>
                {
                    OpenConfigDialog();
                    return TextCommandResult.Success();
                });
        }

        void OpenConfigDialog()
        {
            dialog?.TryClose();
            dialog = new EcosystemConfigDialog(capi, EcosystemConfig.Loaded);
            dialog.OnApplyRequested += ApplyWorkingCopy;
            capi.Gui.RegisterDialog(dialog);
            dialog.TryOpen();
            channel.SendPacket(new EcosystemConfigSyncRequestPacket());
        }

        void OnSyncResponse(EcosystemConfigSyncResponsePacket packet)
        {
            if (dialog == null || packet == null) return;

            if (!string.IsNullOrEmpty(packet.ErrorLangKey))
            {
                capi.ShowChatMessage(Lang.Get(packet.ErrorLangKey));
                return;
            }

            if (string.IsNullOrWhiteSpace(packet.ConfigJson)) return;

            try
            {
                EcosystemConfig serverCfg = EcosystemConfigCopier.FromJson(packet.ConfigJson);
                dialog.MergeServerConfig(serverCfg);
            }
            catch
            {
                capi.ShowChatMessage(Lang.Get("ecosystemflora:config-error-parse"));
            }
        }

        void ApplyWorkingCopy(EcosystemConfig working)
        {
            if (working == null) return;

            if (!EcosystemConfigValidator.TryValidate(working, out _))
            {
                capi.ShowChatMessage(Lang.Get("ecosystemflora:config-ui-error-invalid"));
                return;
            }

            if (CanSaveOnLocalServer())
            {
                var serverApi = capi.World.Api as ICoreAPI;
                if (serverApi == null
                    || !EcosystemConfigSaveService.TryApplyAndPersist(serverApi, working, out _))
                {
                    capi.ShowChatMessage(Lang.Get("ecosystemflora:config-error-save"));
                    return;
                }

                capi.StoreModConfig(EcosystemConfig.Loaded, EcosystemConfig.ConfigFileName);
                FinishApplySuccess();
                return;
            }

            SaveClientScope(working);

            awaitingServerSave = true;
            channel.SendPacket(new EcosystemConfigSaveRequestPacket
            {
                ConfigJson = EcosystemConfigCopier.ToJson(working),
            });
        }

        void OnSaveResponse(EcosystemConfigSaveResponsePacket packet)
        {
            if (!awaitingServerSave) return;
            awaitingServerSave = false;

            if (packet == null || !packet.Ok)
            {
                string key = packet?.ErrorLangKey ?? "ecosystemflora:config-error-save";
                capi.ShowChatMessage(Lang.Get(key));
                return;
            }

            if (!string.IsNullOrWhiteSpace(packet.ConfigJson))
            {
                try
                {
                    EcosystemConfig serverCfg = EcosystemConfigCopier.FromJson(packet.ConfigJson);
                    EcosystemConfigCopier.CopyFields(serverCfg, EcosystemConfig.Loaded);
                    capi.StoreModConfig(EcosystemConfig.Loaded, EcosystemConfig.ConfigFileName);
                }
                catch
                {
                    // Client-only values were already saved locally.
                }
            }

            FinishApplySuccess();
        }

        void SaveClientScope(EcosystemConfig working)
        {
            EcosystemConfigCopier.CopyScope(working, EcosystemConfig.Loaded, ConfigFieldScope.Client);
            capi.StoreModConfig(EcosystemConfig.Loaded, EcosystemConfig.ConfigFileName);
        }

        bool CanSaveOnLocalServer()
        {
            return capi.World.Api is ICoreServerAPI;
        }

        void FinishApplySuccess()
        {
            capi.ShowChatMessage(Lang.Get("ecosystemflora:config-ui-saved"));
            dialog?.TryClose();
        }
    }
}
