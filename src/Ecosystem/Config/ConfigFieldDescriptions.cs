using System;
using System.Collections.Generic;

// Export-only C# tables: fallback when SpeciesEcologyRegistry is not loaded.
#pragma warning disable CS0618

namespace WildFarming.Ecosystem.Config
{
    /// <summary>Hand-tuned en/ru descriptions (with On/Off or Higher/Lower hints) for config UI lang keys.</summary>
    internal static class ConfigFieldDescriptions
    {
        static readonly Dictionary<string, (string en, string ru)> Map = BuildMap();

        public static bool TryGet(string name, out (string en, string ru) desc) =>
            Map.TryGetValue(name, out desc);

        static Dictionary<string, (string en, string ru)> BuildMap()
        {
            var m = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            void D(string n, string en, string ru) => m[n] = (en, ru);

            D(nameof(EcosystemConfig.BalancePreset),
                "natural (default) = full ecology features on + balanced spread. lush/sparse retune spread density. vanilla-minimal = natural without juvenile/phenology blocks. timelapse = stress/test. custom keeps manual values across restarts.",
                "natural (дефолт) = все фичи экологии вкл. + сбалансированный spread. lush/sparse — плотность. vanilla-minimal = natural без ювенильных/фенологии. timelapse = стресс-тест. custom сохраняет ручные значения.");

            D(nameof(EcosystemConfig.EcosystemEnabled),
                "On: spread, competition, stress, and most ecology ticks run. Off: mod ecology idle.",
                "Вкл.: распространение, конкуренция, стресс и тики работают. Выкл.: экология мода простаивает.");

            D(nameof(EcosystemConfig.HarshWildPlants),
                "On: species climate and soil bounds apply (wrong niche builds stress). Off: softer survival checks.",
                "Вкл.: границы климата и почвы вида (не та ниша — стресс). Выкл.: мягче проверки выживания.");

            D(nameof(EcosystemConfig.ApplyWorldgenRainForest),
                "On: spread fitness uses worldgen rainfall map. Off: rainfall gate ignored (forest cover still uses neighbor trees).",
                "Вкл.: пригодность распространения учитывает карту осадков генерации мира. Выкл.: осадки игнорируются (лесность — по соседним деревьям).");

            D(nameof(EcosystemConfig.ReproduceRadius),
                "Higher: wider horizontal search for spread targets. Lower: tighter, more local colonization.",
                "Больше: шире горизонтальный поиск клеток для распространения. Меньше: локальнее колонизация.");

            D(nameof(EcosystemConfig.ReproduceVerticalSearch),
                "Higher: search farther above/below for valid surface. Lower: narrower vertical placement window.",
                "Больше: шире поиск поверхности вверх/вниз. Меньше: уже окно размещения.");

            D(nameof(EcosystemConfig.ReproduceChance),
                "Higher (toward 1): more spread attempts succeed. Lower (toward 0): rarer successful placement.",
                "Больше (к 1): чаще успешное распространение. Меньше (к 0): реже успешное размещение.");

            D(nameof(EcosystemConfig.MinFitness),
                "Higher: pickier offspring placement (fewer marginal sites). Lower: colonizes weaker cells more often.",
                "Больше: избирательнее размещение (меньше слабых клеток). Меньше: чаще на слабых клетках.");

            D(nameof(EcosystemConfig.ReproduceIntervalHours),
                "Higher: longer wait between spread attempts (legacy mode). Lower: more frequent attempts.",
                "Больше: длиннее пауза между попытками (прежний режим). Меньше: чаще попытки.");

            D(nameof(EcosystemConfig.ReproduceAttemptsPerYear),
                "Higher: more spread tries per in-game year at species rate 1 (denser flora). Lower: sparser spread.",
                "Больше: больше попыток распространения за игровой год при скорости 1 (густее). Меньше: реже распространение.");

            D(nameof(EcosystemConfig.UseCalendarScaledSpread),
                "On: intervals scale from DaysPerYear/HoursPerDay. Off: use ReproduceIntervalHours only.",
                "Вкл.: интервалы от календаря мира. Выкл.: только интервал в часах.");

            D(nameof(EcosystemConfig.UseSpeciesSpreadRates),
                "On: per-species SpreadRate scales interval and chance. Off: uniform spread pacing.",
                "Вкл.: скорость вида масштабирует интервал и шанс. Выкл.: единый темп распространения.");

            D(nameof(EcosystemConfig.SpeciesSpreadRateScale),
                "Higher: faster wild spread vs ecology tables (1 = table rates). Lower: slower reproduction for all species.",
                "Больше: быстрее распространение относительно таблиц (1 = как в таблицах). Меньше: медленнее для всех видов.");

            D(nameof(EcosystemConfig.MinSpeciesReproduceIntervalDays),
                "Higher: minimum days between attempts per species (calendar mode). 0 = no floor.",
                "Больше: минимум дней между попытками вида (календарь). 0 = без нижней границы.");

            D(nameof(EcosystemConfig.MinSpeciesReproduceIntervalHours),
                "Higher: minimum hours between attempts (legacy mode only). 0 = no floor.",
                "Больше: минимум часов между попытками (только прежний режим). 0 = без нижней границы.");

            D(nameof(EcosystemConfig.MaxFailedSurvivalChecks),
                "Higher: more failed checks tolerated before stress removal. Lower: plants die sooner from stress.",
                "Больше: больше неудач до удаления от стресса. Меньше: быстрее гибель.");

            D(nameof(EcosystemConfig.GrowthHoursMultiplier),
                "Higher: spread flower seedlings and tallgrass stages mature faster. Lower: longer establishing phase.",
                "Больше: быстрее взрослеют цветы и трава. Меньше: дольше ждать.");

            D(nameof(EcosystemConfig.EnableFlowerSpreadMaturation),
                "On: meadow colonizer flowers spread as small seedlings, then mature. Off: instant adult plant.",
                "Вкл.: новый цветок маленький, потом взрослеет. Выкл.: сразу взрослый.");

            D(nameof(EcosystemConfig.EnableFlowerSpreadAttemptCooldown),
                "On: parent flower waits after each spread attempt before retry. Off: only calendar spread interval applies.",
                "Вкл.: родитель ждёт после каждой попытки распространения. Выкл.: только обычный интервал распространения.");

            D(nameof(EcosystemConfig.FlowerSpreadCooldownHoursMultiplier),
                "Higher: shorter post-spread pause on parent flowers. Lower: longer cooldown between offspring.",
                "Больше: короче пауза после распространения. Меньше: дольше между попытками.");

            D(nameof(EcosystemConfig.MaxPendingFlowerMaturationChecksPerTick),
                "Higher: more maturation checks per game tick. Lower: slower maturation queue.",
                "Больше: быстрее очередь созревания. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableFlowerPhenology),
                "On: meadow flowers follow simulated phases (dormant/vegetative/bloom/dieback). Spread only in bloom; block appearance follows phase.",
                "Вкл.: фазы цветов (покой/вегетация/цветение/отмирание). Распространение только в цветении; вид блока = фаза.");

            D(nameof(EcosystemConfig.FlowerBloomMinTemperature),
                "Higher: flowers need warmer weather to bloom (shorter bloom window). Lower: bloom in cooler cells.",
                "Больше: цветение только в более тёплой погоде. Меньше: цветение в прохладнее.");

            D(nameof(EcosystemConfig.FlowerBloomMaxTemperature),
                "Higher: tolerate hotter summers before dieback. Lower: earlier heat dieback.",
                "Больше: выдерживают более жаркое лето. Меньше: раньше отмирание от жары.");

            D(nameof(EcosystemConfig.FlowerBloomEnergyThreshold),
                "Higher: longer vegetative wait before bloom. Lower: faster bloom after season opens.",
                "Больше: дольше вегетация до цветения. Меньше: быстрее вход в цветение.");

            D(nameof(EcosystemConfig.FlowerPhenologyEnergyGainPerDay),
                "Higher: faster vegetative energy buildup. Lower: slower path to bloom.",
                "Больше: быстрее накопление энергии. Меньше: медленнее путь к цветению.");

            D(nameof(EcosystemConfig.FlowerPhenologyStressEnterDieback),
                "Higher: need more accumulated stress before dieback (smoother, less flicker). Lower: dieback sooner.",
                "Больше: нужно больше накопленного стресса до dieback (меньше мигания). Меньше: dieback раньше.");

            D(nameof(EcosystemConfig.FlowerPhenologyStressExitDieback),
                "Higher: must recover further from stress before leaving dieback/dormant. Lower: quicker green-up.",
                "Больше: дольше сбрасывать стресс перед выходом из dieback. Меньше: быстрее зеленеют.");

            D(nameof(EcosystemConfig.FlowerPhenologyColdStressGainPerDay),
                "Frost (below bloom min °C) and winter season share this rate — hard freezes pack winter-class debt.",
                "Заморозки (ниже мин. °C цветения) и зима копят стресс с одной скоростью — жёсткий мороз ≈ зимний долг.");

            D(nameof(EcosystemConfig.FlowerPhenologyHeatStressGainPerDay),
                "Higher: heat waves push dieback faster. Lower: more heat-tolerant stands.",
                "Больше: жара быстрее толкает в dieback. Меньше: терпимее к жаре.");

            D(nameof(EcosystemConfig.FlowerPhenologySeasonExitStressGainPerDay),
                "Higher: post-bloom / energy collapse enters dieback sooner. Lower: softer seasonal fade.",
                "Больше: после цветения быстрее уходят в dieback. Меньше: мягче сезонный спад.");

            D(nameof(EcosystemConfig.FlowerPhenologyStressDecayPerDay),
                "Higher: good weather clears stress faster (easier recovery). Lower: longer dieback hangover.",
                "Больше: хорошая погода быстрее снимает стресс. Меньше: дольше «после dieback».");

            D(nameof(EcosystemConfig.MaxFlowerPhenologyLifeCycles),
                "Fallback dieback entries before senescence when ecology.csv omits flower_phenology_life_cycles. 0 = unlimited.",
                "Запасной лимит входов в dieback, если в ecology.csv нет flower_phenology_life_cycles. 0 = без лимита.");

            D(nameof(EcosystemConfig.MaxFlowerPhenologyChecksPerTick),
                "Higher: more flower phase updates per tick. Lower: slower phenology pacing.",
                "Больше: больше обновлений фаз за тик. Меньше: медленнее фенология.");

            D(nameof(EcosystemConfig.EnableTallgrassSpreadMaturation),
                "On: spread places very low grass; mod raises to short on a timer before spread. Off: height chosen at spread.",
                "Вкл.: новая трава очень низкая, мод доращивает до низкой, потом размножается. Выкл.: высота сразу по месту.");

            D(nameof(EcosystemConfig.MaxPendingTallgrassPromotionChecksPerTick),
                "Higher: more grass establishment checks per game tick. Lower: slower promotion queue.",
                "Больше: быстрее ждёт рост. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableFernRhizomeSpread),
                "On: ground ferns spread one orthogonal step from patch edge (rhizome). Off: legacy radius spread.",
                "Вкл.: папоротники ползут по краю клумбы (ризома). Выкл.: распространение по радиусу.");

            D(nameof(EcosystemConfig.EnableBerryColonySpread),
                "On: wild berries spread one mat step from colony edge (rhizome, suckers, runners); seed jumps use RhizomeSeedDispersal settings. Off: legacy radius spread.",
                "Вкл.: ягоды ползут с кромки колонии (ризома, поросль, усы); скачки семян — настройки скачки семян ризомы. Выкл.: распространение по радиусу.");

            D(nameof(EcosystemConfig.EnableShoreSedgeMatSpread),
                "On: brown sedge spreads one block at mat edge (slow clump rhizome). Off: legacy radius spread. No seed jumps by default.",
                "Вкл.: осока — 1 клетка с кромки куртины (медленное корневище). Выкл.: радиус. Без прыжков семян.");

            D(nameof(EcosystemConfig.EnableFernSpreadMaturation),
                "On: spread places small juvenile ferns that mature before reproducing. Off: instant adult.",
                "Вкл.: распространение ставит молодой папоротник, потом взрослеет. Выкл.: сразу взрослый.");

            D(nameof(EcosystemConfig.EnableFernSpreadAttemptCooldown),
                "On: parent fern waits after each spread attempt. Off: only calendar interval applies.",
                "Вкл.: пауза родителя после каждой попытки распространения. Выкл.: только календарный интервал.");

            D(nameof(EcosystemConfig.FernSpreadCooldownHoursMultiplier),
                "Higher: shorter post-spread pause on parent ferns. Lower: longer cooldown.",
                "Больше: короче пауза после распространения. Меньше: дольше между попытками.");

            D(nameof(EcosystemConfig.EnableFernSporulationGate),
                "On: ferns spread only during active sporulation season (FernSeason curve). Off: year-round when fitness allows.",
                "Вкл.: распространение только в сезон споруляции. Выкл.: круглый год при подходящей пригодности.");

            D(nameof(EcosystemConfig.MaxPendingFernMaturationChecksPerTick),
                "Higher: more fern maturation checks per tick. Lower: slower juvenile queue.",
                "Больше: быстрее очередь созревания папоротников. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableFernPhenology),
                "On: fern dormant/sporulating/dieback phase blocks and spread gates. Off: text-only sporulation gate.",
                "Вкл.: фазы папоротника (покой/споруляция/отмирание) и блоки. Выкл.: только текстовая споруляция.");

            D(nameof(EcosystemConfig.MaxFernPhenologyChecksPerTick),
                "Higher: more fern phenology updates per tick. Lower: slower phase sync.",
                "Больше: быстрее синхронизация фаз папоротника. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableTallgrassPhenology),
                "On: tallgrass and brown sedge winter dormant and stress dieback visuals; spread gated off-season. Off: vanilla blocks only.",
                "Вкл.: зимний покой и отмирание высокой травы, распространение по сезону. Выкл.: только ванильная высота.");

            D(nameof(EcosystemConfig.MaxTallgrassPhenologyChecksPerTick),
                "Higher: more tallgrass phenology updates per tick. Lower: slower phase sync.",
                "Больше: быстрее синхронизация фаз высокой травы. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableBerrySpreadMaturation),
                "On: spread berry bushes reset to cutting state and register when mature. Off: immediate registry.",
                "Вкл.: распространение ягод как черенок до созревания. Выкл.: сразу в реестр.");

            D(nameof(EcosystemConfig.MaxPendingBerryMaturationChecksPerTick),
                "Higher: more berry maturation checks per tick. Lower: slower spread bush queue.",
                "Больше: быстрее очередь созревания ягод. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableStumpDecay),
                "On: senescent snag stumps decay and remove after calendar years. Off: stumps persist.",
                "Вкл.: пни после старости исчезают через N лет. Выкл.: пни остаются.");

            D(nameof(EcosystemConfig.StumpDecayYears),
                "Higher: stumps linger longer. Lower: faster stump removal.",
                "Больше: пни дольше стоят. Меньше: быстрее исчезают.");

            D(nameof(EcosystemConfig.MaxStumpDecayChecksPerTick),
                "Higher: more stump decay checks per tick. Lower: slower stump queue.",
                "Больше: быстрее обработка очереди пней. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableEcologyHistoryHint),
                "On: inspect (I) includes recent ecology events at the bottom of the report. Off: no history lines.",
                "Вкл.: осмотр (I) показывает недавние события экологии внизу отчёта. Выкл.: без строк истории.");

            D(nameof(EcosystemConfig.EventWakeRetryHours),
                "Higher: event wake pulls spread retry farther forward (after spawn cooldown). Lower: subtler wake nudge.",
                "Больше: пробуждение по событиям сильнее сдвигает повтор распространения (после паузы). Меньше: слабее пробуждение.");

            D(nameof(EcosystemConfig.StaggerReproduceAttempts),
                "On: random initial delay on registration spreads tick load. Off: all plants tick together.",
                "Вкл.: случайная задержка при регистрации размазывает нагрузку. Выкл.: синхронные тики.");

            D(nameof(EcosystemConfig.UseRhizomeSpreadForReeds),
                "On: cattail/tule/papyrus use mat-edge rhizome spread. Off: legacy radius spread.",
                "Вкл.: тростник/папирус — распространение с кромки ковра. Выкл.: прежнее распространение по радиусу.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalEnabled),
                "On: rare seed/fragment jumps for reed and lily mats. Off: mat-edge spread only.",
                "Вкл.: редкие прыжки семян/фрагментов у тростника и кувшинки. Выкл.: только с кромки ковра.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalChanceScale),
                "Higher: more frequent distant seed jumps. Lower: rarer jumps. 1.0 is default.",
                "Больше: чаще дальние прыжки семян. Меньше: реже. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalFitnessScale),
                "Higher: stricter fitness for distant seed landing. Lower: jumps accept weaker sites.",
                "Больше: строже пригодность для дальних прыжков. Меньше: слабее клетки допускаются.");

            D(nameof(EcosystemConfig.UseSurfaceMatSpreadForLilies),
                "On: water lily uses floating pad mat spread. Off: legacy radius spread.",
                "Вкл.: кувшинка — распространение по плавучему ковру. Выкл.: прежнее распространение по радиусу.");

            D(nameof(EcosystemConfig.PlantSpacingEnabled),
                "On: enforce Chebyshev spacing between spread plants. Off: spacing rules ignored.",
                "Вкл.: дистанция по метрике Чебышёва между растениями. Выкл.: дистанция не проверяется.");

            D(nameof(EcosystemConfig.ApplyCrossHabitatSpacing),
                "On: terrestrial and aquatic plants share spacing rules. Off: spacing only within same habitat.",
                "Вкл.: наземные и водные учитывают дистанцию друг друга. Выкл.: дистанция только внутри среды.");

            D(nameof(EcosystemConfig.DefaultSameSpeciesSpacing),
                "Fallback when a species has no same-species spacing (flowers with 0 stay patch-forming). Trees use per-wood CSV / crown size — never 0. Higher: patchier; lower: denser.",
                "Fallback, если у вида нет дистанции своего вида (у цветов 0 = пятно). Деревья — по породе в CSV / размеру кроны, не 0. Больше: реже; меньше: плотнее.");

            D(nameof(EcosystemConfig.DefaultOtherSpeciesSpacing),
                "Fallback when a species has no other-species spacing. Trees use per-wood values grounded in crown radius.",
                "Fallback, если у вида нет дистанции до других. У деревьев — свои значения от размера кроны.");

            D(nameof(EcosystemConfig.SpacingVerticalSearch),
                "Higher: check spacing conflicts farther above/below. Lower: tighter vertical spacing scan.",
                "Больше: шире вертикальная проверка дистанции. Меньше: уже обход по Y.");

            D(nameof(EcosystemConfig.UseCellDisplacement),
                "On: stronger species can displace weaker occupants. Off: no displacement on occupied cells.",
                "Вкл.: сильный вид вытесняет слабого. Выкл.: занятые клетки не вытесняются.");

            D(nameof(EcosystemConfig.DisplacementHoldMargin),
                "Higher: incumbent harder to displace (needs much stronger challenger). Lower: easier takeovers.",
                "Больше: занятый вид труднее вытеснить. Меньше: легче захват клетки.");

            D(nameof(EcosystemConfig.PreferSpreadToEmptyCells),
                "On: weight empty cells higher when spreading. Off: empty and occupied more even.",
                "Вкл.: при распространении выше вес пустых клеток. Выкл.: пустые и занятые ближе по весу.");

            D(nameof(EcosystemConfig.EnableEmptyFirstSpreadCollect),
                "On: collect empty-cell candidates before displacement pass. Off: single combined pass.",
                "Вкл.: сначала пустые клетки, потом вытеснение. Выкл.: один общий проход.");

            D(nameof(EcosystemConfig.EnableSpreadColumnOccupancyHint),
                "On: skip columns known occupied on empty-first pass (faster). Off: scan all columns.",
                "Вкл.: пропуск занятых колонок на сначала пустые (быстрее). Выкл.: обход всех колонок.");

            D(nameof(EcosystemConfig.EmptySpreadFitnessMultiplier),
                "Higher: empty cells much more attractive when prefer-empty is on. Lower: weaker empty bias.",
                "Больше: пустые клетки сильнее предпочитаются. Меньше: слабее смещение к пустым.");

            D(nameof(EcosystemConfig.UseFloraContext),
                "On: neighbor trees/logs/leaves affect spread fitness. Off: ignore forest edge/interior context.",
                "Вкл.: соседние деревья/листва влияют на пригодность. Выкл.: контекст леса игнорируется.");

            D(nameof(EcosystemConfig.FloraContextNeighborRadius),
                "Higher: count trees farther away for forest context. Lower: more local edge/interior signal.",
                "Больше: дальше считаются деревья для контекста. Меньше: локальнее сигнал опушки/чащи.");

            D(nameof(EcosystemConfig.FloraContextInteriorThreshold),
                "Higher: harder to count as forest interior (needs more neighbors). Lower: easier interior label.",
                "Больше: труднее считаться чащей (нужно больше соседей). Меньше: легче «чаща».");

            D(nameof(EcosystemConfig.FloraOpenInteriorPenalty),
                "Higher: open-field species penalized more in forest interior. Lower: milder edge effect.",
                "Больше: луговые виды сильнее штрафуются в чаще. Меньше: слабее эффект.");

            D(nameof(EcosystemConfig.FloraContextCacheHours),
                "Higher: longer cache for local forest cover (less CPU). Lower: fresher context updates.",
                "Больше: дольше кэш лесности (меньше процессор). Меньше: чаще пересчёт контекста.");

            D(nameof(EcosystemConfig.UseNicheContext),
                "On: local moisture/light niche multipliers on spread fitness. Off: ignore niche layer.",
                "Вкл.: влага/свет локальной ниши на пригодность. Выкл.: слой ниши игнорируется.");

            D(nameof(EcosystemConfig.NicheCacheHours),
                "Higher: longer per-cell niche cache. Lower: more frequent niche refresh.",
                "Больше: дольше кэш ниши на клетку. Меньше: чаще обновление ниши.");

            D(nameof(EcosystemConfig.NicheStressThreshold),
                "Higher: stricter niche gate for survival (more stress failures). Lower: more tolerant of bad niche.",
                "Больше: строже ниша для выживания (больше стресса). Меньше: терпимее к плохой нише.");

            D(nameof(EcosystemConfig.EnableStressDeath),
                "On: remove plants after repeated failed survival checks. Off: plants never removed by stress.",
                "Вкл.: удаление после серии неудачных проверок. Выкл.: стресс не убирает растения.");

            D(nameof(EcosystemConfig.StressRecheckHours),
                "Higher: slower stress evaluations per plant (less CPU). Lower: faster stress reactions.",
                "Больше: реже проверка стресса на растение. Меньше: быстрее реакция стресса.");

            D(nameof(EcosystemConfig.MaxStressChecksPerTick),
                "Higher: more stress evaluations per stress tick (faster catch-up, more CPU). Lower: gentler pacing.",
                "Больше: больше проверок стресса за тик. Меньше: мягче темп.");

            D(nameof(EcosystemConfig.EnableSymbiosis),
                "On: forest symbionts need tree hosts; orphans fade via stress death after host loss. Off: symbiosis rules off.",
                "Вкл.: симбионты леса требуют дерево-хозяина; без хозяина угасают через стресс. Выкл.: симбиоз выключен.");

            D(nameof(EcosystemConfig.SymbiosisCascadeRadius),
                "Higher: wider host-cache invalidation and ecology wake when a symbiosis host is removed. Lower: tighter radius.",
                "Больше: шире сброс кэша хозяина и пробуждение экологии. Меньше: уже радиус.");

            D(nameof(EcosystemConfig.UseSeasonalEcology),
                "On: monthly spread multipliers from WildSpeciesSeason profiles. Off: uniform spread year-round.",
                "Вкл.: месячные множители распространения по сезонным профилям. Выкл.: равномерное распространение круглый год.");

            D(nameof(EcosystemConfig.SeasonalStressEnabled),
                "On: seasonal stress die-off rolls for terrestrial plants. Off: no extra seasonal die-off.",
                "Вкл.: сезонные броски гибели от стресса (наземные). Выкл.: без сезонной гибели.");

            D(nameof(EcosystemConfig.UseSoilSuccession),
                "On: spread and death can change soil tier blocks. Off: soil blocks unchanged by ecology.",
                "Вкл.: распространение и гибель меняют уровень почвы. Выкл.: почва не меняется экологией.");

            D(nameof(EcosystemConfig.SoilSuccessionStrength),
                "Higher: stronger soil tier shifts on spread/death. Lower: subtler succession. 1.0 = default.",
                "Больше: сильнее смена уровень почвы. Меньше: слабее смена почвы. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.SoilSuccessionSkipWhenBuiltAbove),
                "On: skip soil swaps when slabs/builds occupy column above ground. Off: succession always runs.",
                "Вкл.: не менять почву под постройками/плитами. Выкл.: смена почвы всегда.");

            D(nameof(EcosystemConfig.UseFarmlandNutrientBridge),
                "On: tilling adds N/P/K from dominant wild plant role. Off: vanilla till nutrients only.",
                "Вкл.: вспашка добавляет азот, фосфор и калий от диких растений. Выкл.: только ванильные питательные.");

            D(nameof(EcosystemConfig.FarmlandNutrientBridgeStrength),
                "Higher: stronger till nutrient bonus from wild plants. Lower: weaker bridge. 1.0 = default.",
                "Больше: сильнее бонус при вспашке. Меньше: слабее мост. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.EnableFallowRestoration),
                "On: empty farmland near wild plants slowly regains nutrients. Off: no fallow restoration.",
                "Вкл.: пустые поля у диких растений медленно восстанавливают азот, фосфор и калий. Выкл.: без восстановления.");

            D(nameof(EcosystemConfig.FallowRestorationStrength),
                "Higher: faster nutrient recovery on fallow fields. Lower: slower restoration. 1.0 = default.",
                "Больше: быстрее восстановление питательных на пару. Меньше: медленнее. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.RespectLandClaims),
                "On: block spread, displacement, stress, and tree changes inside protected claims. Off: claims ignored.",
                "Вкл.: экология не трогает защищённые участки. Выкл.: участки игнорируются.");

            D(nameof(EcosystemConfig.MaxPendingTreeChecksPerTick),
                "Higher: more mod-placed saplings polled per tick until log-grown appears. Lower: slower sapling watch.",
                "Больше: больше саженцев проверяется за тик. Меньше: медленнее ожидание выросших стволов.");

            D(nameof(EcosystemConfig.EnableCyclicTreeDiscovery),
                "On: round-robin scan for new log-grown trunks after load. Off: no background tree discovery.",
                "Вкл.: циклический поиск новых выросших стволов после загрузки. Выкл.: без фонового поиска.");

            D(nameof(EcosystemConfig.MaxTreeRescanColumnsPerTick),
                "Higher: more columns scanned per tick for tree discovery. Lower: slower discovery, less CPU.",
                "Больше: больше колонок повторного обхода за тик. Меньше: медленнее поиск, меньше нагрузка на процессор.");

            D(nameof(EcosystemConfig.EnableCyclicFloraDiscovery),
                "On: round-robin live scan registers flowers/tallgrass after chunk load. Off: one-shot scan only.",
                "Вкл.: циклический повторный обход регистрирует цветы/траву после загрузки. Выкл.: только одноразовый обход.");

            D(nameof(EcosystemConfig.MaxFloraRescanColumnsPerTick),
                "Higher: more columns scanned per tick for flora discovery. Lower: slower meadow fill-in, less CPU.",
                "Больше: больше колонок обхода флоры за тик. Меньше: медленнее заполнение луга, меньше нагрузка на процессор.");

            D(nameof(EcosystemConfig.EnableTreeAging),
                "On: calendar age and yearly structure growth on wild trees. Off: no wild tree growth (senescence needs this).",
                "Вкл.: календарный возраст и рост структуры диких деревьев. Выкл.: без роста (старение требует вкл.).");

            D(nameof(EcosystemConfig.TreeMinSpreadAgeYears),
                "Higher: trees must be older before they can spread. Lower: younger trees may spread sooner. 0 = no age gate.",
                "Больше: деревьям нужно прожить больше лет, прежде чем они смогут распространяться. Меньше: раньше. 0 = без возрастного порога.");

            D(nameof(EcosystemConfig.TreeYoungSpreadBypassTrunkHeight),
                "Higher: more trees bypass the age gate (worldgen-sized trees spread even at age 0). Lower: stricter age gate. 0 = never bypass.",
                "Больше: больше деревьев обходят возрастной порог (деревья из генератора распространяются даже с возрастом 0). Меньше: строже. 0 = не обходить.");

            D(nameof(EcosystemConfig.MaxTreeGrowthAttemptsPerTick),
                "Higher: more trees advanced per reproduce tick. Lower: slower aging, less CPU.",
                "Больше: больше деревьев обрабатывается за тик. Меньше: медленнее старение.");

            D(nameof(EcosystemConfig.MaxTreeGrowthCatchUpYearsPerTick),
                "Higher: trees catch up more missed years after time skips. Lower: slower catch-up, less CPU. 1 = no catch-up.",
                "Больше: деревья быстрее догоняют пропущенные годы при промотке времени. Меньше: медленнее, меньше нагрузка. 1 = без догонки.");

            D(nameof(EcosystemConfig.TreeGrowthActivityScale),
                "Higher: faster wild tree growth vs reference size. Lower: slower growth. 1.0 = default.",
                "Больше: быстрее рост диких деревьев. Меньше: медленнее. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.EnableTreeSenescence),
                "On: phased wild tree death after species lifespan (snag → stump/logs). Off: trees never die of age.",
                "Вкл.: естественная гибель по возрасту (сухостой → пень/брёвна). Выкл.: деревья не стареют.");

            D(nameof(EcosystemConfig.EnableTreeSeralSuccession),
                "On: pioneer trees prefer open cells; climax trees prefer mature forest cover. Off: forest cover gates only.",
                "Вкл.: пионеры на открытых клетках, древостой — в зрелом лесу. Выкл.: только min/max лесности.");

            D(nameof(EcosystemConfig.TreeSenescenceSnagBlocks),
                "Higher: more log-grown blocks left as snag during death phase. Lower: shorter snag stage.",
                "Больше: выше сухостой из блоков выросших стволов. Меньше: короче фаза сухостоя.");

            D(nameof(EcosystemConfig.EnableTreeSenescenceRemains),
                "On: final year spawns vanilla stump and fallen logs. Off: snag removed without remains.",
                "Вкл.: в финале — пень и брёвна. Выкл.: сухостой исчезает без останков.");

            D(nameof(EcosystemConfig.TreeSenescenceFallenLogCount),
                "Higher: more horizontal debarked logs near stump (0 = stump only). Lower: fewer logs.",
                "Больше: больше горизонтальных брёвен у пня (0 = только пень). Меньше: меньше брёвен.");

            D(nameof(EcosystemConfig.EnableFerntreeEcology),
                "On: ferntree-normal blocks register, spread, age, and senesce. Off: vanilla ferntree only.",
                "Вкл.: папоротниковое дерево в экологии (распространение, возраст, старение). Выкл.: только ваниль.");

            D(nameof(EcosystemConfig.FerntreeSenescenceSnagSegments),
                "Higher: more trunk segments during ferntree snag phase. Lower: shorter snag.",
                "Больше: больше сегментов сухостоя папоротника. Меньше: короче сухостой.");

            D(nameof(EcosystemConfig.EnableWildVineEcology),
                "On: wildvine-end blocks spread down and along captured walls. Off: static vines.",
                "Вкл.: дикие лианы растут вниз и вдоль стен. Выкл.: статичные лианы.");

            D(nameof(EcosystemConfig.WildVineWallCaptureRadius),
                "Higher: capture wall faces farther horizontally. Lower: vines only on nearby walls.",
                "Больше: дальше захват стен по горизонтали. Меньше: только близкие стены.");

            D(nameof(EcosystemConfig.WildVineWallCaptureHeight),
                "Higher: taller vertical span for wall capture. Lower: shorter vine wall reach.",
                "Больше: выше вертикальный захват стен. Меньше: короче дальность лиан по стене.");

            D(nameof(EcosystemConfig.WildVineMaxHangDepth),
                "How many blocks a vine may extend downward through open air below the last wall-backed segment (0 = wall only). Higher = longer free-hanging tips.",
                "Сколько блоков лиана может свисать в воздухе под последним участком на стене (0 = только по стене). Больше = длиннее свободный кончик.");

            D(nameof(EcosystemConfig.EnableMyceliumNiche),
                "On: meadow spread penalty and forest bonus near mycelium anchors. Off: no mycelium niche tuning.",
                "Вкл.: штраф луга и бонус леса у якорей грибницы. Выкл.: без ниши грибницы.");

            D(nameof(EcosystemConfig.MyceliumZoneRadius),
                "Higher: wider Chebyshev niche around each anchor (vanilla growRange 7). Lower: tighter zone.",
                "Больше: шире зона ниши вокруг якоря (vanilla 7). Меньше: уже зона.");

            D(nameof(EcosystemConfig.MyceliumMeadowSpreadPenalty),
                "Higher: meadow plants spread much slower at anchor (tapers to 1.0 at edge). Lower: milder penalty.",
                "Больше: луговые виды сильнее штрафуются у якоря. Меньше: слабее штраф.");

            D(nameof(EcosystemConfig.MyceliumForestSpreadBonus),
                "Higher: stronger forest understory bonus at anchor. Lower: weaker bonus.",
                "Больше: сильнее бонус леса у якоря. Меньше: слабее бонус.");

            D(nameof(EcosystemConfig.MyceliumSkipSoilSuccession),
                "On: no soil succession on mycelium anchor cells. Off: anchors participate in succession.",
                "Вкл.: смена почвы на якорях отключена. Выкл.: якоря участвуют в смена почвы.");

            D(nameof(EcosystemConfig.EnableMyceliumEcology),
                "On: register anchors; niche stress and death on mycelium. Off: vanilla mycelium only.",
                "Вкл.: регистрация якорей, стресс и гибель грибницы. Выкл.: только ваниль.");

            D(nameof(EcosystemConfig.EnableMyceliumCapDisplacement),
                "On: Harmony patch lets vanilla cap regrowth displace meadow grass/flowers in growRange. Off: vanilla air-only placement.",
                "Вкл.: патч Harmony — респавн шляпок может вытеснять луговую траву/цветы. Выкл.: только air, как в ванилле.");

            D(nameof(EcosystemConfig.MyceliumTreeHostRadius),
                "Higher: search farther for tree host required by forest mycelium. Lower: stricter host proximity.",
                "Больше: дальше поиск дерева-хозяина для лесной грибницы. Меньше: ближе нужно дерево.");

            D(nameof(EcosystemConfig.MyceliumForestMinForestCover),
                "Higher: forest anchor stressed unless more local cover (stricter). Lower: tolerates more open forest.",
                "Больше: лесной якорь стрессует при меньшей лесности (строже). Меньше: терпимее к прогалинам.");

            D(nameof(EcosystemConfig.MyceliumMeadowMaxForestCover),
                "Higher: meadow anchor tolerates more nearby trees before stress. Lower: stricter meadow niche.",
                "Больше: луговой якорь терпит больше деревьев. Меньше: строже луговая ниша.");

            D(nameof(EcosystemConfig.EnableMyceliumNetworkSpread),
                "On: slow orthogonal network spread from mat edge between anchors. Off: no network colonization.",
                "Вкл.: медленное сетевое распространение с кромки ковра между якорями. Выкл.: без сети.");

            D(nameof(EcosystemConfig.MyceliumSpreadRate),
                "Higher: faster mycelium network spread interval scale. Lower: slower colonization.",
                "Больше: быстрее сетевое распространение грибницы. Меньше: медленнее колонизация.");

            D(nameof(EcosystemConfig.MyceliumSpreadAttemptsPerYear),
                "Higher: more network spread attempts per in-game year. Lower: sparser mycelium network.",
                "Больше: больше попыток сети за игровой год. Меньше: реже сеть.");

            D(nameof(EcosystemConfig.MyceliumSpreadMinFitness),
                "Higher: pickier network colonization and displacement. Lower: accepts weaker anchor sites.",
                "Больше: избирательнее колонизация сети. Меньше: слабее клетки допускаются.");

            D(nameof(EcosystemConfig.EnableSeasonalFoliage),
                "On: deciduous autumn strip and spring bud on log-grown skeleton. Off: static vanilla crowns.",
                "Вкл.: осеннее снятие листвы и весенние почки на выросших стволах. Выкл.: статичные ванильные кроны.");

            D(nameof(EcosystemConfig.FoliageSyncMode),
                "chunk = sync on chunk load (default). hybrid = chunk plus random tick. random = legacy random only.",
                "при загрузке участка (по умолчанию). гибридный = участок плюс случайный тик. случайный = только прежний режим.");

            D(nameof(EcosystemConfig.MaxFoliageCellsTickedPerTick),
                "Higher: more random foliage cells per reproduce tick (hybrid/random). 0 = off.",
                "Больше: больше случайных клеток листвы за тик (гибридный/случайный). 0 = выкл.");

            D(nameof(EcosystemConfig.FoliageBudgetMs),
                "Higher: more ms for foliage random tick (smoother, more CPU). 0 = linked/unlimited alias.",
                "Больше: больше мс на случайный тик листвы. 0 = связанный бюджет или без лимита.");

            D(nameof(EcosystemConfig.FoliageChunkSyncBudgetMs),
                "Higher: more ms per chunk foliage sync pass. Lower: faster passes, less work per chunk.",
                "Больше: больше мс на синхронизация листвы участока. Меньше: быстрее проход, меньше работы.");

            D(nameof(EcosystemConfig.FoliageChunkWorkPerTick),
                "Higher: more chunks resumed per chunk-scan tick. Lower: slower foliage catch-up.",
                "Больше: больше участоков листвы за тик обхода. Меньше: медленнее догонка.");

            D(nameof(EcosystemConfig.FoliageCatchUpOnChunkLoad),
                "On: sync foliage to current season when chunk loads. Off: foliage may lag until random tick.",
                "Вкл.: листва догоняет сезон при загрузке участка. Выкл.: может отставать до случайного тика.");

            D(nameof(EcosystemConfig.MaxFoliageCatchUpPerChunk),
                "Higher: more strip+bud ops per chunk load (0 = unlimited). Lower: less hitch when walking forests.",
                "Больше: больше догонки листвы при загрузке участка (0 = без лимита). Меньше: меньше просадок в лесу.");

            D(nameof(EcosystemConfig.FoliageColumnScanHeightAboveSurface),
                "Higher: scan fewer blocks above surface (less work). 0 = full column height.",
                "Больше: уже обход над поверхностью (меньше работы). 0 = вся колонка.");

            D(nameof(EcosystemConfig.FoliagePeakAutumnBranchyStripActivity),
                "Higher: strip more branchy foliage in peak autumn (0 = keep all branchy). Lower: gentler strip.",
                "Больше: сильнее снятие ветвистой листвы осенью (0 = оставить всё). Меньше: мягче снятие.");

            D(nameof(EcosystemConfig.EnableCanopyFallenSticks),
                "On: drop ванильные палки when branchy foliage strips in autumn. Off: no stick drops.",
                "Вкл.: выпадают ванильные палки при осеннем снятии ветвистой листвы. Выкл.: без палок.");

            D(nameof(EcosystemConfig.CanopyFallenStickChance),
                "Higher (toward 1): more stick drops at peak autumn. Lower (toward 0): rarer sticks.",
                "Больше (к 1): чаще палки в пик осени. Меньше (к 0): реже палки.");

            D(nameof(EcosystemConfig.EnableSpringBranchyAgeBoost),
                "On: spring branchy buds scale with tree calendar age. Off: uniform spring bud strength.",
                "Вкл.: весенние ветвистой листвы почки зависят от возраста дерева. Выкл.: равномерная сила почек.");

            D(nameof(EcosystemConfig.SpringBranchyAgeBoostYearsToMax),
                "Higher: older trees needed for max spring branchy boost. Lower: young trees reach max sooner.",
                "Больше: для максимальный бонус нужны более старые деревья. Меньше: молодые быстрее получают максимум.");

            D(nameof(EcosystemConfig.SpringBranchyAgeBoostMax),
                "Higher: stronger max spring branchy multiplier from age. Lower: subtler age effect.",
                "Больше: сильнее максимум множитель ветвистой листвы от возраста. Меньше: слабее эффект возраста.");

            D(nameof(EcosystemConfig.FoliageRestoreBareSkeleton),
                "On: winter repair adds branchy leaves on bare log-grown pillars. Off: bare crowns stay bare.",
                "Вкл.: зимой восстанавливается ветвистая листва на голых выросших стволах. Выкл.: голые кроны остаются голыми.");

            D(nameof(EcosystemConfig.EnableOrphanFoliagePrune),
                "On: remove wild leaves with no path to log-grown (e.g. after wildfire). Off: leave floating foliage.",
                "Вкл.: снимает дикую листву без связи со стволом (после пожара). Выкл.: висящие листья остаются.");

            D(nameof(EcosystemConfig.OrphanFoliageMaxBfsDepth),
                "Higher: longer support search before pruning (safer, more CPU). Lower: faster, may miss edge cases.",
                "Больше: длиннее поиск опоры перед снятием. Меньше: быстрее, возможны пропуски.");

            D(nameof(EcosystemConfig.OrphanFoliageMaxChecksPerChunkPass),
                "Higher: more orphan BFS checks per chunk pass (0 = unlimited). Lower: slower cleanup.",
                "Больше: больше проверок сирот за проход чанка (0 = без лимита). Меньше: медленнее уборка.");

            D(nameof(EcosystemConfig.OrphanFoliageFireChunkHours),
                "Hours after fire to prioritize orphan-prune chunk passes. Higher: longer priority window. Lower: shorter. 0 = off.",
                "Часов после пожара с приоритетом уборки сирот в чанке. Больше: дольше окно приоритета. Меньше: короче. 0 = выкл.");

            D(nameof(EcosystemConfig.CanopyActivityScale),
                "Higher: faster seasonal defoliation and budding curves. Lower: subtler canopy seasons. 1.0 = default.",
                "Больше: быстрее сезонные кривые сброс листвы/почки. Меньше: слабее сезоны кроны. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.CanopyBudMinTemperature),
                "Higher: spring buds need warmer cells (shorter bud season). Lower: buds in cooler weather.",
                "Больше: почки только в более тёплых клетках (короче сезон). Меньше: почки в прохладнее.");

            D(nameof(EcosystemConfig.CanopyLatitudeInfluence),
                "Higher: stronger polar slowdown of canopy seasons. Lower: less latitude effect. 0 = off.",
                "Больше: сильнее полярное замедление сезонов кроны. Меньше: слабее. 0 = выкл.");

            D(nameof(EcosystemConfig.EnableCanopyAmbience),
                "On: client green motes and autumn leaf drift under tall deciduous crowns. Off: no particles.",
                "Вкл.: клиент — зелёные частицы и осенние листья под высокой кроной. Выкл.: без частиц.");

            D(nameof(EcosystemConfig.CanopyAmbienceMinHeightBlocks),
                "Higher: particles only under taller foliage above feet. Lower: ambience in shorter crowns.",
                "Больше: частицы только под более высокой листвой. Меньше: атмосфера и в низкой кроне.");

            D(nameof(EcosystemConfig.CanopyAmbienceMoteRate),
                "Higher: denser green mote spawn under canopy. Lower: fewer motes. 1.0 = default.",
                "Больше: гуще зелёные частицы под кроной. Меньше: реже. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.CanopyAmbienceLeafDriftRate),
                "Higher: more autumn leaf drift particles. Lower: sparser drift. 1.0 = default.",
                "Больше: больше осенних листьев. Меньше: реже падение листьев. 1.0 — по умолчанию.");

            D(nameof(EcosystemConfig.CanopyAmbienceSampleIntervalSeconds),
                "Higher: less frequent canopy density re-sample (less CPU). Lower: more responsive ambience.",
                "Больше: реже пересчёт плотности кроны. Меньше: отзывчивее атмосфера.");

            D(nameof(EcosystemConfig.CanopyAmbienceSuppressInRain),
                "On: suppress canopy particles during heavy rain. Off: particles still spawn in rain.",
                "Вкл.: частицы гасятся в сильный дождь. Выкл.: частицы и в дождь.");

            D(nameof(EcosystemConfig.EnableChunkFairSpread),
                "On: round-robin spread attempts across registry chunks (fair pacing). Off: less fair chunk order.",
                "Вкл.: распространение по очереди по участкам реестра. Выкл.: менее равномерный порядок.");

            D(nameof(EcosystemConfig.MaxSpreadAttemptsPerChunkPerTick),
                "Higher: more spread attempts per chunk per reproduce tick. Lower: slower per-chunk spread.",
                "Больше: больше попыток распространения на участок за тик. Меньше: медленнее распространения в участоке.");

            D(nameof(EcosystemConfig.MaxSpreadChunksVisitedPerTick),
                "Higher: visit more registry chunks per reproduce tick. Lower: slower global spread sweep.",
                "Больше: больше участоков распространения за тик. Меньше: медленнее обход реестра.");

            D(nameof(EcosystemConfig.EnableEventDrivenSpread),
                "On: wake neighbor ecology when relevant blocks change. Off: spread only on scheduled ticks.",
                "Вкл.: пробуждение соседей при изменении блоков. Выкл.: только по расписанию.");

            D(nameof(EcosystemConfig.EnableSeasonCoarseWake),
                "On: wake seasonal species once per in-game month. Off: no monthly coarse wake.",
                "Вкл.: месячное пробуждение сезонных видов. Выкл.: без месячного пробуждения.");

            D(nameof(EcosystemConfig.EcologyWakeRadiusBlocks),
                "0 = auto from spread radius and spacing. Higher: wake more neighbors on block changes.",
                "0 = авто от распространения и дистанции. Больше: больше соседей просыпается при изменениях.");

            D(nameof(EcosystemConfig.EnableEcologyColumnCache),
                "On: cache spread column snapshots (faster repeat attempts). Off: rescan columns each time.",
                "Вкл.: кэш снимков колонок распространения (быстрее повторы). Выкл.: обход каждый раз.");

            D(nameof(EcosystemConfig.EnableTwoPhaseSpreadPlacement),
                "On: evaluate spread then commit SetBlock in fair pass. Off: immediate placement on success.",
                "Вкл.: оценка распространения, затем применение в честном проходе. Выкл.: немедленная установка блока.");

            D(nameof(EcosystemConfig.MaxSpreadCommitsPerTick),
                "Higher: more spread block commits per tick (0 = MaxReproduceAttemptsPerTick). Lower: slower commits.",
                "Больше: больше применений распространения за тик (0 = лимит попыток за тик). Меньше: медленнее применение.");

            D(nameof(EcosystemConfig.MaxSpreadCommitChunksVisitedPerTick),
                "Higher: more chunks in commit pass (0 = MaxSpreadChunksVisitedPerTick). Lower: narrower commit sweep.",
                "Больше: больше участков в проходе применения (0 = лимит участков за тик). Меньше: более узкий обход.");

            D(nameof(EcosystemConfig.MaxSpreadCommitsPerChunkPerTick),
                "Higher: more commits per chunk per tick (0 = MaxSpreadAttemptsPerChunkPerTick). Lower: slower local commits.",
                "Больше: больше применений на участок (0 = лимит попыток на участок). Меньше: медленнее локально.");

            D(nameof(EcosystemConfig.MaxReproduceAttemptsPerTick),
                "Higher: more spread evaluations per reproduce tick (faster sim, more CPU). Lower: gentler spread pacing.",
                "Больше: больше оценок распространения за тик (быстрее, выше процессор). Меньше: мягче темп распространения.");

            D(nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick),
                "Per worker (× worker count). Higher: more sync column scans when background scan off. Lower: slower registration catch-up.",
                "На поток (× число потоков). Больше: больше синхронных обходов колонок без фонового обхода. Меньше: медленнее догонка.");

