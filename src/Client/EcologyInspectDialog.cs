using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using WildFarming.Ecosystem;
using WildFarming.Network;

namespace WildFarming.Client
{
    public class EcologyInspectDialog : GuiDialog
    {
        EcologyInspectReportPacket report;

        public EcologyInspectDialog(ICoreClientAPI capi) : base(capi)
        {
        }

        public void ShowReport(EcologyInspectReportPacket packet)
        {
            if (packet == null) return;

            report = packet;
            if (IsOpened())
            {
                TryClose();
            }

            TryOpen();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (!TryComposeDialog())
            {
                TryClose();
            }
        }

        bool TryComposeDialog()
        {
            if (report == null) return false;

            // Never assign SingleComposer = null — VS setter NREs on value.OnFocusChanged.
            SingleComposer?.Dispose();

            const double width = 480;
            const double height = 420;
            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(width, height)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, width, height);
            ElementBounds textBounds = ElementBounds.Fixed(
                pad,
                titleH + pad,
                width - pad * 2,
                height - titleH - pad * 2);

            try
            {
                SingleComposer = capi.Gui
                    .CreateCompo("ecosystemflora-inspect", dialogBounds)
                    .AddShadedDialogBG(bgBounds, true)
                    .AddDialogTitleBar(BuildTitle(), OnTitleBarClose)
                    .AddRichtext(BuildBody(report), CairoFont.WhiteDetailText(), textBounds, "inspect-body")
                    .Compose();

                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Ecology inspect dialog failed: {0}", ex);
                capi.ShowChatMessage(Lang.Get("ecosystemflora:inspect-error-dialog"));
                return false;
            }
        }

        string BuildTitle()
        {
            string species = EcologyInspectLineFormat.FormatSpeciesEcho(report.Species ?? string.Empty);
            return Lang.Get("ecosystemflora:inspect-title", species);
        }

        static string BuildBody(EcologyInspectReportPacket report)
        {
            var sb = new StringBuilder();
            sb.Append("<strong>").Append(Lang.Get("ecosystemflora:inspect-pos"))
                .Append("</strong> ").Append(report.X).Append(", ")
                .Append(report.Y).Append(", ").Append(report.Z).AppendLine("<br/>");

            InspectLineLite[] lines = report.InspectLines;
            if (lines != null)
            {
                foreach (InspectLineLite line in lines)
                {
                    sb.Append(EcologyInspectLineFormat.FormatInspectLine(line)).Append("<br/>");
                }
            }

            if (sb.Length == 0)
            {
                sb.Append(Lang.Get("ecosystemflora:inspect-empty"));
            }

            return sb.ToString();
        }

        void OnTitleBarClose()
        {
            TryClose();
        }

        public override string ToggleKeyCombinationCode => null;
    }
}
