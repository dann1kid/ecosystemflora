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
    /// First-run / re-run setup: page 1 = profile + scope; page 2 = benchmark + editable perf knobs
    /// with a read-only bench-suggestion column for comparison.
    /// </summary>
    public class EcosystemSetupWizardDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => null;

        const double DialogWidth = 700;
        const double DialogHeightPage1 = 300;
        const double DialogHeightPage2 = 560;
        const double EditColWidth = 80;
        const double BenchColWidth = 64;
        const double MinimalColWidth = 64;
        const double ColGap = 6;
        const double RowH = 26;

        readonly EcosystemConfig working;
        readonly Dictionary<string, int> benchSnapshot = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> baselineSnapshot = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> minimalSnapshot = new Dictionary<string, int>(StringComparer.Ordinal);

        string profileCode = EcosystemSetupProfiles.Balanced;
        bool nearPlayers;
        int page;
        string lastResultText = "";
        bool benchRan;

        public EcosystemConfig WorkingCopy => working;

        public event Action<EcosystemConfig> OnFinished;

        public EcosystemSetupWizardDialog(ICoreClientAPI capi, EcosystemConfig source) : base(capi)
        {
            working = EcosystemConfigCopier.Clone(source ?? EcosystemConfig.Loaded);
            profileCode = EcosystemSetupProfiles.Balanced;
            nearPlayers = EcosystemSetupProfiles.DefaultNearPlayers(profileCode);
            page = 0;
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
            return page == 0 ? ComposePage1() : ComposePage2();
        }

        bool ComposePage1()
        {
            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double height = DialogHeightPage1;

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
                L("setup-wizard-skip"),
                OnSkip,
                ElementBounds.Fixed(pad, btnY, btnW, 28),
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
                capi.Logger.Error("[ecosystemflora] Setup wizard page1 failed: {0}", ex);
                return false;
            }
        }

        bool ComposePage2()
        {
            double pad = GuiStyle.ElementToDialogPadding;
            double titleH = GuiStyle.TitleBarHeight;
            double height = DialogHeightPage2;

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
                ElementBounds.Fixed(pad, y, contentW, 36));
            y += 40;

            composer.AddButton(
                L("setup-wizard-bench-run"),
                OnRunBench,
                ElementBounds.Fixed(pad, y, 140, 28),
                CairoFont.WhiteDetailText());

            double btnX = pad + 148;
            if (benchRan)
            {
                composer.AddButton(
                    L("setup-wizard-bench-use"),
                    OnUseBenchValues,
                    ElementBounds.Fixed(btnX, y, 150, 28),
                    CairoFont.WhiteSmallText());
                btnX += 158;
            }

            composer.AddButton(
                L("setup-wizard-minimal-use"),
                OnUseMinimalValues,
                ElementBounds.Fixed(btnX, y, 200, 28),
                CairoFont.WhiteSmallText());

            y += 34;

            composer.AddStaticText(
                string.IsNullOrWhiteSpace(lastResultText) ? L("setup-wizard-bench-idle") : lastResultText,
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(pad, y, contentW, 28));
            y += 32;

            composer.AddStaticText(
                L("setup-wizard-bench-edit"),
                CairoFont.WhiteDetailText(),
                ElementBounds.Fixed(pad, y, labelW, 22));
            composer.AddStaticText(
                L("setup-wizard-bench-col-yours"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(editX, y, EditColWidth, 22));
            composer.AddStaticText(
                L("setup-wizard-bench-col-bench"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(benchX, y, BenchColWidth, 22));
            composer.AddStaticText(
                L("setup-wizard-bench-col-minimal"),
                CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(minimalX, y, MinimalColWidth, 22));
            y += 24;

            foreach (string fieldName in EcosystemPerfCalibrator.WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null || field.Kind != ConfigFieldKind.Integer) continue;

                string title = ConfigFieldLangResolver.GetTitle(field);
                composer.AddStaticText(
                    title,
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(pad, y, labelW, RowH));

                string code = "bench-" + fieldName;
                composer.AddNumberInput(
                    ElementBounds.Fixed(editX, y, EditColWidth, 22),
                    val => OnBenchFieldChanged(field, val),
                    CairoFont.WhiteSmallText(),
                    code);

                composer.AddStaticText(
                    FormatSnapshotColumn(benchSnapshot, fieldName, requireBench: true),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(benchX, y, BenchColWidth, RowH));

                composer.AddStaticText(
                    FormatSnapshotColumn(minimalSnapshot, fieldName, requireBench: false),
                    CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(minimalX, y, MinimalColWidth, RowH));
                y += RowH;
            }

            double btnY = height - pad - 30;
            double btnW = 110;
            composer.AddButton(
                L("setup-wizard-back"),
                OnBack,
                ElementBounds.Fixed(pad, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-skip"),
                OnSkip,
                ElementBounds.Fixed(pad + btnW + 8, btnY, btnW, 28),
                CairoFont.WhiteSmallText());
            composer.AddButton(
                L("setup-wizard-apply"),
                OnApply,
                ElementBounds.Fixed(DialogWidth - pad - btnW, btnY, btnW, 28),
                CairoFont.WhiteDetailText());

            try
            {
                SingleComposer = composer.Compose();
                ApplyBenchFieldValues();
                return true;
            }
            catch (Exception ex)
            {
                capi.Logger.Error("[ecosystemflora] Setup wizard page2 failed: {0}", ex);
                return false;
            }
        }

        string FormatSnapshotColumn(Dictionary<string, int> snapshot, string fieldName, bool requireBench)
        {
            if (requireBench && !benchRan) return "—";
            if (snapshot == null || !snapshot.TryGetValue(fieldName, out int value)) return "—";
            return value.ToString(CultureInfo.InvariantCulture);
        }

        void EnsureMinimalSnapshot()
        {
            if (minimalSnapshot.Count > 0) return;
            var tmp = new EcosystemConfig();
            EcosystemPerfCalibrator.ApplySuperMinimal(tmp);
            CaptureSnapshotFrom(tmp, minimalSnapshot);
        }

        void CaptureSnapshot(Dictionary<string, int> target)
        {
            target.Clear();
            foreach (string fieldName in EcosystemPerfCalibrator.WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null) continue;
                object raw = field.GetValue(working);
                if (raw is int n)
                {
                    target[fieldName] = n;
                }
                else if (raw != null && int.TryParse(raw.ToString(), out int parsed))
                {
                    target[fieldName] = parsed;
                }
            }
        }

        void ApplyBenchFieldValues()
        {
            if (SingleComposer == null) return;
            foreach (string fieldName in EcosystemPerfCalibrator.WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null) continue;
                object value = field.GetValue(working);
                SingleComposer.GetNumberInput("bench-" + fieldName)?.SetValue(value?.ToString() ?? "0");
            }
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

        void OnProfileSelected(string code, bool selected)
        {
            if (!selected || string.IsNullOrWhiteSpace(code)) return;
            profileCode = code;
            nearPlayers = EcosystemSetupProfiles.DefaultNearPlayers(code);
            SingleComposer?.GetSwitch("setup-near")?.SetValue(nearPlayers);
        }

        void OnNearPlayersChanged(bool value) => nearPlayers = value;

        bool OnNextFromProfile()
        {
            EcosystemSetupProfiles.ApplyProfile(working, profileCode, nearPlayers);
            page = 1;
            benchRan = false;
            benchSnapshot.Clear();
            CaptureSnapshot(baselineSnapshot);
            EnsureMinimalSnapshot();
            RefreshResultTextFromConfig();
            return TryCompose();
        }

        bool OnBack()
        {
            page = 0;
            return TryCompose();
        }

        bool OnRunBench()
        {
            if (baselineSnapshot.Count == 0)
            {
                CaptureSnapshot(baselineSnapshot);
            }

            // Apply tiers on a clone so we can keep profile defaults in the editable column.
            EcosystemConfig benchCfg = EcosystemConfigCopier.Clone(working);
            EcosystemPerfCalibrator.CalibrationResult result = EcosystemPerfCalibrator.RunAndApply(benchCfg);

            CaptureSnapshotFrom(benchCfg, benchSnapshot);
            // Restore editable column to profile defaults for side-by-side comparison.
            ApplySnapshot(baselineSnapshot, working);
            // Keep auto-tune metadata from the bench run on the working config.
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
            if (!benchRan || benchSnapshot.Count == 0) return true;
            ApplySnapshot(benchSnapshot, working);
            working.BalancePreset = EcosystemBalancePresets.Custom;
            ApplyBenchFieldValues();
            return true;
        }

        bool OnUseMinimalValues()
        {
            EnsureMinimalSnapshot();
            EcosystemPerfCalibrator.ApplySuperMinimal(working);
            working.BalancePreset = EcosystemBalancePresets.Custom;
            nearPlayers = true;
            ApplyBenchFieldValues();
            return true;
        }

        void CaptureSnapshotFrom(EcosystemConfig source, Dictionary<string, int> target)
        {
            target.Clear();
            if (source == null) return;
            foreach (string fieldName in EcosystemPerfCalibrator.WizardEditableFields)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(fieldName);
                if (field == null) continue;
                object raw = field.GetValue(source);
                if (raw is int n)
                {
                    target[fieldName] = n;
                }
                else if (raw != null && int.TryParse(raw.ToString(), out int parsed))
                {
                    target[fieldName] = parsed;
                }
            }
        }

        void ApplySnapshot(Dictionary<string, int> source, EcosystemConfig target)
        {
            if (source == null || target == null) return;
            foreach (KeyValuePair<string, int> pair in source)
            {
                EcosystemConfigFieldDescriptor field = EcosystemConfigSchema.GetField(pair.Key);
                field?.SetValue(target, pair.Value);
            }
        }

        bool OnSkip()
        {
            if (page == 0)
            {
                working.SetupWizardCompleted = true;
                OnFinished?.Invoke(working);
                TryClose();
                return true;
            }

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