            D(nameof(EcosystemConfig.MaxRegistrationsPerTick),
                "Per worker (× worker count). Higher: more sync registrations when background scan off. Lower: slower registry fill.",
                "На поток (× число потоков). Больше: больше синхронизация-регистраций без фонового обхода. Меньше: медленнее заполнение реестра.");

            D(nameof(EcosystemConfig.EnablePlayerPriorityRegistration),
                "On: drain player-vicinity chunks before background registration queue. Off: uniform queue order.",
                "Вкл.: сначала участки у игрока, потом фон. Выкл.: единая очередь.");

            D(nameof(EcosystemConfig.EnableBurstRegistrationNearPlayers),
                "On: finish nearby chunk registration on load within ms budget. Off: no burst completion.",
                "Вкл.: пакетная регистрация участков у игрока при загрузке. Выкл.: без пакетного режима.");

            D(nameof(EcosystemConfig.PlayerRegistrationPriorityRadiusBlocks),
                "Higher: wider player-vicinity priority and burst registration. Lower: tighter priority zone.",
                "Больше: шире зона приоритетной/пакетный регистрации. Меньше: уже зона.");

            D(nameof(EcosystemConfig.MaxPriorityChunkScansPerTick),
                "Per worker (× worker count). Higher: more priority queue passes per chunk-scan tick. Lower: slower player-vicinity registration.",
                "На поток (× число потоков). Больше: больше приоритетных проходов за тик обхода. Меньше: медленнее регистрация у игрока.");

