using System;
using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using WildFarming.Ecosystem;
using WildFarming.Ecosystem.Config;

namespace WildFarming.Client
{
    /// <summary>
    /// First-run / re-run setup: welcome → profile + scope → performance bench table.
    /// </summary>
    public class EcosystemSetupWizardDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        const int PageWelcome = 0;
        const int PageProfile = 1;
        const int PageBench = 2;

        const double DialogWidth = 720;
        const double DialogHeightWelcome = 320;
        const double DialogHeightProfile = 300;
        const double DialogHeightBench = 610;
        const double EditColWidth = 78;
        const double BenchColWidth = 62;
        const double MinimalColWidth = 62;
        const double ColGap = 6;
        const double RowH = 24;
        const int FieldsPerPage = 12;

        readonly EcosystemConfig working;
        EcosystemConfig baselineCfg;
        EcosystemConfig benchCfg;
        EcosystemConfig minimalCfg;

        string profileCode = EcosystemSetupProfiles.Balanced;
        bool nearPlayers;
        int page;
        int fieldPage;
        string lastResultText = "";
        bool benchRan;

        public EcosystemConfig WorkingCopy => working;

        public event Action<EcosystemConfig> OnFinished;

        public EcosystemSetupWizardDialog(ICoreClientAPI capi, EcosystemConfig source) : base(capi)
        {
            working = EcosystemConfigCopier.Clone(source ?? EcosystemConfig.Loaded);
            profileCode = EcosystemSetupProfiles.Balanced;
            nearPlayers = EcosystemSetupProfiles.DefaultNearPlayers(profileCode);
            page = PageWelcome;
            fieldPage = 0;
            RefreshResultTextFromConfig();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            if (!TryCompose())
            {
                TryClose();
            }
        }

        bool TryCompose()
        {
            SingleComposer?.Dispose();
            switch (page)
            {
                case PageWelcome:
                    return ComposeWelcomePage();
                case PageProfile:
                    return ComposeProfilePage();
                default:
                    return ComposeBenchPage();
            }
        }

        bool ComposeWelcomePage()
        {
            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double height = DialogHeightWelcome;

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(DialogWidth, height)
                .WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, DialogWidth, height);

