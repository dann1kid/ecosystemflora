using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WildFarming.Ecosystem.Config
{
    public readonly struct ConfigFieldLangText
    {
        public string TitleEn { get; init; }
        public string DescEn { get; init; }
        public string TitleRu { get; init; }
        public string DescRu { get; init; }
    }

    /// <summary>Builds default en/ru titles and higher/lower/on/off descriptions for config UI lang keys.</summary>
    public static class ConfigFieldLangBuilder
    {
        static readonly Regex EnablePrefix = new("^Enable(.+)$", RegexOptions.Compiled);
        static readonly Regex UsePrefix = new("^Use(.+)$", RegexOptions.Compiled);
        static readonly Regex ApplyPrefix = new("^Apply(.+)$", RegexOptions.Compiled);
        static readonly Regex PreferPrefix = new("^Prefer(.+)$", RegexOptions.Compiled);
        static readonly Regex RespectPrefix = new("^Respect(.+)$", RegexOptions.Compiled);
        static readonly Regex ClonePrefix = new("^Clone(.+)$", RegexOptions.Compiled);

        public static string SplitCamelPublic(string name) => SplitCamel(name);

        public static ConfigFieldLangText Build(EcosystemConfigFieldDescriptor field)
        {
            if (field == null) return default;

            string name = field.Name;
            if (ConfigFieldDescriptions.TryGet(name, out (string descEn, string descRu) handTuned))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = ConfigFieldTitles.En(name),
                    DescEn = handTuned.descEn,
                    TitleRu = ConfigFieldTitles.Ru(name),
                    DescRu = handTuned.descRu,
                };
            }

            ConfigFieldLangText core = BuildCore(field);
            return new ConfigFieldLangText
            {
                TitleEn = ConfigFieldTitles.En(name),
                DescEn = core.DescEn,
                TitleRu = ConfigFieldTitles.Ru(name),
                DescRu = core.DescRu,
            };
        }

        static ConfigFieldLangText BuildCore(EcosystemConfigFieldDescriptor field)
        {
            if (field == null) return default;

            string name = field.Name;
            if (name == nameof(EcosystemConfig.BalancePreset))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Balance preset",
                    DescEn = "natural/lush/sparse apply bundled spread values. custom keeps manual spread tuning across restarts.",
                    TitleRu = "Пресет баланса",
                    DescRu = "natural/lush/sparse задают набор spread. custom сохраняет ваши ручные значения между перезапусками.",
                };
            }

            if (name == nameof(EcosystemConfig.EcosystemEnabled))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Ecosystem enabled",
                    DescEn = "On: spread, competition, stress, and most ecology ticks run. Off: mod ecology idle.",
                    TitleRu = "Экосистема включена",
                    DescRu = "Вкл.: spread, конкуренция, стресс и тики работают. Выкл.: экология простаивает.",
                };
            }

            if (name == nameof(EcosystemConfig.HarshWildPlants))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Harsh wild plants",
                    DescEn = "On: species climate and soil bounds apply (wrong niche builds stress). Off: softer survival.",
                    TitleRu = "Строгий климат",
                    DescRu = "Вкл.: границы климата и почвы вида (не та ниша — стресс). Выкл.: мягче выживание.",
                };
            }

            if (name == nameof(EcosystemConfig.OnlyActivateNearPlayers))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Only near players (playtest)",
                    DescEn = "On: spread, stress, trees, and chunk scans only within player radius (~192 blocks). Off: all loaded chunks.",
                    TitleRu = "Только у игроков (playtest)",
                    DescRu = "Вкл.: spread, стресс, деревья и сканы только в радиусе игрока (~192 блока). Выкл.: все загруженные чанки.",
                };
            }

            if (name == nameof(EcosystemConfig.LimitSpreadNearPlayers))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Limit spread near players",
                    DescEn = "On: spread, stress, and tree aging only near players; chunk registration unchanged. Off: full simulation in loaded chunks.",
                    TitleRu = "Spread только у игроков",
                    DescRu = "Вкл.: spread, стресс и деревья у игроков; регистрация чанков без изменений. Выкл.: полная симуляция.",
                };
            }

            if (name == nameof(EcosystemConfig.FoliageSyncMode))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Foliage sync mode",
                    DescEn = "chunk = sync on chunk load (default). hybrid = chunk plus random tick. random = legacy random only.",
                    TitleRu = "Режим синхронизации листвы",
                    DescRu = "chunk = при загрузке чанка (по умолчанию). hybrid = chunk плюс случайный тик. random = только legacy.",
                };
            }

            if (name == nameof(EcosystemConfig.VerboseLogging))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Verbose logging",
                    DescEn = "On: extra notification and warning logs (CPU cost). Off: errors and startup only.",
                    TitleRu = "Подробные логи",
                    DescRu = "Вкл.: больше notification/warning (нагрузка на CPU). Выкл.: только ошибки и старт.",
                };
            }

            if (name == nameof(EcosystemConfig.ReproduceDebug))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Spread debug log",
                    DescEn = "On: log spread attempts (pair with VerboseLogging). Off: silent spread path.",
                    TitleRu = "Лог spread (debug)",
                    DescRu = "Вкл.: логировать попытки spread (вместе с VerboseLogging). Выкл.: без лога.",
                };
            }

            if (name == nameof(EcosystemConfig.EcologyWakeRadiusBlocks))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Ecology wake radius",
                    DescEn = "0 = auto from spread radius and spacing. Higher: wake more neighbors when blocks change.",
                    TitleRu = "Радиус пробуждения",
                    DescRu = "0 = авто от радиуса spread и spacing. Больше: больше соседей просыпается при изменениях блоков.",
                };
            }

            if (name == nameof(EcosystemConfig.BerryTraitMutationChance))
            {
                return new ConfigFieldLangText
                {
                    TitleEn = "Berry trait mutation",
                    DescEn = "Higher: offspring more often lose one random trait on spread. 0 = no mutations.",
                    TitleRu = "Мутация черт ягод",
                    DescRu = "Больше: чаще теряется случайная черта при spread. 0 = без мутаций.",
                };
            }

            Match m = EnablePrefix.Match(name);
            if (m.Success)
            {
                return BoolOnOff(
                    $"On: {ConfigFieldTitles.En(name).ToLowerInvariant()}. Off: disabled (less simulation work where applicable).",
                    $"Вкл.: {ConfigFieldTitles.Ru(name).ToLowerInvariant()}. Выкл.: отключено (меньше нагрузки, где применимо).");
            }

            m = UsePrefix.Match(name);
            if (m.Success)
            {
                return BoolOnOff(
                    $"On: uses {ConfigFieldTitles.En(name).Replace("Use ", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant()} in spread and survival scoring. Off: ignores that layer.",
                    $"Вкл.: учитывает {ConfigFieldTitles.Ru(name).ToLowerInvariant()} в spread и выживании. Выкл.: слой игнорируется.");
            }

            if (name == nameof(EcosystemConfig.ApplyCrossHabitatSpacing))
            {
                return new ConfigFieldLangText
                {
                    DescEn = "On: terrestrial and aquatic plants share spacing rules. Off: spacing checked only within the same habitat.",
                    DescRu = "Вкл.: наземные и водные растения учитывают spacing друг друга. Выкл.: spacing только внутри одной среды.",
                };
            }

            if (name == nameof(EcosystemConfig.ApplyWorldgenRainForest))
            {
                return new ConfigFieldLangText
                {
                    DescEn = "On: spread fitness uses worldgen rainfall map. Off: rainfall gate ignored (forest cover still uses neighbor trees).",
                    DescRu = "Вкл.: fitness spread учитывает карту worldgen-осадков. Выкл.: осадки игнорируются (лесность — по соседним деревьям).",
                };
            }

            m = PreferPrefix.Match(name);
            if (m.Success)
            {
                return BoolOnOff(
                    "On: prefers empty cells when spreading (gap colonization). Off: empty and occupied weighted more evenly.",
                    "Вкл.: при spread приоритет пустым клеткам. Выкл.: пустые и занятые ближе по весу.");
            }

            m = RespectPrefix.Match(name);
            if (m.Success)
            {
                return BoolOnOff(
                    "On: ecology respects protected claims (no spread, stress, or trees inside). Off: claims ignored.",
                    "Вкл.: экология не трогает защищённые участки. Выкл.: участки игнорируются.");
            }

            m = ClonePrefix.Match(name);
            if (m.Success)
            {
                return BoolOnOff(
                    "On: offspring copies parent bush genetics. Off: vanilla random wild traits on new bushes.",
                    "Вкл.: потомство копирует гены родителя. Выкл.: ванильные случайные черты.");
            }

            if (name == nameof(EcosystemConfig.RhizomeSeedDispersalEnabled))
            {
                return BoolOnOff(
                    "On: rare seed/fragment jumps for reed and lily mats. Off: mat-edge spread only.",
                    "Вкл.: редкие прыжки семян/фрагментов у тростника и кувшинки. Выкл.: только mat-edge spread.");
            }

            if (name == nameof(EcosystemConfig.PlantSpacingEnabled))
            {
                return BoolOnOff(
                    "On: enforce Chebyshev spacing between spread plants. Off: spacing rules ignored.",
                    "Вкл.: Chebyshev spacing между растениями. Выкл.: spacing не проверяется.");
            }

            if (name == nameof(EcosystemConfig.StaggerReproduceAttempts))
            {
                return BoolOnOff(
                    "On: random initial delay on registration to spread tick load. Off: all plants tick together.",
                    "Вкл.: случайная задержка при регистрации размазывает нагрузку. Выкл.: все тикают синхронно.");
            }

            if (name == nameof(EcosystemConfig.CanopyAmbienceSuppressInRain))
            {
                return BoolOnOff(
                    "On: suppress canopy particles during heavy rain. Off: particles still spawn in rain.",
                    "Вкл.: частицы под кроной гасятся в сильный дождь. Выкл.: частицы и в дождь.");
            }

            if (name == nameof(EcosystemConfig.FoliageCatchUpOnChunkLoad))
            {
                return BoolOnOff(
                    "On: sync foliage to current season when chunk loads. Off: foliage may lag until random tick.",
                    "Вкл.: листва догоняет сезон при загрузке чанка. Выкл.: может отставать до random tick.");
            }

            if (name == nameof(EcosystemConfig.FoliageRestoreBareSkeleton))
            {
                return BoolOnOff(
                    "On: winter repair adds branchy leaves on bare log-grown pillars. Off: bare crowns stay bare.",
                    "Вкл.: зимой восстанавливает branchy на голых log-grown. Выкл.: голые кроны остаются голыми.");
            }

            if (name == nameof(EcosystemConfig.TramplingSoilDegradation))
            {
                return BoolOnOff(
                    "On: degrade soil when a plant dies from trampling. Off: trampling kills plants only.",
                    "Вкл.: почва деградирует при гибели от протаптывания. Выкл.: только гибель растения.");
            }

            if (name == nameof(EcosystemConfig.MyceliumSkipSoilSuccession))
            {
                return BoolOnOff(
                    "On: no soil succession on mycelium anchor cells. Off: anchors can change soil like other plants.",
                    "Вкл.: смена почвы на якорях грибницы отключена. Выкл.: якоря участвуют в succession.");
            }

            if (name == nameof(EcosystemConfig.SeasonalStressEnabled))
            {
                return BoolOnOff(
                    "On: seasonal stress die-off rolls for terrestrial plants. Off: no extra seasonal die-off.",
                    "Вкл.: сезонные броски гибели от стресса (наземные). Выкл.: без сезонной гибели.");
            }

            string title = SplitCamel(name);

            if (name == nameof(EcosystemConfig.MaxFailedSurvivalChecks))
            {
                return Numeric(title,
                    "Higher: more failed checks tolerated before removal. Lower: plants die sooner from stress.",
                    "Больше: больше неудач до удаления. Меньше: быстрее гибель от стресса.");
            }

            if (name.EndsWith("Rate", StringComparison.Ordinal)
                && !name.EndsWith("SpreadRate", StringComparison.Ordinal))
            {
                return Numeric(title,
                    "Higher: faster or denser effect. Lower: slower or sparser. 1.0 is default tuning.",
                    "Больше: быстрее или гуще. Меньше: медленнее или реже. 1.0 — дефолт.");
            }

            if (name == nameof(EcosystemConfig.MyceliumSpreadRate))
            {
                return Numeric(title,
                    "Higher: faster mycelium network spread. Lower: slower network colonization.",
                    "Больше: быстрее сетевой spread грибницы. Меньше: медленнее колонизация.");
            }

            if (name == nameof(EcosystemConfig.CanopyBudMinTemperature))
            {
                return Numeric(title,
                    "Higher: spring buds need warmer cells (later/shorter bud season). Lower: buds in cooler weather.",
                    "Больше: почки только в более тёплых клетках (короче сезон). Меньше: почки в прохладнее.");
            }

            if (name == nameof(EcosystemConfig.FlowerBloomMinTemperature))
            {
                return Numeric(title,
                    "Higher: flowers need warmer weather to bloom (shorter bloom window). Lower: bloom in cooler cells.",
                    "Больше: цветение только в более тёплой погоде. Меньше: цветение в прохладнее.");
            }

            if (name == nameof(EcosystemConfig.FlowerBloomMaxTemperature))
            {
                return Numeric(title,
                    "Higher: tolerate hotter summers before dieback. Lower: earlier heat dieback.",
                    "Больше: выдерживают более жаркое лето. Меньше: раньше отмирание от жары.");
            }

            if (name == nameof(EcosystemConfig.FlowerBloomEnergyThreshold))
            {
                return Numeric(title,
                    "Higher: longer vegetative wait before bloom. Lower: faster bloom after season opens.",
                    "Больше: дольше вегетация до цветения. Меньше: быстрее вход в цветение.");
            }

            if (name == nameof(EcosystemConfig.FlowerPhenologyEnergyGainPerDay))
            {
                return Numeric(title,
                    "Higher: faster vegetative energy buildup. Lower: slower path to bloom.",
                    "Больше: быстрее накопление энергии. Меньше: медленнее путь к цветению.");
            }

            if (name == nameof(EcosystemConfig.MaxFlowerPhenologyChecksPerTick))
            {
                return Numeric(title,
                    "Higher: more flower phase updates per tick. Lower: slower phenology pacing.",
                    "Больше: больше обновлений фаз за тик. Меньше: медленнее фенология.");
            }

            if (name == nameof(EcosystemConfig.CanopyLatitudeInfluence))
            {
                return Numeric(title,
                    "Higher: stronger polar slowdown of canopy seasons. Lower: less latitude effect. 0 = off.",
                    "Больше: сильнее полярное замедление сезонов кроны. Меньше: слабее. 0 = выкл.");
            }

            if (name == nameof(EcosystemConfig.FoliageColumnScanHeightAboveSurface))
            {
                return Numeric(title,
                    "Higher: scan fewer blocks above surface (less work). 0 = full column height.",
                    "Больше: скан выше поверхности уже (меньше работы). 0 = вся колонка.");
            }

            if (name == nameof(EcosystemConfig.FerntreeSenescenceSnagSegments)
                || name == nameof(EcosystemConfig.TreeSenescenceSnagBlocks)
                || name == nameof(EcosystemConfig.TreeSenescenceFallenLogCount))
            {
                return Numeric(title,
                    "Higher: more snag segments or fallen logs on senescence. Lower: smaller remains.",
                    "Больше: больше сухостоя или брёвен при гибели. Меньше: скромнее останки.");
            }

            if (name == nameof(EcosystemConfig.SpringBranchyAgeBoostYearsToMax))
            {
                return Numeric(title,
                    "Higher: older trees needed for max spring branchy boost. Lower: young trees reach max boost sooner.",
                    "Больше: для max boost нужны более старые деревья. Меньше: молодые быстрее получают max.");
            }

            if (name.EndsWith("Chance", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher (toward 1): more likely. Lower (toward 0): rarer.",
                    "Больше (к 1): чаще срабатывает. Меньше (к 0): реже.");
            }

            if (name.EndsWith("Fitness", StringComparison.Ordinal) || name.EndsWith("MinFitness", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: pickier placement (fewer weak sites). Lower: colonizes marginal cells more often.",
                    "Больше: избирательнее (меньше слабых клеток). Меньше: чаще на слабых клетках.");
            }

            if (name.Contains("Cover", StringComparison.Ordinal))
            {
                return Numeric(title,
                    "Higher: stricter forest-cover gate. Lower: easier to pass the cover check.",
                    "Больше: строже порог по лесности. Меньше: легче пройти проверку.");
            }

            if (name.EndsWith("Penalty", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: stronger penalty (slower spread there). Lower: milder penalty.",
                    "Больше: сильнее штраф (медленнее spread). Меньше: слабее штраф.");
            }

            if (name.EndsWith("Bonus", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: stronger bonus (faster spread there). Lower: weaker bonus.",
                    "Больше: сильнее бонус (быстрее spread). Меньше: слабее бонус.");
            }

            if (name.Contains("Multiplier", StringComparison.Ordinal)
                || name.Contains("Scale", StringComparison.Ordinal)
                || name.Contains("Strength", StringComparison.Ordinal)
                || name.Contains("Activity", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: stronger or faster effect. Lower: subtler or slower. 1.0 is default tuning.",
                    "Больше: сильнее или быстрее. Меньше: слабее или медленнее. 1.0 — дефолт.");
            }

            if (name.EndsWith("Radius", StringComparison.Ordinal) || name.EndsWith("Blocks", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: larger area included. Lower: tighter, more local effect.",
                    "Больше: шире зона. Меньше: локальнее.");
            }

            if (name.EndsWith("PerTick", StringComparison.Ordinal)
                || (name.StartsWith("Max", StringComparison.Ordinal)
                    && (name.Contains("Attempts", StringComparison.Ordinal)
                        || name.Contains("Checks", StringComparison.Ordinal)
                        || name.Contains("Registrations", StringComparison.Ordinal)
                        || name.Contains("Applies", StringComparison.Ordinal)
                        || name.Contains("Commits", StringComparison.Ordinal)
                        || name.Contains("Scans", StringComparison.Ordinal)
                        || name.Contains("Columns", StringComparison.Ordinal)
                        || name.Contains("Cells", StringComparison.Ordinal)
                        || name.Contains("Growth", StringComparison.Ordinal)
                        || name.Contains("CatchUp", StringComparison.Ordinal)
                        || name.Contains("Work", StringComparison.Ordinal))))
            {
                return Numeric(title, "Higher: more work per tick (faster catch-up, higher CPU). Lower: gentler pacing.",
                    "Больше: больше работы за тик (быстрее догоняет, выше CPU). Меньше: мягче темп.");
            }

            if (name.EndsWith("BudgetMs", StringComparison.Ordinal)
                || name == nameof(EcosystemConfig.TickBudgetMs)
                || name == nameof(EcosystemConfig.SpreadBudgetMs)
                || name == nameof(EcosystemConfig.RegistrationBudgetMs)
                || name == nameof(EcosystemConfig.StressBudgetMs))
            {
                return Numeric(title, "Higher: more milliseconds allowed per tick (smoother sim, more CPU). 0 = use linked budget or unlimited alias.",
                    "Больше: больше мс за тик (ровнее, выше CPU). 0 = связанный бюджет или без лимита.");
            }

            if (name.EndsWith("IntervalMs", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: less frequent ticks (less CPU, slower reactions). Lower: more frequent ticks.",
                    "Больше: реже тики (меньше CPU). Меньше: чаще тики.");
            }

            if (name.EndsWith("IntervalHours", StringComparison.Ordinal)
                || name.EndsWith("RecheckHours", StringComparison.Ordinal)
                || name.EndsWith("CacheHours", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: slower rechecks or longer cache (less CPU). Lower: faster updates.",
                    "Больше: реже пересчёт или дольше кэш. Меньше: чаще обновления.");
            }

            if (name.EndsWith("Seconds", StringComparison.Ordinal) || name.EndsWith("CooldownSeconds", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: longer delay between actions. Lower: more responsive.",
                    "Больше: длиннее пауза. Меньше: отзывчивее.");
            }

            if (name.EndsWith("AttemptsPerYear", StringComparison.Ordinal) || name.EndsWith("PerYear", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: more attempts each in-game year (denser flora). Lower: sparser spread.",
                    "Больше: больше попыток за игровой год (густее). Меньше: реже распространение.");
            }

            if (name.EndsWith("Threshold", StringComparison.Ordinal) || name.EndsWith("Margin", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: stricter gate (harder to pass). Lower: easier to pass.",
                    "Больше: строже порог. Меньше: легче пройти.");
            }

            if (name.EndsWith("Spacing", StringComparison.Ordinal))
            {
                return Numeric(title, "Higher: plants must stay farther apart (patchier meadows). Lower: denser stands allowed.",
                    "Больше: дальше друг от друга (пятнистее). Меньше: плотнее можно.");
            }

            switch (field.Kind)
            {
                case ConfigFieldKind.Boolean:
                    return BoolOnOff(
                        $"On: {ConfigFieldTitles.En(name).ToLowerInvariant()}. Off: disabled.",
                        $"Вкл.: {ConfigFieldTitles.Ru(name).ToLowerInvariant()}. Выкл.: отключено.");
                case ConfigFieldKind.String:
                    return new ConfigFieldLangText
                    {
                        DescEn = "Select mode or preset. Each value changes the algorithm — see handbook.",
                        DescRu = "Выбор режима или пресета. Каждое значение меняет алгоритм — см. справочник.",
                    };
                default:
                    return Numeric(title, "Higher: stronger or more frequent. Lower: weaker or more limited.",
                        "Больше: сильнее или чаще. Меньше: слабее или уже.");
            }
        }

        public static Dictionary<string, string> BuildLangFile(string locale)
        {
            bool ru = locale == "ru";
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (EcosystemConfigFieldDescriptor field in EcosystemConfigSchema.Fields)
            {
                ConfigFieldLangText text = Build(field);
                dict[$"ecosystemflora:config-field-{field.Name}"] = ru ? text.TitleRu : text.TitleEn;
                dict[$"ecosystemflora:config-field-{field.Name}-desc"] = ru ? text.DescRu : text.DescEn;
            }

            dict["ecosystemflora:config-field-FoliageSyncMode-val-chunk"] = ru ? "Синхронизация участков" : "Chunk sync";
            dict["ecosystemflora:config-field-FoliageSyncMode-val-hybrid"] = ru ? "Гибрид" : "Hybrid";
            dict["ecosystemflora:config-field-FoliageSyncMode-val-random"] = ru ? "Случайный тик" : "Random tick";
            return dict;
        }

        static ConfigFieldLangText BoolOnOff(string descEn, string descRu) =>
            new ConfigFieldLangText { DescEn = descEn, DescRu = descRu };

        static ConfigFieldLangText Numeric(string title, string descEn, string descRu) =>
            new ConfigFieldLangText { DescEn = descEn, DescRu = descRu };

        static string SplitCamel(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var sb = new StringBuilder(name.Length + 8);
            sb.Append(name[0]);
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c) && !char.IsUpper(name[i - 1])) sb.Append(' ');
                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