            D(nameof(EcosystemConfig.MaxPriorityRegistrationsPerTick),
                "Per worker (× worker count). Higher: more registrations from priority queue per tick. Lower: slower near-player fill.",
                "На поток (× число потоков). Больше: больше приоритетных регистраций за тик. Меньше: медленнее у игрока.");

            D(nameof(EcosystemConfig.PriorityRegistrationBudgetMs),
                "Higher: more ms per priority registration pass (smoother, more CPU). Lower: stricter time cap.",
                "Больше: больше мс на приоритетный проход. Меньше: жёстче лимит времени.");

            D(nameof(EcosystemConfig.BurstRegistrationBudgetMs),
                "Higher: more ms to finish one burst chunk on load. Lower: smaller burst completion slice.",
                "Больше: больше мс на завершение пакетного участка. Меньше: меньшая доля.");

            D(nameof(EcosystemConfig.MaxBurstRegistrationsPerChunk),
                "Per worker (× worker count). Higher: allow more registrations when finishing one burst chunk. Lower: cap burst chunk size.",
                "На поток (× число потоков). Больше: больше регистраций при завершении пакетного участка. Меньше: ниже лимит.");

            D(nameof(EcosystemConfig.MaxRegistryAppliesPerTick),
                "Per worker (× worker count). Higher: more paced RegisterReproducer applies per chunk-scan tick. Lower: slower registry pacing.",
                "На поток (× число потоков). Больше: больше регистрация за тик обхода. Меньше: медленнее темп.");

