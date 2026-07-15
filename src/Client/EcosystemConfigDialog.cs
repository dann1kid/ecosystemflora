using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;

namespace WildFarming.Client
{
    public class EcosystemConfigDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => EcosystemConfigClientSystem.HotkeyCode;

        const double DialogWidth = 540;
        const double DialogHeight = 580;
        const double RowGap = 6;
        const double TextPadPx = 2;
        const double ControlHeight = 25;
        const double ControlWidth = 118;
        const double HeaderBlockHeight = 88;
        const double FooterBlockHeight = 44;
        const double FooterGap = 12;

        readonly TextDrawUtil textMeasure = new TextDrawUtil();
        readonly CairoFont helpFont = CairoFont.WhiteSmallText();

        readonly struct FieldRowLayout
        {
            public FieldRowLayout(EcosystemConfigFieldDescriptor field, double height)
            {
                Field = field;
                Height = height;
            }

            public EcosystemConfigFieldDescriptor Field { get; }
            public double Height { get; }
        }

        readonly EcosystemConfig working;
        string selectedCategory = "master";
        int fieldPage;
        FieldRowLayout[] currentPageRows = Array.Empty<FieldRowLayout>();
        int currentPageCount = 1;

        bool pendingRecompose;

        public EcosystemConfig WorkingCopy => working;

        public EcosystemConfigDialog(ICoreClientAPI capi, EcosystemConfig source) : base(capi)
        {
            working = EcosystemConfigCopier.Clone(source ?? EcosystemConfig.Loaded);
            if (string.IsNullOrWhiteSpace(working.BalancePreset))
            {
                working.BalancePreset = EcosystemBalancePresets.Natural;
            }
        }

        public void MergeServerConfig(EcosystemConfig serverCfg)
        {
            if (serverCfg == null) return;
            EcosystemConfigCopier.CopyScope(serverCfg, working, ConfigFieldScope.Server);
            RequestRecompose();
        }

        public void RequestRecompose()
        {
            pendingRecompose = true;
            if (!IsOpened()) return;

            pendingRecompose = false;
            if (!TryComposeDialog())
            {
                TryClose();
            }
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (!TryComposeDialog())
            {
                TryClose();
            }
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
            if (!pendingRecompose) return;

            pendingRecompose = false;
            if (!TryComposeDialog())
            {
                TryClose();
            }
        }

        public bool TryComposeDialog()
        {
            SingleComposer?.Dispose();

            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double contentTop = titleH + pad + HeaderBlockHeight;
            double footerTop = DialogHeight - pad - FooterBlockHeight;
            double contentHeight = footerTop - FooterGap - contentTop;
            double contentWidth = DialogWidth - pad * 2;
            currentPageRows = BuildPageRows(contentWidth, contentHeight, out currentPageCount);

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(DialogWidth, DialogHeight)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, DialogWidth, DialogHeight);

            var composer = capi.Gui
                .CreateCompo("ecosystemflora-config", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(L("config-ui-title"), OnTitleBarClose);

            AddHeader(composer, pad, titleH);
            AddFieldRows(composer, pad, contentTop, contentHeight, contentWidth);
            AddFooter(composer, pad, footerTop, FooterBlockHeight);

            try
            {
                SingleComposer = composer.Compose();
                ApplyHoverTextAutoWidth();
                ApplyControlValues();
                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Config dialog failed: {0}", ex);
                capi.ShowChatMessage(L("config-ui-error-compose"));
                return false;
            }
        }