            var composer = capi.Gui
                .CreateCompo("ecosystemflora-setup", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(L("setup-wizard-welcome-title"), () => TryClose());

            double y = titleH + pad;
            double contentW = DialogWidth - pad * 2;

            composer.AddStaticText(
                L("setup-wizard-welcome-heading"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, contentW, 28));
            y += 32;

            composer.AddStaticText(
                L("setup-wizard-welcome-body"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 120));
            y += 128;

            composer.AddStaticText(
                L("setup-wizard-welcome-steps"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 48));

            double btnY = height - pad - 30;
            double btnW = 110;
            composer.AddButton(
                L("setup-wizard-skip"),
                OnSkip,
                ElementBounds.Fixed(pad, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-next"),
                OnNextFromWelcome,
                ElementBounds.Fixed(DialogWidth - pad - btnW, btnY, btnW, 28),
                CairoFont.WhiteDetailText());

            try
            {
                SingleComposer = composer.Compose();
                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Setup wizard welcome failed: {0}", ex);
                return false;
            }
        }

        bool ComposeProfilePage()
        {
            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double height = DialogHeightProfile;

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(DialogWidth, height)
                .WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, DialogWidth, height);

            var composer = capi.Gui
                .CreateCompo("ecosystemflora-setup", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(L("setup-wizard-title"), () => TryClose());

            double y = titleH + pad;
            double labelW = 140;
            double controlW = DialogWidth - pad * 2 - labelW - 8;

            composer.AddStaticText(
                L("setup-wizard-intro"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, DialogWidth - pad * 2, 40));
            y += 44;

            composer.AddStaticText(
                L("setup-wizard-profile"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, labelW, 25));

            string[] codes = EcosystemSetupProfiles.Codes;
            string[] labels = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++)
            {
                labels[i] = L("setup-wizard-profile-" + codes[i]);
            }

            int profileIndex = Array.IndexOf(codes, profileCode);
            if (profileIndex < 0) profileIndex = 0;

            composer.AddDropDown(
                codes,
                labels,
                profileIndex,
                OnProfileSelected,
                ElementBounds.Fixed(pad + labelW, y, controlW, 25),
                "setup-profile");
            y += 32;

            composer.AddSwitch(
                OnNearPlayersChanged,
                ElementBounds.Fixed(pad, y, 40, 24),
                "setup-near",
                20,
                3);
            composer.AddStaticText(
                L("setup-wizard-near-players"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad + 36, y, DialogWidth - pad * 2 - 36, 25));
            y += 34;

            composer.AddStaticText(
                L("setup-wizard-hint"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, DialogWidth - pad * 2, 40));

            double btnY = height - pad - 30;
            double btnW = 110;
            composer.AddButton(
                L("setup-wizard-back"),
                OnBackToWelcome,
                ElementBounds.Fixed(pad, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-skip"),
                OnSkip,
                ElementBounds.Fixed(pad + btnW + 8, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-next"),
                OnNextFromProfile,
                ElementBounds.Fixed(DialogWidth - pad - btnW, btnY, btnW, 28),
                CairoFont.WhiteDetailText());

            try
            {
                SingleComposer = composer.Compose();
                SingleComposer.GetSwitch("setup-near")?.SetValue(nearPlayers);
                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Setup wizard profile failed: {0}", ex);
                return false;
            }
        }

        bool ComposeBenchPage()
        {
            EnsureMinimalCfg();
            string[] fields = EcosystemPerfCalibrator.WizardEditableFields;
            int fieldPageCount = Math.Max(1, (fields.Length + FieldsPerPage - 1) / FieldsPerPage);
            if (fieldPage >= fieldPageCount) fieldPage = fieldPageCount - 1;
            if (fieldPage < 0) fieldPage = 0;

            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double height = DialogHeightBench;

            ElementBounds dialogBounds = ElementBounds
                .FixedSize(DialogWidth, height)
                .WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fixed(0, 0, DialogWidth, height);

            var composer = capi.Gui
                .CreateCompo("ecosystemflora-setup", dialogBounds)
                .AddShadedDialogBG(bgBounds, true)
                .AddDialogTitleBar(L("setup-wizard-bench-title"), () => TryClose());

            double y = titleH + pad;
            double contentW = DialogWidth - pad * 2;
            double minimalX = pad + contentW - MinimalColWidth;
            double benchX = minimalX - ColGap - BenchColWidth;
            double editX = benchX - ColGap - EditColWidth;
            double labelW = editX - pad - ColGap;

            composer.AddStaticText(
                L("setup-wizard-bench-intro"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 32));
            y += 34;

            composer.AddButton(
                L("setup-wizard-bench-run"),
                OnRunBench,
                ElementBounds.Fixed(pad, y, 130, 26),
                CairoFont.WhiteDetailText());

            double btnX = pad + 138;
            if (benchRan)
            {
                composer.AddButton(
                    L("setup-wizard-bench-use"),
                    OnUseBenchValues,
                    ElementBounds.Fixed(btnX, y, 140, 26),
                    CairoFont.WhiteSmallText());
                btnX += 148;
            }

            composer.AddButton(
                L("setup-wizard-minimal-use"),
                OnUseMinimalValues,
                ElementBounds.Fixed(btnX, y, 190, 26),
                CairoFont.WhiteSmallText());
            y += 30;

            composer.AddStaticText(
                string.IsNullOrWhiteSpace(lastResultText) ? L("setup-wizard-bench-idle") : lastResultText,
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 22));
            y += 24;

            composer.AddStaticText(
                L("setup-wizard-bench-legend"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 36));
            y += 38;

            composer.AddStaticText(
                string.Format(
                    CultureInfo.InvariantCulture,
                    L("setup-wizard-bench-page"),
                    fieldPage + 1,
                    fieldPageCount,
                    fields.Length),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 18));
            y += 20;

            composer.AddStaticText(
                L("setup-wizard-bench-edit"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, labelW, 18));
            composer.AddStaticText(
                L("setup-wizard-bench-col-yours"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(editX, y, EditColWidth, 18));
            composer.AddStaticText(
                L("setup-wizard-bench-col-bench"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(benchX, y, BenchColWidth, 18));
            composer.AddStaticText(
                L("setup-wizard-bench-col-minimal"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(minimalX, y, MinimalColWidth, 18));
            y += 20;

            int start = fieldPage * FieldsPerPage;
            int end = Math.Min(fields.Length, start + FieldsPerPage);
            for (int i = start; i < end; i++)
            {
                string fieldName = fields[i];
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null) continue;
                if (field.Kind != ConfigFieldKind.Integer && field.Kind != ConfigFieldKind.Boolean) continue;

                string title = EcosystemPerfKnobHints.FormatTitleWithHint(
                    ConfigFieldLangResolver.GetTitle(field),
                    fieldName);
                composer.AddStaticText(
                    title,
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(pad, y, labelW, RowH));

                string code = "bench-" + fieldName;
                if (field.Kind == ConfigFieldKind.Boolean)
                {
                    composer.AddSwitch(
                        on => OnBenchBoolChanged(field, on),
                        ElementBounds.Fixed(editX, y, 40, 22),
                        code,
                        18,
                        3);
                }
                else
                {
                    composer.AddNumberInput(
                        ElementBounds.Fixed(editX, y, EditColWidth, 20),
                        val => OnBenchFieldChanged(field, val),
                        CairoFont.WhiteSmallText(),
                        code);
                }

                composer.AddStaticText(
                    FormatCfgColumn(benchCfg, field, requireBench: true),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(benchX, y, BenchColWidth, RowH));
                composer.AddStaticText(
                    FormatCfgColumn(minimalCfg, field, requireBench: false),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(minimalX, y, MinimalColWidth, RowH));
                y += RowH;
            }

            double btnY = height - pad - 30;
            double btnW = 90;
            composer.AddButton(
                L("setup-wizard-back"),
                OnBackToProfile,
                ElementBounds.Fixed(pad, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-fields-prev"),
                OnFieldsPrev,
                ElementBounds.Fixed(pad + btnW + 6, btnY, 70, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-fields-next"),
                OnFieldsNext,
                ElementBounds.Fixed(pad + btnW + 82, btnY, 70, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-skip"),
                OnSkip,
                ElementBounds.Fixed(pad + btnW + 158, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-apply"),
                OnApply,
                ElementBounds.Fixed(DialogWidth - pad - btnW, btnY, btnW, 28),
                CairoFont.WhiteDetailText());

            try
            {
                SingleComposer = composer.Compose();
                ApplyBenchFieldValues(start, end);
                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Setup wizard bench failed: {0}", ex);
                return false;
            }
        }

        void ApplyBenchFieldValues(int start, int end)
        {
            if (SingleComposer == null) return;
            string[] fields = EcosystemPerfCalibrator.WizardEditableFields;
            for (int i = start; i < end && i < fields.Length; i++)
            {
                string fieldName = fields[i];
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null) continue;
                object value = field.GetValue(working);
                string code = "bench-" + fieldName;
                if (field.Kind == ConfigFieldKind.Boolean)
                {
                    SingleComposer.GetSwitch(code)?.SetValue(value is bool b && b);
                }
                else
                {
                    SingleComposer.GetNumberInput(code)?.SetValue(value?.ToString() ?? "0");
                }
            }
        }

        string FormatCfgColumn(EcosystemConfig cfg, EcosystemConfigFieldDescriptor field, bool requireBench)
        {
            if (requireBench && (!benchRan || cfg == null)) return "—";
            if (cfg == null || field == null) return "—";
            object value = field.GetValue(cfg);
            if (field.Kind == ConfigFieldKind.Boolean)
            {
                return value is bool b && b ? "on" : "off";
            }

            return value?.ToString() ?? "—";
        }

        void EnsureMinimalCfg()
        {
            if (minimalCfg != null) return;
            minimalCfg = new EcosystemConfig();
            EcosystemPerfCalibrator.ApplySuperMinimal(minimalCfg);
        }

        void OnBenchFieldChanged(EcosystemConfigFieldDescriptor field, string text)
        {
            if (field == null) return;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)
                && !int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out n))
            {
                return;
            }

            field.SetValue(working, n);
            working.BalancePreset = EcosystemBalancePresets.Custom;
        }

        void OnBenchBoolChanged(EcosystemConfigFieldDescriptor field, bool value)
        {
            if (field == null) return;
            field.SetValue(working, value);
            working.BalancePreset = EcosystemBalancePresets.Custom;
            if (field.Name == nameof(EcosystemConfig.OnlyActivateNearPlayers))
            {
                nearPlayers = value;
            }
        }

        void OnProfileSelected(string code, bool selected)
        {
            if (!selected || string.IsNullOrWhiteSpace(code)) return;
            profileCode = code;
            nearPlayers = EcosystemSetupProfiles.DefaultNearPlayers(code);
            SingleComposer?.GetSwitch("setup-near")?.SetValue(nearPlayers);
        }

        void OnNearPlayersChanged(bool value) => nearPlayers = value;

        bool OnNextFromWelcome()
        {
            page = PageProfile;
            return TryCompose();
        }

        bool OnBackToWelcome()
        {
            page = PageWelcome;
            return TryCompose();
        }

        bool OnNextFromProfile()
        {
            EcosystemSetupProfiles.ApplyProfile(working, profileCode, nearPlayers);
            page = PageBench;
            fieldPage = 0;
            benchRan = false;
            benchCfg = null;
            baselineCfg = EcosystemConfigCopier.Clone(working);
            EnsureMinimalCfg();
            RefreshResultTextFromConfig();
            return TryCompose();
        }

        bool OnBackToProfile()
        {
            page = PageProfile;
            return TryCompose();
        }

        bool OnFieldsPrev()
        {
            if (fieldPage > 0)
            {
                fieldPage--;
                return TryCompose();
            }

            return true;
        }

        bool OnFieldsNext()
        {
            int count = EcosystemPerfCalibrator.WizardEditableFields.Length;
            int pages = Math.Max(1, (count + FieldsPerPage - 1) / FieldsPerPage);
            if (fieldPage + 1 < pages)
            {
                fieldPage++;
                return TryCompose();
            }

            return true;
        }

        bool OnRunBench()
        {
            if (baselineCfg == null)
            {
                baselineCfg = EcosystemConfigCopier.Clone(working);
            }

            EcosystemConfig measured = EcosystemConfigCopier.Clone(working);
            EcosystemPerfCalibrator.CalibrationResult result = EcosystemPerfCalibrator.RunAndApply(measured);
            benchCfg = EcosystemConfigCopier.Clone(measured);
            EcosystemPerfCalibrator.CopyWizardFields(baselineCfg, working);
            EcosystemPerfCalibrator.RecordResult(working, result);

            benchRan = true;
            lastResultText = Lang.Get(
                "ecosystemflora:setup-wizard-autotune-result",
                result.Tier.ToString(),
                result.OpsPerMs.ToString("0.0", CultureInfo.InvariantCulture),
                result.ElapsedMs);
            return TryCompose();
        }

        bool OnUseBenchValues()
        {
            if (!benchRan || benchCfg == null) return true;
            EcosystemPerfCalibrator.CopyWizardFields(benchCfg, working);
            working.BalancePreset = EcosystemBalancePresets.Custom;
            nearPlayers = working.OnlyActivateNearPlayers;
            return TryCompose();
        }

        bool OnUseMinimalValues()
        {
            EnsureMinimalCfg();
            EcosystemPerfCalibrator.CopyWizardFields(minimalCfg, working);
            working.BalancePreset = EcosystemBalancePresets.Custom;
            nearPlayers = working.OnlyActivateNearPlayers;
            return TryCompose();
        }

        bool OnSkip()
        {
            working.SetupWizardCompleted = true;
            OnFinished?.Invoke(working);
            TryClose();
            return true;
        }

        bool OnApply()
        {
            EcosystemConfigValidator.NormalizeInPlace(working);
            working.SetupWizardCompleted = true;
            if (benchRan)
            {
                working.BalancePreset = EcosystemBalancePresets.Custom;
            }

            OnFinished?.Invoke(working);
            TryClose();
            return true;
        }

        void RefreshResultTextFromConfig()
        {
            if (string.IsNullOrWhiteSpace(working.LastAutoTuneTier))
            {
                lastResultText = "";
                return;
            }

            lastResultText = Lang.Get(
                "ecosystemflora:setup-wizard-autotune-result",
                working.LastAutoTuneTier,
                working.LastAutoTuneOpsPerMs.ToString("0.0", CultureInfo.InvariantCulture),
                working.LastAutoTuneElapsedMs);
        }

        static string L(string key) => Lang.Get("ecosystemflora:" + key);
    }
}