            D(nameof(EcosystemConfig.MaxRegistryAppliesPerChunkPerTick),
                "Per worker (× worker count). Higher: more registry inserts from one chunk per drain pass. Lower: fairer but slower single-chunk meadows.",
                "На поток (× число потоков). Больше: больше вставки из одного участка за проход очереди. Меньше: честнее по очереди, но медленнее луга.");

            D(nameof(EcosystemConfig.RegistrationWorkerCount),
                "Higher: more background column-classification threads (max 8). 0 = half CPU cores. Registration throughput keys are per worker and scale with this count. Lower: fewer parallel scans. Snapshot and SetBlock stay on main thread.",
                "Больше: больше потоков классификации (макс. 8). 0 = половина ядер. Лимиты регистрации — на поток и умножаются на это число. Меньше: меньше параллельных обходов. Снимок и установка блока — в основном потоке.");

            D(nameof(EcosystemConfig.MaxPriorityRegistryAppliesPerTick),
                "Per worker (× worker count). Higher: more extra applies for player-vicinity chunks. Lower: slower near-player registry.",
                "На поток (× число потоков). Больше: больше приоритетных вставок у игрока за тик. Меньше: медленнее реестр у игрока.");

            D(nameof(EcosystemConfig.EnableBackgroundRegistrationScan),
                "On: classify columns on worker from main-thread snapshot. Off: sync scan on main thread only.",
                "Вкл.: классификация колонок во фоновом потоке из снимка. Выкл.: только синхронизация в основном потоке.");

