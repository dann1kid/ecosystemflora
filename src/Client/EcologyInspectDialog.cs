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
            ComposeDialog();
        }

        void ComposeDialog()
        {
            if (report == null) return;

            SingleComposer?.Dispose();

            const double width = 480;
            const double height = 420;

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(width, height)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds textBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 440, 340);

            SingleComposer = capi.Gui
                .CreateCompo("ecosystemflora-inspect", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(BuildTitle(), OnTitleBarClose)
                .AddRichtext(BuildBody(report), CairoFont.WhiteDetailText(), textBounds, "inspect-body")
                .Compose();
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

            return sb.ToString();
        }

        void OnTitleBarClose()
        {
            TryClose();
        }

        public override string ToggleKeyCombinationCode => null;
    }
}