        void AddHeader(GuiComposer composer, double pad, double titleH)
        {
            double y = titleH + pad;
            double labelW = 110;
            double controlW = DialogWidth - pad * 2 - labelW - 8;

            composer.AddStaticText(
                L("config-ui-preset"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, labelW, 25));

            string[] presetCodes = EcosystemConfigSchema.PresetCodes;
            string[] presetLabels = presetCodes.Select(PresetLabel).ToArray();
            int presetIndex = Array.FindIndex(
                presetCodes,
                p => p.Equals(working.BalancePreset, StringComparison.OrdinalIgnoreCase));
            if (presetIndex < 0) presetIndex = 0;

            composer.AddDropDown(
                presetCodes,
                presetLabels,
                presetIndex,
                OnPresetSelected,
                ElementBounds.Fixed(pad + labelW, y, controlW, 25),
                "cfg-preset");

            composer.AddAutoSizeHoverText(
                string.Format(CultureInfo.InvariantCulture, L("config-ui-preset-desc"), nameof(EcosystemConfig.BalancePreset)),
                CairoFont.WhiteSmallText(),
                380,
                ElementBounds.Fixed(pad, y, labelW + controlW, 25).FlatCopy(),
                "cfg-preset-tip");

            y += 28;

            composer.AddStaticText(
                L("config-ui-category"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, labelW, 25));

            string[] categories = EcosystemConfigSchema.CategoryOrder;
            string[] categoryLabels = categories.Select(CategoryLabel).ToArray();
            int catIndex = Array.IndexOf(categories, selectedCategory);
            if (catIndex < 0) catIndex = 0;

            composer.AddDropDown(
                categories,
                categoryLabels,
                catIndex,
                OnCategorySelected,
                ElementBounds.Fixed(pad + labelW, y, controlW, 25),
                "cfg-category");

            composer.AddAutoSizeHoverText(
                L("config-ui-category-desc"),
                CairoFont.WhiteSmallText(),
                380,
                ElementBounds.Fixed(pad, y, labelW + controlW, 25).FlatCopy(),
                "cfg-category-tip");

            y += 26;

            string pageText = string.Format(
                CultureInfo.InvariantCulture,
                L("config-ui-page"),
                fieldPage + 1,
                currentPageCount);

            composer.AddStaticText(
                pageText,
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, DialogWidth - pad * 2, 20));
        }

        void AddFieldRows(GuiComposer composer, double pad, double top, double maxHeight, double contentWidth)
        {
            double y = top;

            for (int i = 0; i < currentPageRows.Length; i++)
            {
                FieldRowLayout row = currentPageRows[i];
                if (y + row.Height > top + maxHeight + 0.5) break;

                AddFieldRow(composer, row, pad, y, contentWidth);
                y += row.Height;
            }

            if (currentPageRows.Length == 0)
            {
                composer.AddStaticText(
                    L("config-ui-empty-category"),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(pad, top + 12, contentWidth, 30));
            }
        }

        FieldRowLayout[] BuildPageRows(double contentWidth, double maxHeight, out int pageCount)
        {
            double labelWidth = LabelWidth(contentWidth);
            EcosystemConfigFieldDescriptor[] all = EcosystemConfigSchema
                .GetCategoryFields(selectedCategory)
                .ToArray();

            var layouts = new FieldRowLayout[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                layouts[i] = MeasureFieldRow(all[i], labelWidth);
            }

            var pages = new List<FieldRowLayout[]>();
            var page = new List<FieldRowLayout>();
            double used = 0;

            for (int i = 0; i < layouts.Length; i++)
            {
                FieldRowLayout row = layouts[i];
                if (used + row.Height > maxHeight && page.Count > 0)
                {
                    pages.Add(page.ToArray());
                    page.Clear();
                    used = 0;
                }

                page.Add(row);
                used += row.Height;
            }

            if (page.Count > 0 || pages.Count == 0)
            {
                pages.Add(page.ToArray());
            }

            pageCount = Math.Max(1, pages.Count);
            if (fieldPage >= pageCount) fieldPage = pageCount - 1;
            if (fieldPage < 0) fieldPage = 0;

            return pages[fieldPage];
        }

        static double LabelWidth(double contentWidth) => contentWidth - ControlWidth - 6;

        FieldRowLayout MeasureFieldRow(EcosystemConfigFieldDescriptor field, double labelWidth)
        {
            string title = ConfigFieldLangResolver.GetTitle(field);
            string desc = ConfigFieldLangResolver.GetDescription(field);
            string scopeHint = field.Scope == ConfigFieldScope.Client
                ? L("config-ui-scope-client")
                : L("config-ui-scope-server");

            double textHeight = MeasureHelpTextHeight(title, scopeHint, field.Name, desc, labelWidth);
            double innerHeight = Math.Max(textHeight, ControlHeight + 2);
            return new FieldRowLayout(field, innerHeight + RowGap);
        }

        /// <summary>Measures help block height at the left-column width (control column excluded).</summary>
        double MeasureHelpTextHeight(string title, string scopeHint, string jsonKey, string desc, double labelWidth)
        {
            string plain = BuildFieldHelpPlainText(title, scopeHint, jsonKey, desc);
            double scaledHeight = textMeasure.GetMultilineTextHeight(
                helpFont,
                plain,
                labelWidth,
                EnumLinebreakBehavior.AfterWord);

            double height = scaledHeight / RuntimeEnv.GUIScale;
            height += textMeasure.GetLineHeight(helpFont) / RuntimeEnv.GUIScale * 0.2;
            return height + TextPadPx;
        }

        static string BuildFieldHelpPlainText(string title, string scopeHint, string jsonKey, string desc)
        {
            var sb = new StringBuilder();
            sb.Append(title);
            sb.Append('\n').Append(scopeHint);
            if (!string.IsNullOrWhiteSpace(jsonKey))
            {
                sb.Append('\n').Append(FormatJsonKey(jsonKey));
            }

            if (!string.IsNullOrWhiteSpace(desc))
            {
                sb.Append('\n').Append(desc);
            }

            return sb.ToString();
        }

        void AddFieldRow(GuiComposer composer, FieldRowLayout rowLayout, double x, double y, double width)
        {
            EcosystemConfigFieldDescriptor field = rowLayout.Field;
            string code = "cfg-" + field.Name;
            string title = ConfigFieldLangResolver.GetTitle(field);
            string desc = ConfigFieldLangResolver.GetDescription(field);
            string scopeHint = field.Scope == ConfigFieldScope.Client
                ? L("config-ui-scope-client")
                : L("config-ui-scope-server");

            double labelWidth = LabelWidth(width);
            double controlX = x + width - ControlWidth;
            double controlY = y + 2;

            double bodyHeight = rowLayout.Height - RowGap;

            composer.AddRichtext(
                BuildFieldHelpText(title, scopeHint, field.Name, desc),
                helpFont,
                ElementBounds.Fixed(x, y, labelWidth, bodyHeight),
                code + "-help");

            switch (field.Kind)
            {
                case ConfigFieldKind.Boolean:
                    composer.AddSwitch(
                        on => OnBoolChanged(field, on),
                        ElementBounds.Fixed(controlX, controlY, 40, 24),
                        code,
                        20,
                        3);
                    break;

                case ConfigFieldKind.String when field.AllowedValues != null && field.AllowedValues.Length > 0:
                    string[] values = field.AllowedValues;
                    string[] labels = values.Select(v => FieldAllowedLabel(field.Name, v)).ToArray();
                    string current = field.GetValue(working)?.ToString() ?? values[0];
                    int idx = Array.FindIndex(values, v => v.Equals(current, StringComparison.OrdinalIgnoreCase));
                    if (idx < 0) idx = 0;
                    composer.AddDropDown(
                        values,
                        labels,
                        idx,
                        (val, selected) => OnStringAllowedChanged(field, val, selected),
                        ElementBounds.Fixed(controlX, controlY, ControlWidth, 25),
                        code);
                    break;

                default:
                    composer.AddNumberInput(
                        ElementBounds.Fixed(controlX, controlY, ControlWidth, 25),
                        text => OnNumberChanged(field, text),
                        CairoFont.WhiteDetailText(),
                        code);
                    break;
            }

            string tooltip = BuildFieldHelpPlainText(title, scopeHint, field.Name, desc);
            composer.AddAutoSizeHoverText(
                tooltip,
                CairoFont.WhiteSmallText(),
                380,
                ElementBounds.Fixed(x, y, width, bodyHeight).FlatCopy(),
                code + "-tip");
        }

        void ApplyHoverTextAutoWidth()
        {
            if (SingleComposer == null) return;

            SetHoverAutoWidth("cfg-preset-tip");
            SetHoverAutoWidth("cfg-category-tip");

            foreach (FieldRowLayout row in currentPageRows)
            {
                SetHoverAutoWidth("cfg-" + row.Field.Name + "-tip");
            }
        }

        void SetHoverAutoWidth(string key)
        {
            SingleComposer.GetHoverText(key)?.SetAutoWidth(true);
        }

        void AddFooter(GuiComposer composer, double pad, double y, double height)
        {
            double btnW = 88;
            double gap = 6;
            double x = pad;

            composer.AddButton(
                L("config-ui-reload"),
                OnReloadClicked,
                ElementBounds.Fixed(x, y, btnW, height),
                CairoFont.WhiteSmallText());
            x += btnW + gap;

            composer.AddButton(
                L("config-ui-prev"),
                OnPrevPage,
                ElementBounds.Fixed(x, y, 64, height),
                CairoFont.WhiteSmallText());
            x += 64 + gap;

            composer.AddButton(
                L("config-ui-next"),
                OnNextPage,
                ElementBounds.Fixed(x, y, 64, height),
                CairoFont.WhiteSmallText());

            x = DialogWidth - pad - btnW;
            composer.AddButton(
                L("config-ui-cancel"),
                () => { TryClose(); return true; },
                ElementBounds.Fixed(x - btnW - gap, y, btnW, height),
                CairoFont.WhiteSmallText());

            composer.AddButton(
                L("config-ui-apply"),
                OnApplyClicked,
                ElementBounds.Fixed(x, y, btnW, height),
                CairoFont.WhiteDetailText());
        }

        void ApplyControlValues()
        {
            foreach (FieldRowLayout row in currentPageRows)
            {
                EcosystemConfigFieldDescriptor field = row.Field;
                string code = "cfg-" + field.Name;
                object value = field.GetValue(working);

                if (field.Kind == ConfigFieldKind.Boolean)
                {
                    SingleComposer.GetSwitch(code)?.SetValue(value is bool b && b);
                    continue;
                }

                if (field.Kind == ConfigFieldKind.String
                    && field.AllowedValues != null
                    && field.AllowedValues.Length > 0)
                {
                    continue;
                }

                SingleComposer.GetNumberInput(code)?.SetValue(value?.ToString() ?? "0");
            }
        }

        void OnPresetSelected(string code, bool selected)
        {
            if (!selected || string.IsNullOrWhiteSpace(code)) return;
            EcosystemConfigSchema.ApplyPresetSelection(working, code);
            RequestRecompose();
        }

        void OnCategorySelected(string code, bool selected)
        {
            if (!selected || string.IsNullOrWhiteSpace(code)) return;
            selectedCategory = code;
            fieldPage = 0;
            RequestRecompose();
        }

        void OnBoolChanged(EcosystemConfigFieldDescriptor field, bool value)
        {
            field.SetValue(working, value);
            EcosystemConfigSchema.MarkCustomIfPresetFieldEdited(working, field.Name);
        }

        void OnStringAllowedChanged(EcosystemConfigFieldDescriptor field, string value, bool selected)
        {
            if (!selected || string.IsNullOrWhiteSpace(value)) return;
            field.SetValue(working, value);
            EcosystemConfigSchema.MarkCustomIfPresetFieldEdited(working, field.Name);
            if (field.Name == nameof(EcosystemConfig.BalancePreset))
            {
                RequestRecompose();
            }
        }

        void OnNumberChanged(EcosystemConfigFieldDescriptor field, string text)
        {
            if (!TryParseNumber(field, text, out object parsed)) return;
            field.SetValue(working, parsed);
            EcosystemConfigSchema.MarkCustomIfPresetFieldEdited(working, field.Name);
        }

        static bool TryParseNumber(EcosystemConfigFieldDescriptor field, string text, out object parsed)
        {
            parsed = null;
            if (string.IsNullOrWhiteSpace(text)) return false;

            switch (field.Kind)
            {
                case ConfigFieldKind.Integer:
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    {
                        return false;
                    }

                    parsed = i;
                    return true;

                case ConfigFieldKind.Float:
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    {
                        return false;
                    }

                    parsed = f;
                    return true;

                case ConfigFieldKind.Double:
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    {
                        return false;
                    }

                    parsed = d;
                    return true;
            }

            return false;
        }

        bool OnReloadClicked()
        {
            EcosystemConfigSaveService.ReloadFromDisk(capi, createDefaultIfMissing: true);
            EcosystemConfigCopier.CopyFields(EcosystemConfig.Loaded, working);
            RequestRecompose();
            capi.ShowChatMessage(L("config-ui-reloaded"));
            return true;
        }

        bool OnPrevPage()
        {
            if (fieldPage > 0)
            {
                fieldPage--;
                RequestRecompose();
            }

            return true;
        }

        bool OnNextPage()
        {
            if (fieldPage + 1 < currentPageCount)
            {
                fieldPage++;
                RequestRecompose();
            }

            return true;
        }

        bool OnApplyClicked()
        {
            if (EcosystemConfigValidator.NormalizeInPlace(working) > 0)
            {
                capi.ShowChatMessage(L("config-ui-clamped"));
                RequestRecompose();
            }

            OnApplyRequested?.Invoke(working);
            return true;
        }

        void OnTitleBarClose()
        {
            TryClose();
        }

        public event Action<EcosystemConfig> OnApplyRequested;

        static string BuildFieldHelpText(string title, string scopeHint, string jsonKey, string desc)
        {
            var sb = new StringBuilder();
            sb.Append("<strong>").Append(EscapeRichText(title)).Append("</strong><br/>");
            sb.Append("<i>").Append(EscapeRichText(scopeHint)).Append("</i>");

            if (!string.IsNullOrWhiteSpace(jsonKey))
            {
                sb.Append("<br/><strong>").Append(EscapeRichText(FormatJsonKey(jsonKey))).Append("</strong>");
            }

            if (!string.IsNullOrWhiteSpace(desc))
            {
                sb.Append("<br/>").Append(EscapeRichText(desc));
            }

            return sb.ToString();
        }

        static string FormatJsonKey(string propertyName) =>
            string.Format(CultureInfo.InvariantCulture, L("config-ui-json-key"), propertyName);

        static string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);
        }

        static string L(string key) => Lang.Get("ecosystemflora:" + key);

        static string PresetLabel(string code)
        {
            string key = "config-preset-" + code.ToLowerInvariant();
            string translated = L(key);
            return translated == "ecosystemflora:" + key ? code : translated;
        }

        static string CategoryLabel(string code)
        {
            string key = "config-cat-" + code;
            string translated = L(key);
            return translated == "ecosystemflora:" + key ? code : translated;
        }

        static string FieldAllowedLabel(string fieldName, string value)
        {
            string key = "config-field-" + fieldName + "-val-" + value.ToLowerInvariant();
            string translated = L(key);
            return translated == "ecosystemflora:" + key ? value : translated;
        }
    }
}