            D(nameof(EcosystemConfig.EnableBackgroundSpreadSolve),
                "On: score spread candidates on worker from compact env snapshots; SetBlock stays on main thread. Requires two-phase spread. Terrestrial, mat, and water crowfoot.",
                "Вкл.: оценка распространения во фоновом потоке из компактных снимок; установка блока в основном потоке. Нужно двухфазное распространение. Наземные, ковёр и водяной лютик.");

            D(nameof(EcosystemConfig.SpreadWorkerCount),
                "Higher: more background spread-scoring threads (max 8). 0 = half CPU cores. Snapshot and SetBlock stay on main thread.",
                "Больше: больше потоков оценки распространения (макс. 8). 0 = половина ядер. Снимок и установка блока — в основном потоке.");

            D(nameof(EcosystemConfig.MaxRegistrationSnapshotCellsPerTick),
                "Per worker (× worker count). Higher: copy more block ids to snapshot per main tick. Lower: slower background scan feed.",
                "На поток (× число потоков). Больше: больше идентификаторов блоков в снимок за тик. Меньше: медленнее подача данных фоновому обходу.");

            D(nameof(EcosystemConfig.TickBudgetMs),
                "Higher: more ms allowed per reproduce tick (smoother, more CPU). 0 = unlimited.",
                "Больше: больше мс за размножение-тик. 0 = без лимита.");

