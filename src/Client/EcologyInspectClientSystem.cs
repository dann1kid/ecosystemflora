using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using WildFarming.Ecosystem;
using WildFarming.Network;

namespace WildFarming.Client
{
    public class EcologyInspectClientSystem : ModSystem
    {
        public const string HotkeyCode = "ecosystemflora-inspect";

        ICoreClientAPI capi;
        IClientNetworkChannel channel;
        EcologyInspectDialog dialog;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

        public override void Start(ICoreAPI api)
        {
            api.Network
                .RegisterChannel(EcologyInspectChannel.Name)
                .RegisterMessageType<EcologyInspectRequestPacket>()
                .RegisterMessageType<EcologyInspectReportPacket>();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            dialog = new EcologyInspectDialog(api);
            api.Gui.RegisterDialog(dialog);

            channel = api.Network.GetChannel(EcologyInspectChannel.Name)
                .SetMessageHandler<EcologyInspectReportPacket>(OnReport);

            api.Input.RegisterHotKey(
                HotkeyCode,
                Lang.Get("ecosystemflora:inspect-hotkey-name"),
                GlKeys.I,
                HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler(HotkeyCode, OnInspectHotkey);
        }

        bool OnInspectHotkey(KeyCombination comb)
        {
            if (!EcosystemConfig.Loaded.EnableEcologyInspect) return true;

            BlockSelection sel = capi.World.Player?.CurrentBlockSelection;
            if (sel?.Position == null)
            {
                capi.ShowChatMessage(Lang.Get("ecosystemflora:inspect-error-notarget"));
                return true;
            }

            Block block = capi.World.BlockAccessor.GetBlock(sel.Position);
            if (block == null || string.IsNullOrEmpty(PlantCodeHelper.GetEcologySpecies(block.Code)))
            {
                capi.ShowChatMessage(Lang.Get("ecosystemflora:inspect-error-noplant"));
                return true;
            }

            channel.SendPacket(new EcologyInspectRequestPacket
            {
                X = sel.Position.X,
                Y = sel.Position.Y,
                Z = sel.Position.Z,
            });

            return true;
        }

        void OnReport(EcologyInspectReportPacket report)
        {
            if (report == null) return;

            if (!string.IsNullOrEmpty(report.ErrorLangKey))
            {
                capi.ShowChatMessage(Lang.Get(report.ErrorLangKey));
                return;
            }

            dialog.ShowReport(report);
        }
    }
}
