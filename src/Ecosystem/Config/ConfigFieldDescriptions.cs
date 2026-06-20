using System;
using System.Collections.Generic;

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
                "natural/lush/sparse apply bundled spread values on each server start. custom keeps your manual spread tuning.",
                "natural/lush/sparse задают набор spread при каждом старте сервера. custom сохраняет ваши ручные значения.");

            D(nameof(EcosystemConfig.EcosystemEnabled),
                "On: spread, competition, stress, and most ecology ticks run. Off: mod ecology idle.",
                "Вкл.: spread, конкуренция, стресс и тики работают. Выкл.: экология мода простаивает.");

            D(nameof(EcosystemConfig.HarshWildPlants),
                "On: species climate and soil bounds apply (wrong niche builds stress). Off: softer survival checks.",
                "Вкл.: границы климата и почвы вида (не та ниша — стресс). Выкл.: мягче проверки выживания.");

            D(nameof(EcosystemConfig.ApplyWorldgenRainForest),
                "On: spread fitness uses worldgen rainfall map. Off: rainfall gate ignored (forest cover still uses neighbor trees).",
                "Вкл.: fitness spread учитывает карту worldgen-осадков. Выкл.: осадки игнорируются (лесность — по соседним деревьям).");

            D(nameof(EcosystemConfig.ReproduceRadius),
                "Higher: wider horizontal search for spread targets. Lower: tighter, more local colonization.",
                "Больше: шире горизонтальный поиск клеток для spread. Меньше: локальнее колонизация.");

            D(nameof(EcosystemConfig.ReproduceVerticalSearch),
                "Higher: search farther above/below for valid surface. Lower: narrower vertical placement window.",
                "Больше: шире поиск поверхности вверх/вниз. Меньше: уже окно размещения.");

            D(nameof(EcosystemConfig.ReproduceChance),
                "Higher (toward 1): more spread attempts succeed. Lower (toward 0): rarer successful placement.",
                "Больше (к 1): чаще успешный spread. Меньше (к 0): реже успешное размещение.");

            D(nameof(EcosystemConfig.MinFitness),
                "Higher: pickier offspring placement (fewer marginal sites). Lower: colonizes weaker cells more often.",
                "Больше: избирательнее размещение (меньше слабых клеток). Меньше: чаще на слабых клетках.");

            D(nameof(EcosystemConfig.ReproduceIntervalHours),
                "Higher: longer wait between spread attempts (legacy mode). Lower: more frequent attempts.",
                "Больше: длиннее пауза между попытками (legacy). Меньше: чаще попытки.");

            D(nameof(EcosystemConfig.ReproduceAttemptsPerYear),
                "Higher: more spread tries per in-game year at species rate 1 (denser flora). Lower: sparser spread.",
                "Больше: больше попыток spread за игровой год при rate 1 (густее). Меньше: реже spread.");

            D(nameof(EcosystemConfig.UseCalendarScaledSpread),
                "On: intervals scale from DaysPerYear/HoursPerDay. Off: use ReproduceIntervalHours only.",
                "Вкл.: интервалы от DaysPerYear/HoursPerDay. Выкл.: только ReproduceIntervalHours.");

            D(nameof(EcosystemConfig.UseSpeciesSpreadRates),
                "On: per-species SpreadRate scales interval and chance. Off: uniform spread pacing.",
                "Вкл.: SpreadRate вида масштабирует интервал и шанс. Выкл.: единый темп spread.");

            D(nameof(EcosystemConfig.MinSpeciesReproduceIntervalDays),
                "Higher: minimum days between attempts per species (calendar mode). 0 = no floor.",
                "Больше: минимум дней между попытками вида (календарь). 0 = без нижней границы.");

            D(nameof(EcosystemConfig.MinSpeciesReproduceIntervalHours),
                "Higher: minimum hours between attempts (legacy mode only). 0 = no floor.",
                "Больше: минимум часов между попытками (только legacy). 0 = без нижней границы.");

            D(nameof(EcosystemConfig.MaxFailedSurvivalChecks),
                "Higher: more failed checks tolerated before stress removal. Lower: plants die sooner from stress.",
                "Больше: больше неудач до удаления от стресса. Меньше: быстрее гибель.");

            D(nameof(EcosystemConfig.GrowthHoursMultiplier),
                "Higher: spread flower seedlings and tallgrass stages mature faster. Lower: longer establishing phase.",
                "Больше: быстрее взрослеют цветы и трава. Меньше: дольше ждать.");

            D(nameof(EcosystemConfig.EnableFlowerSpreadMaturation),
                "On: meadow colonizer flowers spread as small seedlings, then mature. Off: instant adult plant.",
                "Вкл.: новый цветок маленький, потом взрослеет. Выкл.: сразу взрослый.");

            D(nameof(EcosystemConfig.MaxPendingFlowerMaturationChecksPerTick),
                "Higher: more maturation checks per game tick. Lower: slower maturation queue.",
                "Больше: быстрее очередь созревания. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EnableTallgrassSpreadMaturation),
                "On: spread places very low grass; mod raises to short on a timer before spread. Off: height chosen at spread.",
                "Вкл.: новая трава очень низкая, мод доращивает до низкой, потом размножается. Выкл.: высота сразу по месту.");

            D(nameof(EcosystemConfig.MaxPendingTallgrassPromotionChecksPerTick),
                "Higher: more grass establishment checks per game tick. Lower: slower promotion queue.",
                "Больше: быстрее ждёт рост. Меньше: медленнее.");

            D(nameof(EcosystemConfig.EventWakeRetryHours),
                "Higher: event wake pulls spread retry farther forward (after spawn cooldown). Lower: subtler wake nudge.",
                "Больше: event wake сильнее сдвигает retry spread (после cooldown). Меньше: слабее пробуждение.");

            D(nameof(EcosystemConfig.StaggerReproduceAttempts),
                "On: random initial delay on registration spreads tick load. Off: all plants tick together.",
                "Вкл.: случайная задержка при регистрации размазывает нагрузку. Выкл.: синхронные тики.");

            D(nameof(EcosystemConfig.UseRhizomeSpreadForReeds),
                "On: cattail/tule/papyrus use mat-edge rhizome spread. Off: legacy radius spread.",
                "Вкл.: тростник/папирус — spread по краю mat. Выкл.: legacy spread по радиусу.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalEnabled),
                "On: rare seed/fragment jumps for reed and lily mats. Off: mat-edge spread only.",
                "Вкл.: редкие прыжки семян/фрагментов у тростника и кувшинки. Выкл.: только mat-edge.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalChanceScale),
                "Higher: more frequent distant seed jumps. Lower: rarer jumps. 1.0 is default.",
                "Больше: чаще дальние прыжки семян. Меньше: реже. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.RhizomeSeedDispersalFitnessScale),
                "Higher: stricter fitness for distant seed landing. Lower: jumps accept weaker sites.",
                "Больше: строже fitness для дальних прыжков. Меньше: слабее клетки допускаются.");

            D(nameof(EcosystemConfig.UseSurfaceMatSpreadForLilies),
                "On: water lily uses floating pad mat spread. Off: legacy radius spread.",
                "Вкл.: кувшинка — spread по плавучему mat. Выкл.: legacy spread по радиусу.");

            D(nameof(EcosystemConfig.PlantSpacingEnabled),
                "On: enforce Chebyshev spacing between spread plants. Off: spacing rules ignored.",
                "Вкл.: Chebyshev spacing между растениями. Выкл.: spacing не проверяется.");

            D(nameof(EcosystemConfig.ApplyCrossHabitatSpacing),
                "On: terrestrial and aquatic plants share spacing rules. Off: spacing only within same habitat.",
                "Вкл.: наземные и водные учитывают spacing друг друга. Выкл.: spacing только внутри среды.");

            D(nameof(EcosystemConfig.DefaultSameSpeciesSpacing),
                "Higher: same species must stay farther apart (patchier). Lower: denser same-species stands.",
                "Больше: дальше друг от друга один вид (пятнистее). Меньше: плотнее одновидовые группы.");

            D(nameof(EcosystemConfig.DefaultOtherSpeciesSpacing),
                "Higher: different species need more distance. Lower: mixed stands can be denser.",
                "Больше: больше дистанция между разными видами. Меньше: плотнее смешанные группы.");

            D(nameof(EcosystemConfig.SpacingVerticalSearch),
                "Higher: check spacing conflicts farther above/below. Lower: tighter vertical spacing scan.",
                "Больше: шире вертикальная проверка spacing. Меньше: уже скан по Y.");

            D(nameof(EcosystemConfig.UseCellDisplacement),
                "On: stronger species can displace weaker occupants. Off: no displacement on occupied cells.",
                "Вкл.: сильный вид вытесняет слабого. Выкл.: занятые клетки не вытесняются.");

            D(nameof(EcosystemConfig.DisplacementHoldMargin),
                "Higher: incumbent harder to displace (needs much stronger challenger). Lower: easier takeovers.",
                "Больше: занятый вид труднее вытеснить. Меньше: легче захват клетки.");

            D(nameof(EcosystemConfig.PreferSpreadToEmptyCells),
                "On: weight empty cells higher when spreading. Off: empty and occupied more even.",
                "Вкл.: при spread выше вес пустых клеток. Выкл.: пустые и занятые ближе по весу.");

            D(nameof(EcosystemConfig.EnableEmptyFirstSpreadCollect),
                "On: collect empty-cell candidates before displacement pass. Off: single combined pass.",
                "Вкл.: сначала пустые клетки, потом вытеснение. Выкл.: один общий проход.");

            D(nameof(EcosystemConfig.EnableSpreadColumnOccupancyHint),
                "On: skip columns known occupied on empty-first pass (faster). Off: scan all columns.",
                "Вкл.: пропуск занятых колонок на empty-first (быстрее). Выкл.: скан всех колонок.");

            D(nameof(EcosystemConfig.EmptySpreadFitnessMultiplier),
                "Higher: empty cells much more attractive when prefer-empty is on. Lower: weaker empty bias.",
                "Больше: пустые клетки сильнее предпочитаются. Меньше: слабее смещение к пустым.");

            D(nameof(EcosystemConfig.UseFloraContext),
                "On: neighbor trees/logs/leaves affect spread fitness. Off: ignore forest edge/interior context.",
                "Вкл.: соседние деревья/листва влияют на fitness. Выкл.: контекст леса игнорируется.");

            D(nameof(EcosystemConfig.FloraContextNeighborRadius),
                "Higher: count trees farther away for forest context. Lower: more local edge/interior signal.",
                "Больше: дальше считаются деревья для контекста. Меньше: локальнее сигнал опушки/чащи.");

            D(nameof(EcosystemConfig.FloraContextInteriorThreshold),
                "Higher: harder to count as forest interior (needs more neighbors). Lower: easier interior label.",
                "Больше: труднее считаться чащей (нужно больше соседей). Меньше: легче «interior».");

            D(nameof(EcosystemConfig.FloraOpenInteriorPenalty),
                "Higher: open-field species penalized more in forest interior. Lower: milder edge effect.",
                "Больше: луговые виды сильнее штрафуются в чаще. Меньше: слабее эффект.");

            D(nameof(EcosystemConfig.FloraContextCacheHours),
                "Higher: longer cache for local forest cover (less CPU). Lower: fresher context updates.",
                "Больше: дольше кэш лесности (меньше CPU). Меньше: чаще пересчёт контекста.");

            D(nameof(EcosystemConfig.UseNicheContext),
                "On: local moisture/light niche multipliers on spread fitness. Off: ignore niche layer.",
                "Вкл.: влага/свет локальной ниши в fitness. Выкл.: слой ниши игнорируется.");

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
                "On: forest symbionts need tree hosts; cascade on host loss. Off: symbiosis rules off.",
                "Вкл.: симбионты леса требуют дерево-хозяина; каскад при потере. Выкл.: симбиоз выключен.");

            D(nameof(EcosystemConfig.SymbiosisCascadeRadius),
                "Higher: wider radius when host tree removed triggers symbiont stress. Lower: tighter cascade.",
                "Больше: шире каскад при удалении дерева-хозяина. Меньше: уже зона каскада.");

            D(nameof(EcosystemConfig.UseSeasonalEcology),
                "On: monthly spread multipliers from WildSpeciesSeason profiles. Off: uniform spread year-round.",
                "Вкл.: месячные множители spread по сезонным профилям. Выкл.: равномерный spread круглый год.");

            D(nameof(EcosystemConfig.SeasonalStressEnabled),
                "On: seasonal stress die-off rolls for terrestrial plants. Off: no extra seasonal die-off.",
                "Вкл.: сезонные броски гибели от стресса (наземные). Выкл.: без сезонной гибели.");

            D(nameof(EcosystemConfig.UseSoilSuccession),
                "On: spread and death can change soil tier blocks. Off: soil blocks unchanged by ecology.",
                "Вкл.: spread и гибель меняют tier почвы. Выкл.: почва не меняется экологией.");

            D(nameof(EcosystemConfig.SoilSuccessionStrength),
                "Higher: stronger soil tier shifts on spread/death. Lower: subtler succession. 1.0 = default.",
                "Больше: сильнее смена tier почвы. Меньше: слабее succession. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.SoilSuccessionSkipWhenBuiltAbove),
                "On: skip soil swaps when slabs/builds occupy column above ground. Off: succession always runs.",
                "Вкл.: не менять почву под постройками/плитами. Выкл.: succession всегда.");

            D(nameof(EcosystemConfig.UseFarmlandNutrientBridge),
                "On: tilling adds N/P/K from dominant wild plant role. Off: vanilla till nutrients only.",
                "Вкл.: вспашка добавляет N/P/K от диких растений. Выкл.: только ванильные питательные.");

            D(nameof(EcosystemConfig.FarmlandNutrientBridgeStrength),
                "Higher: stronger till nutrient bonus from wild plants. Lower: weaker bridge. 1.0 = default.",
                "Больше: сильнее бонус при вспашке. Меньше: слабее мост. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.EnableFallowRestoration),
                "On: empty farmland near wild plants slowly regains nutrients. Off: no fallow restoration.",
                "Вкл.: пустые поля у диких растений медленно восстанавливают N/P/K. Выкл.: без восстановления.");

            D(nameof(EcosystemConfig.FallowRestorationStrength),
                "Higher: faster nutrient recovery on fallow fields. Lower: slower restoration. 1.0 = default.",
                "Больше: быстрее восстановление питательных на пару. Меньше: медленнее. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.RespectLandClaims),
                "On: block spread, displacement, stress, and tree changes inside protected claims. Off: claims ignored.",
                "Вкл.: экология не трогает защищённые участки. Выкл.: участки игнорируются.");

            D(nameof(EcosystemConfig.MaxPendingTreeChecksPerTick),
                "Higher: more mod-placed saplings polled per tick until log-grown appears. Lower: slower sapling watch.",
                "Больше: больше саженцев проверяется за тик. Меньше: медленнее ожидание log-grown.");

            D(nameof(EcosystemConfig.EnableCyclicTreeDiscovery),
                "On: round-robin scan for new log-grown trunks after load. Off: no background tree discovery.",
                "Вкл.: циклический поиск новых log-grown после загрузки. Выкл.: без фонового поиска.");

            D(nameof(EcosystemConfig.MaxTreeRescanColumnsPerTick),
                "Higher: more columns scanned per tick for tree discovery. Lower: slower discovery, less CPU.",
                "Больше: больше колонок перескана за тик. Меньше: медленнее поиск, меньше CPU.");

            D(nameof(EcosystemConfig.EnableTreeAging),
                "On: calendar age and yearly structure growth on wild trees. Off: no wild tree growth (senescence needs this).",
                "Вкл.: календарный возраст и рост структуры диких деревьев. Выкл.: без роста (senescence требует вкл.).");

            D(nameof(EcosystemConfig.MaxTreeGrowthAttemptsPerTick),
                "Higher: more trees advanced per reproduce tick. Lower: slower aging, less CPU.",
                "Больше: больше деревьев обрабатывается за тик. Меньше: медленнее старение.");

            D(nameof(EcosystemConfig.TreeGrowthActivityScale),
                "Higher: faster wild tree growth vs reference size. Lower: slower growth. 1.0 = default.",
                "Больше: быстрее рост диких деревьев. Меньше: медленнее. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.EnableTreeSenescence),
                "On: phased wild tree death after species lifespan (snag → stump/logs). Off: trees never die of age.",
                "Вкл.: естественная гибель по возрасту (сухостой → пень/брёвна). Выкл.: деревья не стареют.");

            D(nameof(EcosystemConfig.TreeSenescenceSnagBlocks),
                "Higher: more log-grown blocks left as snag during death phase. Lower: shorter snag stage.",
                "Больше: выше сухостой из log-grown блоков. Меньше: короче фаза сухостоя.");

            D(nameof(EcosystemConfig.EnableTreeSenescenceRemains),
                "On: final year spawns vanilla stump and fallen logs. Off: snag removed without remains.",
                "Вкл.: в финале — пень и брёвна. Выкл.: сухостой исчезает без останков.");

            D(nameof(EcosystemConfig.TreeSenescenceFallenLogCount),
                "Higher: more horizontal debarked logs near stump (0 = stump only). Lower: fewer logs.",
                "Больше: больше горизонтальных брёвен у пня (0 = только пень). Меньше: меньше брёвен.");

            D(nameof(EcosystemConfig.EnableFerntreeEcology),
                "On: ferntree-normal blocks register, spread, age, and senesce. Off: vanilla ferntree only.",
                "Вкл.: папоротниковое дерево в экологии (spread, возраст, senescence). Выкл.: только ваниль.");

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
                "Больше: выше вертикальный захват стен. Меньше: короче reach лиан.");

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
                "Вкл.: смена почвы на якорях отключена. Выкл.: якоря участвуют в succession.");

            D(nameof(EcosystemConfig.EnableMyceliumEcology),
                "On: register anchors; niche stress and death on mycelium. Off: vanilla mycelium only.",
                "Вкл.: регистрация якорей, стресс и гибель грибницы. Выкл.: только ваниль.");

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
                "Вкл.: медленный сетевой spread от края mat между якорями. Выкл.: без сети.");

            D(nameof(EcosystemConfig.MyceliumSpreadRate),
                "Higher: faster mycelium network spread interval scale. Lower: slower colonization.",
                "Больше: быстрее сетевой spread грибницы. Меньше: медленнее колонизация.");

            D(nameof(EcosystemConfig.MyceliumSpreadAttemptsPerYear),
                "Higher: more network spread attempts per in-game year. Lower: sparser mycelium network.",
                "Больше: больше попыток сети за игровой год. Меньше: реже сеть.");

            D(nameof(EcosystemConfig.MyceliumSpreadMinFitness),
                "Higher: pickier network colonization and displacement. Lower: accepts weaker anchor sites.",
                "Больше: избирательнее колонизация сети. Меньше: слабее клетки допускаются.");

            D(nameof(EcosystemConfig.EnableSeasonalFoliage),
                "On: deciduous autumn strip and spring bud on log-grown skeleton. Off: static vanilla crowns.",
                "Вкл.: осенний strip и весенние почки на log-grown. Выкл.: статичные ванильные кроны.");

            D(nameof(EcosystemConfig.FoliageSyncMode),
                "chunk = sync on chunk load (default). hybrid = chunk plus random tick. random = legacy random only.",
                "chunk = при загрузке чанка (по умолчанию). hybrid = chunk плюс random tick. random = только legacy.");

            D(nameof(EcosystemConfig.MaxFoliageCellsTickedPerTick),
                "Higher: more random foliage cells per reproduce tick (hybrid/random). 0 = off.",
                "Больше: больше случайных клеток листвы за тик (hybrid/random). 0 = выкл.");

            D(nameof(EcosystemConfig.FoliageBudgetMs),
                "Higher: more ms for foliage random tick (smoother, more CPU). 0 = linked/unlimited alias.",
                "Больше: больше мс на random tick листвы. 0 = связанный бюджет или без лимита.");

            D(nameof(EcosystemConfig.FoliageChunkSyncBudgetMs),
                "Higher: more ms per chunk foliage sync pass. Lower: faster passes, less work per chunk.",
                "Больше: больше мс на sync листвы чанка. Меньше: быстрее проход, меньше работы.");

            D(nameof(EcosystemConfig.FoliageChunkWorkPerTick),
                "Higher: more chunks resumed per chunk-scan tick. Lower: slower foliage catch-up.",
                "Больше: больше чанков листвы за тик скана. Меньше: медленнее догонка.");

            D(nameof(EcosystemConfig.FoliageCatchUpOnChunkLoad),
                "On: sync foliage to current season when chunk loads. Off: foliage may lag until random tick.",
                "Вкл.: листва догоняет сезон при загрузке чанка. Выкл.: может отставать до random tick.");

            D(nameof(EcosystemConfig.MaxFoliageCatchUpPerChunk),
                "Higher: more strip+bud ops per chunk per pass (0 = unlimited). Lower: slower seasonal catch-up.",
                "Больше: больше операций догонки на чанк (0 = без лимита). Меньше: медленнее догонка.");

            D(nameof(EcosystemConfig.FoliageColumnScanHeightAboveSurface),
                "Higher: scan fewer blocks above surface (less work). 0 = full column height.",
                "Больше: уже скан над поверхностью (меньше работы). 0 = вся колонка.");

            D(nameof(EcosystemConfig.FoliagePeakAutumnBranchyStripActivity),
                "Higher: strip more branchy foliage in peak autumn (0 = keep all branchy). Lower: gentler strip.",
                "Больше: сильнее strip branchy осенью (0 = оставить всё). Меньше: мягче strip.");

            D(nameof(EcosystemConfig.EnableCanopyFallenSticks),
                "On: drop loosestick-free when branchy foliage strips in autumn. Off: no stick drops.",
                "Вкл.: палки loosestick-free при осеннем strip branchy. Выкл.: без палок.");

            D(nameof(EcosystemConfig.CanopyFallenStickChance),
                "Higher (toward 1): more stick drops at peak autumn. Lower (toward 0): rarer sticks.",
                "Больше (к 1): чаще палки в пик осени. Меньше (к 0): реже палки.");

            D(nameof(EcosystemConfig.EnableSpringBranchyAgeBoost),
                "On: spring branchy buds scale with tree calendar age. Off: uniform spring bud strength.",
                "Вкл.: весенние branchy почки зависят от возраста дерева. Выкл.: равномерная сила почек.");

            D(nameof(EcosystemConfig.SpringBranchyAgeBoostYearsToMax),
                "Higher: older trees needed for max spring branchy boost. Lower: young trees reach max sooner.",
                "Больше: для max boost нужны более старые деревья. Меньше: молодые быстрее получают max.");

            D(nameof(EcosystemConfig.SpringBranchyAgeBoostMax),
                "Higher: stronger max spring branchy multiplier from age. Lower: subtler age effect.",
                "Больше: сильнее max множитель branchy от возраста. Меньше: слабее эффект возраста.");

            D(nameof(EcosystemConfig.FoliageRestoreBareSkeleton),
                "On: winter repair adds branchy leaves on bare log-grown pillars. Off: bare crowns stay bare.",
                "Вкл.: зимой branchy на голых log-grown. Выкл.: голые кроны остаются голыми.");

            D(nameof(EcosystemConfig.CanopyActivityScale),
                "Higher: faster seasonal defoliation and budding curves. Lower: subtler canopy seasons. 1.0 = default.",
                "Больше: быстрее сезонные кривые defol/bud. Меньше: слабее сезоны кроны. 1.0 — дефолт.");

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
                "Больше: частицы только под более высокой листвой. Меньше: ambience и в низкой кроне.");

            D(nameof(EcosystemConfig.CanopyAmbienceMoteRate),
                "Higher: denser green mote spawn under canopy. Lower: fewer motes. 1.0 = default.",
                "Больше: гуще зелёные частицы под кроной. Меньше: реже. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.CanopyAmbienceLeafDriftRate),
                "Higher: more autumn leaf drift particles. Lower: sparser drift. 1.0 = default.",
                "Больше: больше осенних листьев. Меньше: реже drift. 1.0 — дефолт.");

            D(nameof(EcosystemConfig.CanopyAmbienceSampleIntervalSeconds),
                "Higher: less frequent canopy density re-sample (less CPU). Lower: more responsive ambience.",
                "Больше: реже пересчёт плотности кроны. Меньше: отзывчивее ambience.");

            D(nameof(EcosystemConfig.CanopyAmbienceSuppressInRain),
                "On: suppress canopy particles during heavy rain. Off: particles still spawn in rain.",
                "Вкл.: частицы гасятся в сильный дождь. Выкл.: частицы и в дождь.");

            D(nameof(EcosystemConfig.EnableChunkFairSpread),
                "On: round-robin spread attempts across registry chunks (fair pacing). Off: less fair chunk order.",
                "Вкл.: round-robin spread по чанкам реестра. Выкл.: менее равномерный порядок.");

            D(nameof(EcosystemConfig.MaxSpreadAttemptsPerChunkPerTick),
                "Higher: more spread attempts per chunk per reproduce tick. Lower: slower per-chunk spread.",
                "Больше: больше spread-попыток на чанк за тик. Меньше: медленнее spread в чанке.");

            D(nameof(EcosystemConfig.MaxSpreadChunksVisitedPerTick),
                "Higher: visit more registry chunks per reproduce tick. Lower: slower global spread sweep.",
                "Больше: больше чанков spread за тик. Меньше: медленнее обход реестра.");

            D(nameof(EcosystemConfig.EnableEventDrivenSpread),
                "On: wake neighbor ecology when relevant blocks change. Off: spread only on scheduled ticks.",
                "Вкл.: пробуждение соседей при изменении блоков. Выкл.: только по расписанию.");

            D(nameof(EcosystemConfig.EnableSeasonCoarseWake),
                "On: wake seasonal species once per in-game month. Off: no monthly coarse wake.",
                "Вкл.: месячное пробуждение сезонных видов. Выкл.: без месячного wake.");

            D(nameof(EcosystemConfig.EcologyWakeRadiusBlocks),
                "0 = auto from spread radius and spacing. Higher: wake more neighbors on block changes.",
                "0 = авто от радиуса spread и spacing. Больше: больше соседей просыпается при изменениях.");

            D(nameof(EcosystemConfig.EnableEcologyColumnCache),
                "On: cache spread column snapshots (faster repeat attempts). Off: rescan columns each time.",
                "Вкл.: кэш снимков колонок spread (быстрее повторы). Выкл.: скан каждый раз.");

            D(nameof(EcosystemConfig.EnableTwoPhaseSpreadPlacement),
                "On: evaluate spread then commit SetBlock in fair pass. Off: immediate placement on success.",
                "Вкл.: оценка spread, затем commit в fair pass. Выкл.: немедленная установка блока.");

            D(nameof(EcosystemConfig.MaxSpreadCommitsPerTick),
                "Higher: more spread block commits per tick (0 = MaxReproduceAttemptsPerTick). Lower: slower commits.",
                "Больше: больше commit spread за тик (0 = MaxReproduceAttemptsPerTick). Меньше: медленнее commit.");

            D(nameof(EcosystemConfig.MaxSpreadCommitChunksVisitedPerTick),
                "Higher: more chunks in commit pass (0 = MaxSpreadChunksVisitedPerTick). Lower: narrower commit sweep.",
                "Больше: больше чанков в commit pass (0 = MaxSpreadChunksVisitedPerTick). Меньше: уже обход.");

            D(nameof(EcosystemConfig.MaxSpreadCommitsPerChunkPerTick),
                "Higher: more commits per chunk per tick (0 = MaxSpreadAttemptsPerChunkPerTick). Lower: slower local commits.",
                "Больше: больше commit на чанк (0 = MaxSpreadAttemptsPerChunkPerTick). Меньше: медленнее локально.");

            D(nameof(EcosystemConfig.MaxReproduceAttemptsPerTick),
                "Higher: more spread evaluations per reproduce tick (faster sim, more CPU). Lower: gentler spread pacing.",
                "Больше: больше оценок spread за тик (быстрее, выше CPU). Меньше: мягче темп spread.");

            D(nameof(EcosystemConfig.MaxChunkColumnsScannedPerTick),
                "Higher: more sync column scans when background scan off. Lower: slower registration catch-up.",
                "Больше: больше sync-сканов колонок без фонового скана. Меньше: медленнее догонка.");

            D(nameof(EcosystemConfig.MaxRegistrationsPerTick),
                "Higher: more sync registrations when background scan off. Lower: slower registry fill.",
                "Больше: больше sync-регистраций без фонового скана. Меньше: медленнее заполнение реестра.");

            D(nameof(EcosystemConfig.EnablePlayerPriorityRegistration),
                "On: drain player-vicinity chunks before background registration queue. Off: uniform queue order.",
                "Вкл.: сначала чанки у игрока, потом фон. Выкл.: единая очередь.");

            D(nameof(EcosystemConfig.EnableBurstRegistrationNearPlayers),
                "On: finish nearby chunk registration on load within ms budget. Off: no burst completion.",
                "Вкл.: burst-дoregistration чанков у игрока при загрузке. Выкл.: без burst.");

            D(nameof(EcosystemConfig.PlayerRegistrationPriorityRadiusBlocks),
                "Higher: wider player-vicinity priority and burst registration. Lower: tighter priority zone.",
                "Больше: шире зона приоритетной/burst регистрации. Меньше: уже зона.");

            D(nameof(EcosystemConfig.MaxPriorityChunkScansPerTick),
                "Higher: more priority queue passes per chunk-scan tick. Lower: slower player-vicinity registration.",
                "Больше: больше приоритетных проходов за тик скана. Меньше: медленнее регистрация у игрока.");

            D(nameof(EcosystemConfig.MaxPriorityRegistrationsPerTick),
                "Higher: more registrations from priority queue per tick. Lower: slower near-player fill.",
                "Больше: больше приоритетных регистраций за тик. Меньше: медленнее у игрока.");

            D(nameof(EcosystemConfig.PriorityRegistrationBudgetMs),
                "Higher: more ms per priority registration pass (smoother, more CPU). Lower: stricter time cap.",
                "Больше: больше мс на приоритетный проход. Меньше: жёстче лимит времени.");

            D(nameof(EcosystemConfig.BurstRegistrationBudgetMs),
                "Higher: more ms to finish one burst chunk on load. Lower: smaller burst completion slice.",
                "Больше: больше мс на завершение burst-чанка. Меньше: меньший slice.");

            D(nameof(EcosystemConfig.MaxBurstRegistrationsPerChunk),
                "Higher: allow more registrations when finishing one burst chunk. Lower: cap burst chunk size.",
                "Больше: больше регистраций при завершении burst-чанка. Меньше: ниже cap.");

            D(nameof(EcosystemConfig.MaxRegistryAppliesPerTick),
                "Higher: more paced RegisterReproducer applies per chunk-scan tick. Lower: slower registry pacing.",
                "Больше: больше RegisterReproducer за тик скана. Меньше: медленнее pacing.");

            D(nameof(EcosystemConfig.MaxPriorityRegistryAppliesPerTick),
                "Higher: more extra applies for player-vicinity chunks. Lower: slower near-player registry.",
                "Больше: больше apply у игрока за тик. Меньше: медленнее реестр у игрока.");

            D(nameof(EcosystemConfig.EnableBackgroundRegistrationScan),
                "On: classify columns on worker from main-thread snapshot. Off: sync scan on main thread only.",
                "Вкл.: классификация колонок на worker из snapshot. Выкл.: только sync на main thread.");

            D(nameof(EcosystemConfig.MaxRegistrationSnapshotCellsPerTick),
                "Higher: copy more block ids to snapshot per main tick. Lower: slower background scan feed.",
                "Больше: больше block id в snapshot за тик. Меньше: медленнее feed фонового скана.");

            D(nameof(EcosystemConfig.TickBudgetMs),
                "Higher: more ms allowed per reproduce tick (smoother, more CPU). 0 = unlimited.",
                "Больше: больше мс за reproduce-тик. 0 = без лимита.");

            D(nameof(EcosystemConfig.SpreadBudgetMs),
                "Higher: more ms for spread phase (0 = TickBudgetMs). Lower: tighter spread cap.",
                "Больше: больше мс на spread (0 = TickBudgetMs). Меньше: жёстче cap spread.");

            D(nameof(EcosystemConfig.RegistrationBudgetMs),
                "Higher: more ms for chunk-scan phase (0 = TickBudgetMs). Lower: tighter registration cap.",
                "Больше: больше мс на chunk-scan (0 = TickBudgetMs). Меньше: жёстче cap регистрации.");

            D(nameof(EcosystemConfig.StressBudgetMs),
                "Higher: more ms for stress phase (0 = TickBudgetMs). Lower: tighter stress cap.",
                "Больше: больше мс на стресс (0 = TickBudgetMs). Меньше: жёстче cap стресса.");

            D(nameof(EcosystemConfig.EnableReproduceTickProfiling),
                "On: log reproduce phase timings when registry is large. Off: no profiling logs.",
                "Вкл.: лог фаз reproduce при большом реестре. Выкл.: без профилирования.");

            D(nameof(EcosystemConfig.ReproduceTickProfilingMinRegistry),
                "Higher: profiling logs only when registry is larger. Lower: logs on smaller registries.",
                "Больше: профиль только при большем реестре. Меньше: логи и на меньшем реестре.");

            D(nameof(EcosystemConfig.ReproduceTickProfilingIntervalMs),
                "Higher: less frequent profiling log lines. Lower: more frequent timing logs.",
                "Больше: реже строки профиля. Меньше: чаще логи таймингов.");

            D(nameof(EcosystemConfig.StressTickIntervalMs),
                "Higher: less frequent stress ticks (less CPU). Lower: more frequent stress updates.",
                "Больше: реже тики стресса (меньше CPU). Меньше: чаще обновления стресса.");

            D(nameof(EcosystemConfig.ReproduceTickIntervalMs),
                "Higher: less frequent spread/foliage/tree ticks. Lower: more frequent spread updates.",
                "Больше: реже тики spread/листва/деревьев. Меньше: чаще обновления spread.");

            D(nameof(EcosystemConfig.ChunkScanTickIntervalMs),
                "Higher: less frequent registration and foliage chunk sync. Lower: faster registry/foliage sync.",
                "Больше: реже chunk-scan (регистрация/sync листвы). Меньше: быстрее sync.");

            D(nameof(EcosystemConfig.OnlyActivateNearPlayers),
                "On: spread, stress, trees, and chunk scans only within player radius (~192 blocks). Off: all loaded chunks.",
                "Вкл.: spread, стресс, деревья и сканы только в радиусе игрока (~192 блока). Выкл.: все загруженные чанки.");

            D(nameof(EcosystemConfig.LimitSpreadNearPlayers),
                "On: spread, stress, and tree aging only near players; chunk registration unchanged. Off: full simulation.",
                "Вкл.: spread, стресс и деревья у игроков; регистрация без изменений. Выкл.: полная симуляция.");

            D(nameof(EcosystemConfig.PlayerActivationRadiusBlocks),
                "Higher: wider radius for OnlyActivateNearPlayers and LimitSpreadNearPlayers. Lower: tighter playtest zone.",
                "Больше: шире радиус для флагов «только у игроков». Меньше: уже зона playtest.");

            D(nameof(EcosystemConfig.VerboseLogging),
                "On: extra notification and warning logs (CPU cost). Off: errors and startup only.",
                "Вкл.: больше notification/warning (нагрузка на CPU). Выкл.: только ошибки и старт.");

            D(nameof(EcosystemConfig.ReproduceDebug),
                "On: log spread attempts (pair with VerboseLogging). Off: silent spread path.",
                "Вкл.: логировать попытки spread (вместе с VerboseLogging). Выкл.: без лога spread.");

            D(nameof(EcosystemConfig.EnableTrampling),
                "On: player proximity accumulates trampling stress on plants. Off: no trampling stress.",
                "Вкл.: близость игрока накапливает стресс протаптывания. Выкл.: без trampling.");

            D(nameof(EcosystemConfig.TramplingRadius),
                "Higher: players affect plants farther away. Lower: must stand closer to trample.",
                "Больше: игрок влияет издалека. Меньше: нужно стоять ближе.");

            D(nameof(EcosystemConfig.TramplingStressThreshold),
                "Higher: more exposure ticks before trampling counts as failed survival. Lower: faster trample kill.",
                "Больше: больше тиков экспозиции до неудачи. Меньше: быстрее гибель от протаптывания.");

            D(nameof(EcosystemConfig.TramplingSoilDegradation),
                "On: degrade soil when plant dies from trampling. Off: trampling kills plants only.",
                "Вкл.: почва деградирует при гибели от протаптывания. Выкл.: только гибель растения.");

            D(nameof(EcosystemConfig.EnableFlowerDrygrass),
                "On: empty hand harvests flower block; knife/scythe yields drygrass. Off: vanilla harvest only.",
                "Вкл.: пустая рука — блок цветка; нож/коса — сухая трава. Выкл.: только ваниль.");

            D(nameof(EcosystemConfig.EnableEcologyInspect),
                "On: allow ecology inspect hotkey (default I). Off: inspect disabled.",
                "Вкл.: осмотр экологии по hotkey (по умолчанию I). Выкл.: осмотр отключён.");

            D(nameof(EcosystemConfig.EcologyInspectCooldownSeconds),
                "Higher: longer wait between inspect requests per player. Lower: more frequent inspects.",
                "Больше: длиннее кулдаун осмотра на игрока. Меньше: чаще осмотр.");

            D(nameof(EcosystemConfig.EcologyInspectScanRadius),
                "Higher: wider nearby-species tally in inspect report. Lower: more local species list.",
                "Больше: шире подсчёт видов в отчёте. Меньше: локальнее список.");

            D(nameof(EcosystemConfig.EnableEcologyAreaScan),
                "On: include area species mix in inspect report. Off: target block only.",
                "Вкл.: в отчёте — смесь видов вокруг. Выкл.: только целевой блок.");

            D(nameof(EcosystemConfig.CloneBerryTraits),
                "On: spread copies parent bush genetic traits. Off: vanilla random wild traits on new bushes.",
                "Вкл.: spread копирует генетические черты куста. Выкл.: ванильные случайные черты.");

            D(nameof(EcosystemConfig.BerryTraitMutationChance),
                "Higher: offspring more often lose one random trait on spread. 0 = no mutations.",
                "Больше: чаще теряется случайная черта при spread. 0 = без мутаций.");

            D(nameof(EcosystemConfig.EnableThirdPartyParticipants),
                "On: blocks with ecologyParticipant JSON from other mods join ecology. Off: vanilla blocks only.",
                "Вкл.: блоки с ecologyParticipant из других модов (напр. Wildgrass). Выкл.: только ваниль.");

            return m;
        }
    }
}