            D(nameof(EcosystemConfig.SpreadBudgetMs),
                "Higher: more ms for spread phase (0 = TickBudgetMs). Lower: tighter spread cap.",
                "Больше: больше мс на распространение (0 = бюджет тика). Меньше: жёстче лимит распространения.");

            D(nameof(EcosystemConfig.RegistrationBudgetMs),
                "Higher: more ms for chunk-scan phase (0 = TickBudgetMs). Lower: tighter registration cap.",
                "Больше: больше мс на обход участков (0 = бюджет тика). Меньше: жёстче лимит регистрации.");

            D(nameof(EcosystemConfig.StressBudgetMs),
                "Higher: more ms for stress phase (0 = TickBudgetMs). Lower: tighter stress cap.",
                "Больше: больше мс на стресс (0 = бюджет тика). Меньше: жёстче лимит стресса.");

            D(nameof(EcosystemConfig.EnableReproduceTickProfiling),
                "On: log reproduce phase timings when registry is large. Off: no profiling logs.",
                "Вкл.: лог фаз размножения при большом реестре. Выкл.: без профилирования.");

            D(nameof(EcosystemConfig.ReproduceTickProfilingMinRegistry),
                "Higher: profiling logs only when registry is larger. Lower: logs on smaller registries.",
                "Больше: профиль только при большем реестре. Меньше: логи и на меньшем реестре.");

