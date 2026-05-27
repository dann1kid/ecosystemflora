using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
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

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Title bar ~40px; fixed body height for richtext (required for FitToChildren parent chain).
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 440, 340);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);

            SingleComposer = capi.Gui
                .CreateCompo("ecosystemflora-inspect", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(BuildTitle(), OnTitleBarClose)
                .AddRichtext(BuildBody(report), CairoFont.WhiteDetailText(), textBounds, "inspect-body")
                .Compose();
        }

        string BuildTitle()
        {
            return Lang.Get("ecosystemflora:inspect-title", report.Species ?? "?");
        }

        static string BuildBody(EcologyInspectReportPacket report)
        {
            var sb = new StringBuilder();
            sb.Append("<strong>").Append(Lang.Get("ecosystemflora:inspect-pos"))
                .Append("</strong> ").Append(report.X).Append(", ")
                .Append(report.Y).Append(", ").Append(report.Z).AppendLine("<br/>");

            if (report.Lines != null)
            {
                for (int i = 0; i < report.Lines.Length; i++)
                {
                    sb.Append(report.Lines[i]).Append("<br/>");
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
