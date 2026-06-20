using System;
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

        const int MaxFieldsPerPage = 6;
        const double DialogWidth = 540;
        const double DialogHeight = 540;
        const double RowHeight = 62;
        const double HeaderBlockHeight = 90;
        const double FooterBlockHeight = 44;
        const double FooterGap = 12;

        readonly EcosystemConfig working;
        string selectedCategory = "master";
        int fieldPage;
        int fieldsPerPage = 5;
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
            fieldsPerPage = Math.Max(1, Math.Min(MaxFieldsPerPage, (int)Math.Floor(contentHeight / RowHeight)));

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(DialogWidth, DialogHeight)
                .WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, DialogWidth, DialogHeight);

            var composer = capi.Gui
                .CreateCompo("ecosystemflora-config", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(L("config-ui-title"), OnTitleBarClose);

            AddHeader(composer, pad, titleH);
            AddFieldRows(composer, pad, contentTop, contentHeight);
            AddFooter(composer, pad, footerTop, FooterBlockHeight);

            try
            {
                SingleComposer = composer.Compose();
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

            y += 32;

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

            y += 30;

            EcosystemConfigFieldDescriptor[] pageFields = GetPageFields(out int pageCount);
            string pageText = string.Format(
                CultureInfo.InvariantCulture,
                L("config-ui-page"),
                fieldPage + 1,
                Math.Max(1, pageCount));

            composer.AddStaticText(
                pageText,
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, DialogWidth - pad * 2, 20));
        }

        void AddFieldRows(GuiComposer composer, double pad, double top, double maxHeight)
        {
            EcosystemConfigFieldDescriptor[] pageFields = GetPageFields(out _);
            double y = top;

            for (int i = 0; i < pageFields.Length; i++)
            {
                if (y + RowHeight > top + maxHeight + 0.5) break;

                AddFieldRow(composer, pageFields[i], pad, y, DialogWidth - pad * 2);
                y += RowHeight;
            }

            if (pageFields.Length == 0)
            {
                composer.AddStaticText(
                    L("config-ui-empty-category"),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(pad, top + 12, DialogWidth - pad * 2, 30));
            }
        }

        void AddFieldRow(GuiComposer composer, EcosystemConfigFieldDescriptor field, double x, double y, double width)
        {
            string code = "cfg-" + field.Name;
            string title = FieldTitle(field.Name);
            string desc = FieldDescription(field.Name);
            string scopeHint = field.Scope == ConfigFieldScope.Client
                ? L("config-ui-scope-client")
                : L("config-ui-scope-server");

            composer.AddStaticText(
                title + "  " + scopeHint,
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(x, y, width, 18));

            if (!string.IsNullOrEmpty(desc))
            {
                composer.AddStaticText(
                    desc,
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(x, y + 18, width - 120, 16));
            }

            double controlX = x + width - 118;
            double controlY = y + 16;
            double controlW = 118;

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
                        ElementBounds.Fixed(controlX, controlY, controlW, 25),
                        code);
                    break;

                default:
                    composer.AddNumberInput(
                        ElementBounds.Fixed(controlX, controlY, controlW, 25),
                        text => OnNumberChanged(field, text),
                        CairoFont.WhiteDetailText(),
                        code);
                    break;
            }
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
            foreach (EcosystemConfigFieldDescriptor field in GetPageFields(out _))
            {
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

        EcosystemConfigFieldDescriptor[] GetPageFields(out int pageCount)
        {
            EcosystemConfigFieldDescriptor[] all = EcosystemConfigSchema
                .GetCategoryFields(selectedCategory)
                .ToArray();

            pageCount = Math.Max(1, (all.Length + fieldsPerPage - 1) / fieldsPerPage);
            if (fieldPage >= pageCount) fieldPage = pageCount - 1;
            if (fieldPage < 0) fieldPage = 0;

            return all.Skip(fieldPage * fieldsPerPage).Take(fieldsPerPage).ToArray();
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
            GetPageFields(out int pageCount);
            if (fieldPage + 1 < pageCount)
            {
                fieldPage++;
                RequestRecompose();
            }

            return true;
        }

        bool OnApplyClicked()
        {
            if (!EcosystemConfigValidator.TryValidate(working, out _))
            {
                capi.ShowChatMessage(L("config-ui-error-invalid"));
                return true;
            }

            OnApplyRequested?.Invoke(working);
            return true;
        }

        void OnTitleBarClose()
        {
            TryClose();
        }

        public event Action<EcosystemConfig> OnApplyRequested;

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

        static string FieldTitle(string name)
        {
            string key = "config-field-" + name;
            string translated = L(key);
            if (translated != "ecosystemflora:" + key) return translated;
            return SplitCamelCase(name);
        }

        static string FieldDescription(string name)
        {
            string key = "config-field-" + name + "-desc";
            string translated = L(key);
            return translated == "ecosystemflora:" + key ? string.Empty : translated;
        }

        static string FieldAllowedLabel(string fieldName, string value)
        {
            string key = "config-field-" + fieldName + "-val-" + value.ToLowerInvariant();
            string translated = L(key);
            return translated == "ecosystemflora:" + key ? value : translated;
        }

        static string SplitCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var sb = new StringBuilder(name.Length + 8);
            sb.Append(name[0]);
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                {
                    sb.Append(' ');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