            D(nameof(EcosystemConfig.ReproduceTickProfilingIntervalMs),
                "Higher: less frequent profiling log lines. Lower: more frequent timing logs.",
                "Больше: реже строки профиля. Меньше: чаще логи таймингов.");

            D(nameof(EcosystemConfig.StressTickIntervalMs),
                "Higher: less frequent stress ticks (less CPU). Lower: more frequent stress updates.",
                "Больше: реже тики стресса (меньше процессор). Меньше: чаще обновления стресса.");

            D(nameof(EcosystemConfig.ReproduceTickIntervalMs),
                "Higher: less frequent spread/foliage/tree ticks. Lower: more frequent spread updates.",
                "Больше: реже тики распространения/листва/деревьев. Меньше: чаще обновления распространения.");

            D(nameof(EcosystemConfig.ChunkScanTickIntervalMs),
                "Higher: less frequent registration and foliage chunk sync. Lower: faster registry/foliage sync.",
                "Больше: реже обход участков (регистрация/синхронизация листвы). Меньше: быстрее синхронизация.");

            D(nameof(EcosystemConfig.OnlyActivateNearPlayers),
                "On: spread, stress, trees, and chunk scans only within player radius (~192 blocks). Off: all loaded chunks.",
                "Вкл.: распространение, стресс, деревья и обходы только в радиусе игрока (~192 блока). Выкл.: все загруженные участки.");

