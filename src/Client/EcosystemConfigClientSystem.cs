using System;
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
        public const string AutoTuneCommandCode = "ecoautotune";

        ICoreClientAPI capi;
        IClientNetworkChannel channel;
        EcosystemConfigDialog dialog;
        EcosystemSetupWizardDialog wizard;
        bool awaitingServerSave;
        bool wizardPromptPending = true;
        bool lastSyncCanEdit;

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

            api.ChatCommands
                .GetOrCreate(AutoTuneCommandCode)
                .WithDescription(Lang.Get("ecosystemflora:autotune-command-desc"))
                .HandleWith(_ =>
                {
                    RunAutoTuneAndSave();
                    return TextCommandResult.Success();
                });

            api.Event.LevelFinalize += () =>
            {
                wizardPromptPending = true;
                channel.SendPacket(new EcosystemConfigSyncRequestPacket());
            };
        }

        void RunAutoTuneAndSave()
        {
            if (!lastSyncCanEdit && !CanSaveOnLocalServer())
            {
                // Still try — server will reject without privilege.
            }

            EcosystemConfig working = EcosystemConfigCopier.Clone(EcosystemConfig.Loaded);
            EcosystemPerfCalibrator.CalibrationResult result =
                EcosystemPerfCalibrator.RunAndApply(working);
            working.BalancePreset = EcosystemBalancePresets.Custom;

            capi.ShowChatMessage(Lang.Get(
                "ecosystemflora:setup-wizard-autotune-result",
                result.Tier.ToString(),
                result.OpsPerMs.ToString("0.0"),
                result.ElapsedMs));

            ApplyWorkingCopy(working, closeConfigDialog: false);

            if (dialog != null && dialog.IsOpened())
            {
                EcosystemConfigCopier.CopyScope(working, dialog.WorkingCopy, ConfigFieldScope.Server);
                dialog.RequestRecompose();
            }
        }

        public void OpenSetupWizard(bool force = false)
        {
            if (wizard != null && wizard.IsOpened()) return;

            wizard?.TryClose();
            wizard = new EcosystemSetupWizardDialog(capi, EcosystemConfig.Loaded);
            wizard.OnFinished += ApplyWorkingCopyQuiet;
            capi.Gui.RegisterDialog(wizard);
            wizard.TryOpen();
            channel.SendPacket(new EcosystemConfigSyncRequestPacket());
            _ = force;
        }

        void OpenConfigDialog()
        {
            dialog?.TryClose();
            dialog = new EcosystemConfigDialog(capi, EcosystemConfig.Loaded);
            dialog.OnApplyRequested += ApplyWorkingCopy;
            dialog.OnSetupWizardRequested += () => OpenSetupWizard(force: true);
            dialog.OnAutoTuneRequested += OnAutoTuneFromConfig;
            capi.Gui.RegisterDialog(dialog);
            dialog.TryOpen();
            channel.SendPacket(new EcosystemConfigSyncRequestPacket());
        }

        void OnAutoTuneFromConfig()
        {
            if (dialog == null) return;
            EcosystemPerfCalibrator.CalibrationResult result =
                EcosystemPerfCalibrator.RunAndApply(dialog.WorkingCopy);
            dialog.WorkingCopy.BalancePreset = EcosystemBalancePresets.Custom;
            dialog.RequestRecompose();
            capi.ShowChatMessage(Lang.Get(
                "ecosystemflora:setup-wizard-autotune-result",
                result.Tier.ToString(),
                result.OpsPerMs.ToString("0.0"),
                result.ElapsedMs));
        }

        void OnSyncResponse(EcosystemConfigSyncResponsePacket packet)
        {
            if (packet == null) return;

            if (!string.IsNullOrEmpty(packet.ErrorLangKey))
            {
                capi.ShowChatMessage(Lang.Get(packet.ErrorLangKey));
                return;
            }

            if (string.IsNullOrWhiteSpace(packet.ConfigJson)) return;

            EcosystemConfig serverCfg;
            try
            {
                serverCfg = EcosystemConfigCopier.FromJson(packet.ConfigJson);
            }
            catch
            {
                capi.ShowChatMessage(Lang.Get("ecosystemflora:config-error-parse"));
                return;
            }

            lastSyncCanEdit = packet.CanEditConfig || CanSaveOnLocalServer();
            EcosystemConfigCopier.CopyScope(serverCfg, EcosystemConfig.Loaded, ConfigFieldScope.Server);

            if (dialog != null && dialog.IsOpened())
            {
                dialog.MergeServerConfig(serverCfg);
            }

            if (wizard != null && wizard.IsOpened())
            {
                EcosystemConfigCopier.CopyScope(serverCfg, wizard.WorkingCopy, ConfigFieldScope.Server);
            }
            else if (wizardPromptPending && !serverCfg.SetupWizardCompleted && lastSyncCanEdit)
            {
                wizardPromptPending = false;
                OpenSetupWizard();
            }
            else
            {
                wizardPromptPending = false;
            }
        }

        void ApplyWorkingCopyQuiet(EcosystemConfig working)
        {
            ApplyWorkingCopy(working, closeConfigDialog: false);
        }

        void ApplyWorkingCopy(EcosystemConfig working)
        {
            ApplyWorkingCopy(working, closeConfigDialog: true);
        }

        void ApplyWorkingCopy(EcosystemConfig working, bool closeConfigDialog)
        {
            if (working == null) return;

            EcosystemConfigValidator.NormalizeInPlace(working);

            if (CanSaveOnLocalServer())
            {
                var serverApi = capi.World.Api as ICoreAPI;
                if (serverApi == null
                    || !EcosystemConfigSaveService.TryApplyAndPersist(serverApi, working, out _))
                {
                    capi.ShowChatMessage(Lang.Get("ecosystemflora:config-error-save"));
                    return;
                }

                // Client-scope mirror only — server/world settings live in SaveGame.
                SaveClientScope(EcosystemConfig.Loaded);
                FinishApplySuccess(closeConfigDialog);
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
                    EcosystemConfigCopier.CopyScope(serverCfg, EcosystemConfig.Loaded, ConfigFieldScope.Server);
                    SaveClientScope(EcosystemConfig.Loaded);
                }
                catch
                {
                    // Client-only values were already saved locally.
                }
            }

            FinishApplySuccess(closeConfigDialog: true);
        }

        void SaveClientScope(EcosystemConfig working)
        {
            EcosystemConfigCopier.CopyScope(working, EcosystemConfig.Loaded, ConfigFieldScope.Client);
            // Global ModConfig is a template only — never carry this world's wizard completion into it.
            capi.StoreModConfig(
                EcosystemWorldConfigStore.CloneAsGlobalTemplate(EcosystemConfig.Loaded),
                EcosystemConfig.ConfigFileName);
        }

        bool CanSaveOnLocalServer()
        {
            return capi.World.Api is ICoreServerAPI;
        }

        void FinishApplySuccess(bool closeConfigDialog)
        {
            capi.ShowChatMessage(Lang.Get("ecosystemflora:config-ui-saved"));
            if (closeConfigDialog)
            {
                dialog?.TryClose();
            }
        }
    }
}