            D(nameof(EcosystemConfig.LimitSpreadNearPlayers),
                "On: spread, stress, and tree aging only near players; chunk registration unchanged. Off: full simulation.",
                "Вкл.: распространение, стресс и деревья у игроков; регистрация без изменений. Выкл.: полная симуляция.");

            D(nameof(EcosystemConfig.PlayerActivationRadiusBlocks),
                "Higher: wider radius for OnlyActivateNearPlayers and LimitSpreadNearPlayers. Lower: tighter playtest zone.",
                "Больше: шире радиус для флагов «только у игроков». Меньше: уже зона для тестирования.");

            D(nameof(EcosystemConfig.VerboseLogging),
                "On: extra notification and warning logs (CPU cost). Off: errors and startup only.",
                "Вкл.: больше уведомлений и предупреждений (нагрузка на процессор). Выкл.: только ошибки и старт.");

            D(nameof(EcosystemConfig.ReproduceDebug),
                "On: log spread attempts (pair with VerboseLogging). Off: silent spread path.",
                "Вкл.: логировать попытки распространения (вместе с «Подробные логи»). Выкл.: без лога распространения.");

            D(nameof(EcosystemConfig.EnableTrampling),
                "On: footsteps leave column pressure, wear plants, and slow meadow recolonization. Off (default): no trail ecology.",
                "Вкл.: шаги уплотняют колонки, изнашивают растения и замедляют зарастание. Выкл. (по умолчанию): троп нет.");

            D(nameof(EcosystemConfig.TramplingRadius),
                "Higher: plants next to the foot cell also wear. 0 = only the cell you stand in.",
                "Больше: изнашиваются и соседние растения. 0 = только клетка под ногами.");

            D(nameof(EcosystemConfig.TramplingStressThreshold),
                "Higher: more footsteps on a flower/fern before removal. Tallgrass shortens one stage each step first.",
                "Больше: больше шагов по цветку до снятия. Трава сначала теряет ступень высоты.");

            D(nameof(EcosystemConfig.TramplingSoilDegradation),
                "On: traffic syncs grass coverage (normal↔verysparse) on the same fertility soil; abandoned trails restore as pressure fades. Off: plants only. Never drains farmland.",
                "Вкл.: трафик синхронизирует покров (normal↔verysparse) при том же плодородии; заброшенные тропы зарастают со спадом давления. Выкл.: только растения. Пашню не трогает.");

            D(nameof(EcosystemConfig.FootTrafficStepsToFullCoverageWear),
                "How many footsteps on one column to reach verysparse (default 20). Set 0 to use the advanced pressure-step override.",
                "Сколько шагов по одной колонке до verysparse (дефолт 20). 0 — использовать доп. pressure-step.");

            D(nameof(EcosystemConfig.FootTrafficPressurePerStep),
                "Higher: trails pack faster (0–255 column pressure). Lower: need more traffic for the same wear.",
                "Больше: тропа уплотняется быстрее (давление 0–255). Меньше: нужно больше проходов.");

            D(nameof(EcosystemConfig.FootTrafficDecayPerDay),
                "Higher: abandoned trails heal faster. Lower: packed paths linger longer.",
                "Больше: заброшенные тропы зарастают быстрее. Меньше: колея держится дольше.");

            D(nameof(EcosystemConfig.FootTrafficMinSpreadMultiplier),
                "Higher: easier reclaim at full pressure. Lower: harder for plants to reclaim the trail (fitness floor).",
                "Больше: легче зарастить колею при полном давлении. Меньше: сложнее вернуть луг (пол fitness).");

            D(nameof(EcosystemConfig.FootTrafficSoilWearPressureStep),
                "Advanced override when StepsToFullCoverageWear is 0. Pressure points per coverage stage (≤127). Unused when Steps > 0.",
                "Доп. режим, если StepsToFullCoverageWear = 0. Пункты давления на ступень покрова (≤127). При Steps > 0 не используется.");

            D(nameof(EcosystemConfig.FootTrafficAnimalStrideBlocks),
                "Higher: animals leave traffic less often per distance. Lower: denser animal trails.",
                "Больше: животные реже оставляют след на метр пути. Меньше: гуще тропы животных.");

            D(nameof(EcosystemConfig.FootTrafficAnimalPlayerRadiusBlocks),
                "Higher: animals farther from players still leave trails. Lower: only nearby fauna. 0 = all loaded creatures.",
                "Больше: тропы животных дальше от игроков. Меньше: только рядом. 0 = все загруженные.");

            D(nameof(EcosystemConfig.FootTrafficSampleIntervalMs),
                "Unused legacy key — kept for config merge. Higher/lower values have no effect; animals use physics stride, players OnFootStep.",
                "Не используется (legacy). Больше/меньше не влияют: животные — physics stride, игроки — OnFootStep.");

            D(nameof(EcosystemConfig.EnableAnimalFootTraffic),
                "On: large creatures near players get physics stride trail hooks (can hitch SSP with herds). Off (default): players only via OnFootStep.",
                "Вкл.: крупные существа у игроков — physics stride (стада могут лагать SSP). Выкл. (по умолчанию): только игроки (OnFootStep).");

            D(nameof(EcosystemConfig.EnableFlowerDrygrass),
                "On: empty hand harvests flower block; knife/scythe yields drygrass. Off: vanilla harvest only.",
                "Вкл.: пустая рука — блок цветка; нож/коса — сухая трава. Выкл.: только ваниль.");

            D(nameof(EcosystemConfig.EnableEcologyInspect),
                "On: allow ecology inspect hotkey (default I). Off: inspect disabled.",
                "Вкл.: осмотр экологии по клавише (по умолчанию I). Выкл.: осмотр отключён.");

            D(nameof(EcosystemConfig.EcologyInspectCooldownSeconds),
                "Higher: longer wait between inspect requests per player. Lower: more frequent inspects.",
                "Больше: длиннее пауза осмотра на игрока. Меньше: чаще осмотр.");

            D(nameof(EcosystemConfig.EcologyInspectScanRadius),
                "Higher: wider nearby-species tally in inspect report. Lower: more local species list.",
                "Больше: шире подсчёт видов в отчёте. Меньше: локальнее список.");

            D(nameof(EcosystemConfig.EnableEcologyAreaScan),
                "On: include area species mix in inspect report. Off: target block only.",
                "Вкл.: в отчёте — смесь видов вокруг. Выкл.: только целевой блок.");

            D(nameof(EcosystemConfig.CloneBerryTraits),
                "On: spread copies parent bush genetic traits. Off: vanilla random wild traits on new bushes.",
                "Вкл.: распространение копирует генетические черты куста. Выкл.: ванильные случайные черты.");

            D(nameof(EcosystemConfig.BerryTraitMutationChance),
                "Higher: offspring more often lose one random trait on spread. 0 = no mutations.",
                "Больше: чаще теряется случайная черта при распространении. 0 = без мутаций.");

            D(nameof(EcosystemConfig.EnableThirdPartyParticipants),
                "On: blocks with ecologyParticipant JSON from other mods join ecology. Off: vanilla blocks only.",
                "Вкл.: блоки с меткой участника экологии из других модов (например, Wildgrass). Выкл.: только ваниль.");

            return m;
        }
    }
}
