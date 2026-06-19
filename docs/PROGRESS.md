# Прогресс разработки

**Текущая стадия:** `Ecosystem v3.8.0` — Phase 6 simulation engine (chunk-fair spread, event wake, two-phase placement, registration priority, empty-first spread); prior **3.7.x** tree fern / canopy / wild vines / extended flora.  
**Версия мода:** `3.8.0` · **Игра:** Vintage Story 1.22+ · **Сборка:** .NET 10 · **Тесты:** xUnit (**332**)  

**ModDB:** https://mods.vintagestory.at/ecosystemflora  

Последнее обновление документации: 2026-06-18 (код **3.8.0**, Phase 6 + registration perf, [`CHANGELOG.md`](CHANGELOG.md)).

См. также: [PROJECT_VISION.md](PROJECT_VISION.md) (теория), [PROMPT.md](PROMPT.md) (промпт для агентов), [CHANGELOG.md](CHANGELOG.md) (релизные заметки), [THIRD_PARTY_ECOLOGY.md](THIRD_PARTY_ECOLOGY.md) (сторонние блоки), [GAPS.md](GAPS.md) (пробелы идеи), [CANOPY_PHENOLOGY.md](CANOPY_PHENOLOGY.md) (сезонная листва), [CANOPY_AMBIENCE.md](CANOPY_AMBIENCE.md) (частицы кроны), [TREE_AGING.md](TREE_AGING.md) (зрелость диких деревьев), [FERNTREE.md](FERNTREE.md) (древовидный папоротник), [WILD_VINE.md](WILD_VINE.md) (дикие лианы).

---

## Стадии проекта

| Стадия | Описание | Статус |
|--------|----------|--------|
| **0 — Archive** | Оригинал JakeCool19 v1.2.0, legacy-код | ✅ в репо, не в сборке |
| **1 — MVP-alpha** | Экосистемное ядро, wildplant + семена (устаревший путь) | ✅ архив идей |
| **2 — MVP-beta** | Reproduce, рецепты, survival | ✅ superseded |
| **3 — MVP-vanilla-flowers** | Только `game:flower-*`, оптимизация, playtest | ✅ завершено |
| **4 — Ecosystem v1** | Участники, rain/forest, выбор клеток, скорость по виду | ✅ завершено |
| **4.1 — Spacing + calendar** | Дистанция между растениями, spread по игровому году | ✅ в main |
| **4.2 — Aquatic** | Рогоз, камыш, кувшинка, водяной лютик | ✅ код + playtest (2026-05-24) |
| **5 — Content** | Папоротники, деревья, ягоды, tallgrass | ✅ в main |
| **6 — Ecosystem v2.1** | Displacement, stress death, symbiosis, flora context | ✅ в main; playtest ✅ (2026-05-22) |

---

## Текущая модель (Ecosystem v3.8.0)

Дикая экосистема на **ванильных** блоках родителей (`game:`). Сторонние blocktypes — **`ecologyParticipant`** ([`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)). Валидация баланса: **логи** + **осмотр (I)** — mat edge, seed %, stress, season, **грибница** (ниша, кромка сети, стресс якоря).

**Конкуренция за клетку:** spread + displacement + stress + symbiosis. Aquatic reeds/lily — **mat spread** (edge + rare seed), не independent radius-4.

**Сбор луга (v3.1.7):** пустой слот hotbar → блок цветка или tallgrass (`PlantHandHarvest` на `DidBreakBlock`); нож/коса → drygrass (`FlowerDrygrassDrops` для цветов, ваниль для tallgrass). Патч косы: `flower-` в `codePrefixes`. Выключить: `EnableFlowerDrygrass`.

**Legacy BE (v3.1.8):** `LegacyBlockEntityMigration` регистрирует `EcoSystemLife` и `EcosystemPlant` только для десериализации старых чанков; через ~200 ms после загрузки колонны BE удаляется. Новые растения BE не получают. Для полной очистки сохранения — один проход с модом + save; без мода VS отбрасывает orphan BE (VerboseDebug).

| Группа | Блоки |
|--------|--------|
| **Цветы** | `game:flower-*` (20 видов) + `flower-lupine-*` |
| **Луг** | `tallgrass-*` — матрица под цветами |
| **Тростник** | `tallplant-coopersreed-*` (рогоз), `tallplant-tule-*` (камыш), `tallplant-papyrus-*` (папирус) |
| **Поверхностная вода** | `waterlily` (кувшинка) |
| **Подводный** | `aquatic-watercrowfoot-*` (водяной лютик: section / tip / top) |
| **Деревья** | `log-grown-{wood}-*` → spread `sapling-{wood}-free` (14 пород); **v3.6** — полный цикл (рост, senescence, пень/бревна) — [`TREE_AGING.md`](TREE_AGING.md) |
| **Древовидный папоротник** | `ferntree-normal-*` — spread колонны, aging, senescence — [`FERNTREE.md`](FERNTREE.md) |
| **Дикие лианы** | `wildvine-end-*` / tropical — рост вниз + захват стен — [`WILD_VINE.md`](WILD_VINE.md) |
| **Ягоды** | `fruitingbush-wild-*` (10 видов) → тот же блок; почва + `LocalForestCover` |
| **Папоротники** | `fern-*` (4) + `tallfern` — лес/влажность, свет ≥7 под кроной |
| **Грибница** | Vanilla `BlockEntityMycelium` под `soil-*` / `forestfloor` / ствол — ниша, стресс, медленный network spread (**3.1.12**) |

| Слой | Поведение |
|------|-----------|
| **Объект** | Ванильные блоки; регистрация — `RegistrationScanQueue` + burst; **Phase A** `PendingRegistrationQueue`; **background scan** (snapshot → worker: цветы / лианы / деревья); **грибница** — `MyceliumChunkRegistrar` при load (BE-якоря, main); сезонная крона — `FoliageChunkSyncPass` на main |
| **Участник** | `IEcosystemParticipant` → `EcosystemParticipant` |
| **Среда** | Температура (сезон), `WorldgenRainfall`, **`LocalForestCover`** (соседние стволы), почва, жидкость |
| **Клетка-кандидат** | Скан в радиусе → **взвешенный** выбор по fitness |
| **Скорость** | `SpreadRate` per-species; календарь (`ReproduceAttemptsPerYear`) или legacy часы |
| **Spacing** | `WildFlowerSpacing` + конфиг; у тростника нет вертикального стека в колонке |
| **Displacement** | `UseCellDisplacement`, `DisplacementHoldMargin` — challenger вытесняет weaker hold |
| **Stress** | `EnableStressDeath` — N failed survival checks → снятие блока |
| **Symbiosis** | `FloraSymbiosis` — папоротники/лесные виды без дерева-хоста → stress |
| **Flora context** | `FloraContextSampler` — open / edge / forest interior как множитель fitness |
| **Почва** | `SoilKind` + `WildPlantSoil` — high/medium/low, песок, глина, торф, гравий; min/max `block.Fertility` |
| **Семена мода** | Не в сборке |

### Среда обитания (`EcologyHabitat`)

| Habitat | Виды | Размещение |
|---------|------|------------|
| `Terrestrial` | Цветы, люпин | `SurfacePlacement` — твёрдая почва, без жидкости в клетке |
| `ReedNearWater` | Рогоз, камыш | `ReedPlacement` — см. правила ниже |
| `WaterSurface` | Кувшинка | `WaterPlacement` — на открытой поверхности воды |
| `UnderwaterColumn` | Водяной лютик | `CrowfootPlacement` + `CrowfootColumnPlacer` — колонка section → tip/top |
| `TerrestrialTree` | Зрелый ствол `log-grown` | `TreePlacement` — саженец; **v3.2** — `CanopyPhenology` (deciduous defol/bud); **v3.7** — палки, age-branch buds — [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md); рост саженца — **vanilla** treegen |
| `Ferntree` | `ferntree-normal-trunk` | Young column spread; yearly crown maturity + height — [`FERNTREE.md`](FERNTREE.md) |
| `WildVine` | `wildvine-end-*` tips | Column pass at chunk load → paced registry; spread on reproduce tick — [`WILD_VINE.md`](WILD_VINE.md) |
| `MyceliumAnchor` | Vanilla `BlockEntityMycelium` | `MyceliumChunkRegistrar` at chunk load (BE scan); network spread + stress on reproduce tick (chunk-fair with vines) |

### Грибница (v3.1.12)

Мод **не** добавляет свои грибы — только экология вокруг vanilla `BlockEntityMycelium` (growRange 7).

| Слой | Поведение |
|------|-----------|
| **Ниша (`MyceliumZone`)** | Луговые виды — штраф spread рядом с **лесной** грибницей; лесной understory — бонус. **Луговая** грибница **не** отталкивает цветы/траву (`MyceliumCoexistence`). |
| **Hard block** | Spread растений на клетку с активным mycelium BE — запрещён (`WildSoilGroundRules.HasActiveMycelium`). |
| **Coexistence** | Луговая грибница: цветы/трава могут spread **на** клетку с BE; луговой network может расти **под** цветком/травой (`MyceliumCoexistence`). Лесная грибница — штраф spread луга в зоне, без coexistence. |
| **Стресс (`EnableMyceliumEcology`)** | Луг в лесу / лес без tree-host → failed checks → `RemoveBlockEntity`. Рубка дерева — каскад для лесных якорей (`MyceliumTreeCascade`); луг и polypore на стволе — без каскада. |
| **Network spread** | Ортогональный шаг с кромки матa; displacement между видами по fitness; клон vanilla BE (`MyceliumAnchorSpawner`). Участвует в **chunk-fair spread** reproduce-тика (как лианы). |
| **Регистрация** | При load чанка — `MyceliumChunkRegistrar` (перечень BE в колонке, ~250 ms delay); не через worker column snapshot, но тот же реестр и reproduce loop, что у lianas. |
| **Осмотр (I)** | Шляпка `mushroom-*` или почва (`soil-*`, `forestfloor`, торф, ствол) — клиент шлёт запрос без локального BE; сервер строит отчёт. |
| **Почва** | `MyceliumSkipSoilSuccession` — без сукцессии/fallow drip на якорной клетке с BE. |

Конфиг: см. таблицу ниже. **Обновление:** при старте мода отсутствующие ключи получают C#-дефолты и **дописываются в** `ModConfig/ecosystemflora.json` (`StoreModConfig` после load).

### Деревья (вариант A)

Полный цикл — [`TREE_AGING.md`](TREE_AGING.md) § Lifecycle:

1. **Семя** — зрелый `log-grown` ставит `sapling-*-free` (зима выкл., не на лёд/снег).
2. **Рост** — ванильный treegen; при появлении `log-grown` — регистрация с **возрастом 0**.
3. **Жизнь** — раз в игровой год: возраст +1, укрупнение, spread; ствол **не** гибнет от стресса.
4. **Senescence** — после lifespan породы: **4 года** (листва → остов → snag → **пень + бревна**).
5. **После** — останки = ванильные `log-*` (не `log-grown`); дыру заполняют соседние деревья через spread.

- **Родитель:** основание колонны `log-grown-{wood}-*` (`WildTreeEcology` — климат, rain/forest, `SpreadRadius`, spacing).
- **Потомство:** `sapling-{wood}-free`; вырастет/погибнет — только игра.
- **Регистрация:** при загрузке чанка (скан колонки) + `PendingTreeSaplings` для саженцев, посаженных модом, пока не появится `log-grown`.
- **3.1.11:** spread через `PassesTerrestrialPhysical` (почва, fluid, mycelium); лёд/снег — `IsUnplantableGround`; зимний spread multiplier = 0 (ноя–фев). Stress death по-прежнему только `Terrestrial`, не стволы.
- **Без** living trunk / mod-блоков ствола; при снятии мода — обычные бревна и саженцы.

### Рогоз, камыш, папирус, кувшинка (spread v3.1.3–3.1.6)

**Reeds (`coopersreed`, `tule`, `papyrus`):** при `UseRhizomeSpreadForReeds` (default) — **ризомный ковёр**: spread только с **кромки** стоянки, шаг **±1** по X/Z; редкий **seed/fragment** (~6–10% попыток, радиус 5–6). `SpreadRate`: 1.0 / 0.85 / 0.75. Legacy: `UseRhizomeSpreadForReeds: false`.

**Кувшинка:** при `UseSurfaceMatSpreadForLilies` — **плавучий ковёр** (8-соседняя кромка, диagonali OK); seed ~5%, radius 4; `SpreadRate` 1.2.

**Осмотр (I):** spread mode, mat edge yes/no, seed % — см. lang `inspect-line-spread-mode-*`, `inspect-line-mat-frontier-*`. **Грибница:** `inspect-line-mycelium-*`, `mycelium-niche-*` (шляпка или почва).

Правила **размещения** (без изменений):

```
[ поверхность — воздух ]
[ один блок воды — сюда рогоз, water-normal ]
[ дно — muddygravel или `gravel-*`; land — вода в радиусе 3 ]
```

| Случай | Клетка растения | Вариант блока |
|--------|-----------------|---------------|
| Берег, без водной колонки | `bed.Y + 1`, вода в радиусе 3 | `land-normal` |
| Мелководье, ровно 1 вода над илом | тот же `gravel.Y + 1`, **внутри** водного блока | `water-normal` |
| 2+ воды между илом и поверхностью | — | spread **запрещён** |
| Дно не muddy/rock gravel | — | spread **запрещён** |
| Уже есть рогоз в колонке (X/Z) | — | spread **запрещён** |

**Камыш (tule):** те же правила размещения, что у рогоза; умеренный климат (5–25°C); ризомный spread чуть медленнее рогоза.

**Папирус:** высота 2 блока; в воде — ровно 1 водный слой над илом; жаркий климат (24–40°C).

### Водяной лютик

**Spread:** independent (радиус конфига, `SpreadRate` 2.0) — не mat; см. [GAPS.md](GAPS.md).

- Spread ставит колонку от `aquatic-watercrowfoot-section`, сверху `tip` или `top` (~35% с цветами).
- Глубина воды над илом/почвой: 2–8 блоков (как worldgen).
- Реестр привязан к **нижнему** блоку колонки (`GetColumnBase`).

### Архитектура (код)

- [x] `EcosystemSystem`, `ReproducerRegistry`, `ChunkFlowerScanner`, `EcologyInspectService`, `EcologyInspectServerSystem`, канал protobuf `ecosystemflora-ecologyinspect`
- [x] `MyceliumEcology`, `MyceliumZone`, `MyceliumCoexistence`, `MyceliumNetworkSpread`, `MyceliumStressEvaluator`, `MyceliumTreeCascade`, `MyceliumInspect`, `MyceliumChunkRegistrar`, `MyceliumAnchorSpawner` — грибница v3.1.12
- [x] `EnvironmentalContext` — rainfall (worldgen) + `LocalForestCover` (соседние деревья)
- [x] `SpreadVacancy` — spread aquatic в водные клетки (не только air)
- [x] `SuitabilityEvaluator` — ветки по `EcologyHabitat`
- [x] `WildFlowerClimate` + `WildAquaticEcology`
- [x] `WildFlowerSpacing` + `PlantSpacing` (горизонталь + запрет стека тростника в колонке)
- [x] `SpeciesSpread` — календарь и legacy
- [x] `ReproducePlacement`, `SurfacePlacement`, `ReedPlacement`, `WaterPlacement`
- [x] `CrowfootPlacement`, `CrowfootColumnPlacer`
- [x] `BlockFluidHelper` (core fluid primitives) + `ReedColumnHelper` + `WaterColumnHelper`
- [x] `PlantCodeHelper` — species, `ResolveReedSpreadBlock` (land/water)
- [x] `EcosystemParticipant`
- [x] `CellCompetition`, `EcologySpreadFitness`, `FloraContextSampler`, `FloraSymbiosis`, `WildSpeciesModifiers`
- [x] Legacy не в сборке

### Патчи (`enabledpatches.json`)

Пусто — патчи `entityClass` удалены (регистрация через `ChunkFlowerScanner`). Старые BE: `LegacyBlockEntityMigration` (`src/Ecosystem/LegacyBlockEntityMigration.cs`) — отложенное удаление при загрузке колонны.

### Исправления (после v1)

| Проблема | Решение |
|----------|---------|
| Spread почти не работал | `WorldGenValues` для rain; лес — `LocalForestCover` |
| «То есть, то нет» на поляне | Пул кандидатов; fitness = min факторов |
| Реестр сбрасывался зимой | `SameEcologySpecies` для `-free`/`-snow` |
| Одинаковая скорость видов | `SpreadRate` + `UseSpeciesSpreadRates` |
| NLR на calendar в `Init` | Лог календаря на первом reproduce-тике |
| Рогоз без гравия | `muddygravel` или `gravel-*` (гранит и т.п.); привязка к колонке дна |
| Рогоз в воздухе / столб воды | Только `gravel+1`; один водный слой; без `gravel+2` |
| Рогоз на рогозе | Запрет стека в колонке; не считать чужой тростник «водой» |
| Spread не в водный блок | `IsDedicatedWaterCell` + `water-normal` по клетке |
| Камыш / лютик | `VerticalBlocks`, `UnderwaterColumn`, колонка лютика |
| Aquatic spread в воду | `SpreadVacancy` — вода не считается «занятой» клеткой |
| Рогоз на лугу | Только `muddygravel`/`gravel-*`; land — вода в 3 блоках |
| Подводное дно озёр | `gravel-granite` и др. как reed bed (не только `muddygravel`) |
| `ProcessStress` IndexOutOfRange при stress death | Удаление растений отложено до конца обхода; live `entries.Count` в round-robin |
| Ложный `noSurf` на пустой яме | Air + replaceable; mycelium только при активном `BlockEntityMycelium` (не `MyceliumHost` на почве) |
| `preflight` при снеге/мусоре | Spread placement — только **air**; replaceable debris не перезаписывается (факелы, loosestones). Скан колонны по-прежнему проходит сквозь replaceable |
| Цветы на грибнице | Spread запрещён только при **активном** mycelium BE под клеткой |
| Осмотр (I) краш GUI | `EcologyInspectDialog` — фиксированный размер; **3.1.10:** compose в `OnGuiOpened`, не `SingleComposer = null` |
| Legacy BE не очищались | `ScheduleStripColumn` собирает позиции **после** десериализации BE (callback по `chunkCoord`) |
| Spread съедает факелы / loosestones | `IsVacantPlantSpace` — только air; replaceable ≥5000 больше не считается пустой клеткой |
| Terrain Slabs → полный блок при barren | `SoilSuccessionGuard` — защита **самого** ground (`terrainslabs:*`, path `*slab*`); v3.1.2 смотрел только блок сверху |

### Playtest

| Область | Статус |
|---------|--------|
| MVP-vanilla-flowers | ✅ |
| Ecosystem v1 (rain, rates) + local forest | ✅ |
| Spacing + calendar | ✅ в коде |
| **v2.1 — spread + displacement на лугах** | ✅ сессия 2026-05-22: пустая/редкая поверхность пышно зарастает; видимое замещение блоков |
| Рогоз: ил → 1 вода → рогоз | ✅ по отчёту пользователя |
| **Aquatic** (рогоз, камыш, лютик, `gravel-*`) | ✅ 2026-05-24: spread в воду/берег; луг не захватывается |
| Покос → быстрые виды → вытеснение | ✅ проверено 2026-05-26 |
| Symbiosis cascade при сломе дерева | ✅ проверено 2026-05-26 |
| Trampling (протаптывание тропинок) | ✅ проверено 2026-05-26 |

**Aquatic (закрыто 2026-05-24):** `gravel-*` под водой; берег — land-normal у воды; `SpreadVacancy`; без spread на луг. Опционально позже: камыш в жаре, лютик на глубине 8+.

---

## Конфиг (`ecosystemflora.json`)

Файл: `%AppData%/Vintagestory/ModConfig/ecosystemflora.json` (сервер; клиент — своя копия, если есть). Пример всех ключей: `assets/ecosystemflora/ecosystemflora.example.json`. **v3.1.12:** после успешной загрузки мод **дописывает** отсутствующие ключи с C#-дефолтами (`EcosystemConfig.TryLoadFromDisk` → `StoreModConfig`).

| Параметр | Назначение |
|----------|------------|
| `ReproduceRadius` / `ReproduceVerticalSearch` | Поиск клеток |
| `ReproduceChance` | Базовый шанс (× SpreadRate вида) |
| `ReproduceAttemptsPerYear` | Попыток spread за **игровой год** при SpreadRate=1 |
| `UseCalendarScaledSpread` | Интервал в игровых днях |
| `ReproduceIntervalHours` | Legacy, если calendar off |
| `MinSpeciesReproduceIntervalDays` / `Hours` | Пол между попытками (0 = без пола) |
| `MinFitness` | Порог fitness (0–1) |
| `ApplyWorldgenRainForest` | Только rainfall из worldgen; лес — `LocalForestCover` |
| `UseSpeciesSpreadRates` | Per-species `SpreadRate` |
| `PlantSpacingEnabled` | Дистанция между растениями |
| `DefaultSameSpeciesSpacing` / `DefaultOtherSpeciesSpacing` | Базовые дистанции |
| `SpacingVerticalSearch` | ±Y при проверке соседей |
| `TickBudgetMs` | Жёсткий потолок ms/тик для spread (default 30); 0 = без лимита |
| `MaxReproduceAttemptsPerTick` | Лимит CPU (spread) |
| `MaxStressChecksPerTick` | Лимит CPU (stress) |
| `MaxChunkColumnsScannedPerTick` / `MaxRegistrationsPerTick` | Обработка очереди чанков за тик; скан продолжается до конца чанка через курсор (см. v2.11.2) |
| `EnableEcologyInspect` | Осмотр растения по хоткею (**I**): запрос → отчёт по сети |
| `EcologyInspectCooldownSeconds` | Кулдаун между запросами осмотра |
| `EcologyInspectScanRadius` | Радиус зонального скана (доминанты в `SpacingIndex`, 4–32) |
| `EnableEcologyAreaScan` | Включить блок «экология рядом» в отчёте |
| `CloneBerryTraits` | **v3.0:** при spread ягоды клонировать черты родителя (`BEBehaviorFruitingBush.OnGrownFromCutting`; default **true**) |
| `EnableThirdPartyParticipants` | **v3.1:** блоки с `ecologyParticipant` + `ecologySpecies` + `ecologySpreadBlock` из любых доменов модов (default **true**) |
| `BerryTraitMutationChance` | **v3.1.1:** шанс потери одного trait при spread ягод (default **0**) |
| `UseSoilSuccession` | Смена tier почвы при spread/death; **false** = только spread без подмены soil |
| `SoilSuccessionSkipWhenBuiltAbove` | **v3.1.2:** не менять почву под slab/постройкой (default **true**) |
| `UseRhizomeSpreadForReeds` | **v3.1.3–5:** ризомный spread reeds; **false** = legacy radius |
| `UseSurfaceMatSpreadForLilies` | **v3.1.5:** ковёр кувшинки на воде |
| `RhizomeSeedDispersalEnabled` / `RhizomeSeedDispersalChanceScale` / `RhizomeSeedDispersalFitnessScale` | **v3.1.4:** редкий seed/обломок для mat spread |
| `SoilSuccessionStrength` | Множитель силы сукцессии |
| `ReproduceDebug` / `VerboseLogging` | Лог spread / Skip; master-switch логирования |
| `OnlyActivateNearPlayers` | Default **false** — все зарегистрированные растения в **загруженных** чанках; **true** = playtest/perf: spread, stress, tree aging **и chunk scans** только в радиусе игрока |
| `LimitSpreadNearPlayers` | Default **false**; **true** (при `OnlyActivateNearPlayers: false`) — spread, stress и tree/ferntree aging только в registry chunks в радиусе `PlayerActivationRadiusBlocks`; **регистрация чанков не ограничена** |
| `PlayerActivationRadiusBlocks` | Радиус для `OnlyActivateNearPlayers` / `LimitSpreadNearPlayers` (192 по умолчанию) |
| `UseCellDisplacement` / `DisplacementHoldMargin` | Вытеснение занятых ecology-клеток |
| `EnableStressDeath` / `MaxFailedSurvivalChecks` | Стресс-смерть при несоответствии нише |
| `EnableSymbiosis` / `UseFloraContext` | Симбиоз с деревьями; локальный forest-edge контекст |
| `RespectLandClaims` | Нет spread/displace/stress/soil внутри land claim |
| *(hardcoded)* | Spread запрещён на farmland; на клетку с **активным** mycelium BE (`HasActiveMycelium`) |
| `EnableMyceliumNiche` | Штраф луга / бонус леса в радиусе якоря (default on) |
| `MyceliumZoneRadius` | Радиус зоны (default 7, как vanilla growRange) |
| `MyceliumMeadowSpreadPenalty` | Множитель fitness луга у якоря (default 0.35, taper к 1.0 на краю зоны) |
| `MyceliumForestSpreadBonus` | Множитель fitness лесного understory у якоря (default 1.22) |
| `MyceliumSkipSoilSuccession` | Не менять почву / fallow на клетке с mycelium BE (default on) |
| `EnableMyceliumEcology` | Регистрация BE, стресс/смерть якоря, осмотр грибницы |
| `EnableMyceliumNetworkSpread` | Медленный spread сети по кромке |
| `MyceliumSpreadRate` / `MyceliumSpreadAttemptsPerYear` | Темп network spread (default 0.12 / 4 yr) |
| `MyceliumSpreadMinFitness` | Min fitness для colonize/displace соседнего якоря (default 0.35) |
| `MyceliumTreeHostRadius` | Поиск tree-host для лесной грибницы (default 4) |
| `MyceliumForestMinForestCover` / `MyceliumMeadowMaxForestCover` | Пороги стресса лес/луг (default 0.12 / 0.45) |
| *(config merge)* | **v3.1.12:** после load `StoreModConfig` — новые ключи дописываются в json на диск |
| `UseSeasonalEcology` / `SeasonalStressEnabled` | Spread и зимняя/осенняя stress по сезону (`WildSpeciesSeason`) |
| `EnableTrampling` / `TramplingRadius` / `TramplingStressThreshold` | Протаптывание (default **off**): растения гибнут рядом с часто ходящими игроками |
| `TramplingSoilDegradation` | Деградация почвы на протоптанных тропах (default **off**) |
| `EnableFlowerDrygrass` | Пустая рука → блок цветка/tallgrass; нож/коса → drygrass (`PlantHandHarvest`, патч косы `flower-`) |
| `EnableTreeAging` / `EnableTreeSenescence` / `TreeSenescenceSnagBlocks` | **v3.6:** годовой рост; поэтапная senescence (4 года); возраст и фаза в savegame moddata — [`TREE_AGING.md`](TREE_AGING.md) |
| `EnableFerntreeEcology` / `FerntreeSenescenceSnagSegments` | **v3.7:** древовидный папоротник — [`FERNTREE.md`](FERNTREE.md) |
| `FoliagePeakAutumnBranchyStripActivity` / `EnableCanopyFallenSticks` / `CanopyFallenStickChance` | **v3.7:** частичный strip branchy; палки под кроной — [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md) |
| `EnableSpringBranchyAgeBoost` / `SpringBranchyAgeBoostYearsToMax` / `SpringBranchyAgeBoostMax` | **v3.7:** весенние ветви по возрасту дерева |
| `EnableWildVineEcology` / `WildVineWallCaptureRadius` / `WildVineWallCaptureHeight` | **v3.7:** лианы — [`WILD_VINE.md`](WILD_VINE.md) |
| `MaxTreeGrowthAttemptsPerTick` / `TreeGrowthActivityScale` | Round-robin деревьев за reproduce tick; темп роста |
| `BalancePreset` | `natural` / `lush` / `sparse` — пресеты spread |

**Рекомендуемая «естественная» база:** `BalancePreset: natural` или `lush` для более плотного луга; не тестовые `ReproduceChance: 1` / `MinFitness: 0.1` в финальной игре.

---

## Виды в экосистеме

**Цветы (20):** catmint, cornflower, forgetmenot, edelweiss, heather, horsetail, orangemallow, wilddaisy, westerngorse, cowparsley, goldenpoppy, lilyofthevalley, woad, redtopgrass, bluebell, ghostpipewhite, ghostpipepink, ghostpipered, daffodil, mugwort.

**Дополнительно:** lupine (`flower-lupine-*`).

**Водная флора:**

| species | RU | Примечание |
|---------|-----|------------|
| `coopersreed` | рогоз | ризомный mat; ил + 0–1 вода |
| `tule` | камыш | ризомный mat; 5–25°C |
| `papyrus` | папирус | ризомный mat; 24–40°C; 2 блока |
| `waterlily` | кувшинка | плавучий mat; поверхность воды |
| `watercrowfoot` | водяной лютик | independent spread; подводная колонка |

---

## Roadmap / TODO

Mod DB — **опубликовано** 2026-05-26. Актуальные пробелы идеи — **[GAPS.md](GAPS.md)**. Валидация: **логи** + **осмотр (I)**.

**Релизы 3.1.x (main):**

| Версия | Содержание |
|--------|------------|
| **3.1.0** | Third-party JSON participants |
| **3.1.1** | `BerryTraitMutationChance` |
| **3.1.2** | Soil succession balance, Terrain Slabs guard |
| **3.1.3–5** | Aquatic mat spread A–C (rhizome, seed, lily mat) |
| **3.1.6** | Handbook/inspect/docs D |
| **3.1.7** | Meadow harvest: hand → plant block; knife/scythe → drygrass; scythe mows all flowers |
| **3.1.8** | Legacy BE migration (`EcosystemPlant` + `EcoSystemLife`); fix inspect dialog (I); handbook `{{wood}}` VTML |
| **3.1.9** | Spread — только air (не перезаписывает факелы/loosestones); `SoilSuccessionGuard` защищает ground `terrainslabs:*` |
| **3.1.10** | Meadow harvest: цветы → дроп в мир; tallgrass без дропа (только коса/нож); `MeadowHarvestRegistry`; fix inspect (I) NRE (`SingleComposer`); конфиг на клиенте |
| **3.1.11** | Tree spread: не на лёд/снег (`IsUnplantableGround`); `TerrestrialTree` → terrestrial preflight; зимний spread = 0; `PlantGroundRulesTests` |
| **3.1.12** | Mycelium niche + ecology + network spread; meadow coexistence; inspect (I) по шляпке и почве; config auto-merge; `Mycelium*Tests` |
| **3.2.0** | **Canopy phenology** — deciduous partial defol (autumn scan+update), spring bud on skeleton; in-RAM CA, no disk; [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md) |
| **3.5.0** | **Canopy ambience** — client view-distance particles, flutter leaves; autumn crown sync fix; [`CANOPY_AMBIENCE.md`](CANOPY_AMBIENCE.md) |
| **3.6.0** | **Wild tree maturation** — calendar age + structure growth; **phased senescence** (4 yearly stages → stump/logs); age + phase persist in savegame; inspect (I) on trunk; [`TREE_AGING.md`](TREE_AGING.md) |
| **3.7.0** | **Tree fern** (`ferntree`); **canopy** — partial branchy strip, fallen sticks, age-scaled spring branches; **wild vines** — tip spread down + wall capture; [`FERNTREE.md`](FERNTREE.md), [`WILD_VINE.md`](WILD_VINE.md) |
| **3.8.0** | **Phase 6 simulation engine** — chunk-fair spread, event wake, column cache, two-phase placement, season coarse wake; **player-priority registration** + burst; **empty-first spread** + column occupancy hint (displacement preserved); [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) |

**Следующие итерации:**

| Версия | Содержание | Статус |
|--------|------------|--------|
| **v3.0** | Ягодные кусты 1.22 — trait inheritance при spread | [x] код (playtest пользователя) |
| **v3.1** | Attribute-based participant contract — сторонние моды без хардкода | [x] код (+ [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)) |
| **v3.1.x polish** | Crowfoot; de handbook; dominant UX | [ ] [GAPS.md](GAPS.md) |

### Дорога к ModDB (план)

| Шаг | Содержание | Статус |
|-----|------------|--------|
| 1 | **Сезонность** — множители spread/stress по месяцу/сезону, зимняя выживаемость per species | [x] MVP |
| 2 | **Strip** legacy JakeCool (мёртвый код/assets вне сборки) | [x] done |
| 3 | **Rename** `EcosystemPlant` → `EcoSystemLife` | [x] done |
| 4 | Рефактор + **perf-анализ** (фаза 3 — по результатам) | [x] done |
| 5 | **Unit-тесты** — чистые функции (scoring, classification, season, mat spread, hand harvest, mycelium, canopy, tree growth/senescence, ferntree, vines, config merge, Phase 6 schedulers) | [x] 332 tests |
| 6 | **Разбить `BlockFluidHelper`** → `ReedColumnHelper`, `WaterColumnHelper` | [x] done |
| 7 | ModDB + hotfix по отзывам | [x] опубликовано 2026-05-26 |
| 8 | **3.6.0** — tree aging, handbook en/ru, [`CHANGELOG.md`](CHANGELOG.md), [`MODDB_DESCRIPTION.md`](MODDB_DESCRIPTION.md) release block | [x] docs готовы; [ ] push + upload ModDB |
| 9 | **3.7.0** — ferntree, canopy sticks/vines, docs | [x] код + docs; [ ] playtest + ModDB |

**Canopy phenology (v3.2):** deciduous partial defol + spring bud — [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md). Старый backlog «зимняя листва как визуал-only» superseded для deciduous.

### v2.3 — сезонность

- [x] `WildSpeciesSeason` — spread по `EnumSeason`, `WinterSurvival`, `FallDieoffChance`
- [x] `SeasonEcology` — chance, interval, fitness; весенний ramp (`GetSeasonRel`)
- [x] Зимнее/осеннее отмирание — `SeasonalStressEnabled` (terrestrial stress)
- [x] **12-месячные кривые per species** — `WildSpeciesSeason` (`float[12]` spread + stress), `SeasonEcology.SpreadMultiplierInterpolated`; шаблоны видов + fallback

Mod DB — **опубликовано** (2026-05-26); линейка релизов **2.10.x** (2026-05-27) → **2.11.x** (inspect, возобновляемый chunk-scan, локализация диалога).

### v1.x — контент и баланс

- [x] **Tallgrass** в экосистеме
- [x] Патч **drygrass** / **сбор луга** — рука → блок (цветок, tallgrass); нож/коса → drygrass (`PlantHandHarvest` + `FlowerDrygrassDrops`)
- [x] Пресеты баланса `natural` / `lush` / `sparse`

### v2.1 — единая конкуренция за клетку (§11 PROJECT_VISION)

**Один механизм:** spread + displacement + stress death + symbiosis. Без `DisturbedTracker` / colonizer window.

- [x] `CellCompetition` — spreadScore vs holdScore, displacement
- [x] `FloraContext` как множитель fitness (опушка emergent)
- [x] `EnableStressDeath` на `EcoSystemLife`
- [x] `FloraSymbiosis` — каскад при смерти дерева/хоста
- [x] `WildSpeciesModifiers`: `HoldStrength` вместо disturbed/colonizer
- [x] Playtest v2.1 (2026-05-22): луга, опушка, покос, symbiosis, aquatic
- [x] Aquatic shore spacing — `ApplyCrossHabitatSpacing` (default false)
- [x] Config template v2.1 — `ecosystemflora.example.json`
- [x] Land claims — `RespectLandClaims`, `LandClaimGuard` (spread, displace, stress, soil, crowfoot)
- [x] Тюнинг `HoldStrength` / `DisplacementHoldMargin` — hold без SpreadRate, margin 1.18, soft empty preference, таблица видов

### v2.2 — ниша: почва, влажность, освещение

**Сейчас:** `SoilKind` + fertility, `FloraContext`, **v2.2** `MoistureLevel`/`LightLevel` (`NicheSampler`, `WildSpeciesNiche`) — мягкий spread + stress вне ниши. Playtest (2026-05): OK для луга/опушки; лес у воды — отложен.

**Цель:** таблица предпочтений per-species — тип почвы + **уровень влажности** + **уровень света** — чтобы хвощ/ландыш/луговые цветы занимали разные микрониши без только symbiosis.

- [x] **Модель уровней** — `MoistureLevel`, `LightLevel`, `LocalNiche` (`NicheLevel.cs`)
- [x] **Сэмплер локальной ниши** — `NicheSampler` (влажность + sunlight, кеш)
- [x] **Таблица видов (MVP)** — `WildSpeciesNiche` для horsetail, ferns, lily, meadow flowers
- [x] **Fitness / stress (MVP)** — `EcologySpreadFitness.ApplyNiche` + `NicheStressThreshold`
- [x] **Understory без symbiosis** — playtest (2026-05): хвощ уходит (niche stress); на месте **eaglefern**, **tallgrass**
- [x] **Aquatic vs берег (spacing)** — `ApplyCrossHabitatSpacing: false`; влажность/субстрат — в v2.2
- [x] Playtest v2.2 niche (2026-05): доминанты по зонам соблюдаются (луг, опушка); лес у воды — не проверен (редкая локация, багрепорт при необходимости)
- [x] **Сукцессия почвы (block-only)** — tier в блоке; `forestfloor` → `soil` только при **death** гумусных ролей; лес/колонизаторы — вариант `forestfloor`; без RAM; без death при ручном сломе
- [x] **Мост на пашню (block-only)** — `WildSoilAgroSampler` + tier soil до вспашки; `UseFarmlandNutrientBridge`
- [x] **Залежь (fallow)** — `FallowRestoration`: пустая пашня **под диким растением** экосистемы восстанавливает N/P/K (`EnableFallowRestoration`); роль почвы из `PlantSoilRole` — **это основная реализация залежи**

См. [PROJECT_VISION.md §14](PROJECT_VISION.md#14-ниша-почва-влажность-освещение-v22).

### v2.9 — пашня, залежь, spread на farmland

- [x] **MaxFertility** — `skipMaxFertility: true` на spread/survival (высокоплодная почва не режет колонизацию)
- [x] **Spread на пустую пашню** — `IsFarmland` как опора (`SideSolid` bypass), fertility 150 в preflight/context
- [x] **`FallowRestoration`** + `EnableFallowRestoration` / `FallowRestorationStrength`
- [x] **`FarmlandTillBridge`** — N/P/K при вспашке от tier soil + ролей растений над клеткой
- [x] **Player-placed auto-register** — `OnDidPlaceBlock` → `TryRegisterPlacedBlock` (`playerPlaced: true`)

### v2.10 — spread hotfixes и displacement

- [x] **`PlantVacancyRules`** — единая вакантность (air / replaceable), fluid-слой `Id==0` не блокирует
- [x] **Mycelium** — `HasActiveMycelium` (BE), не `BlockBehaviorMyceliumHost` на типе блока
- [x] **Displacement/hold** — hold без `SpreadRate`; `DisplacementHoldMargin` 1.18; `EmptySpreadFitnessMultiplier`; пресеты `UseCalendarScaledSpread` + `UseSpeciesSpreadRates`
- [x] **Chunk scan** — неактивные чанки снова в очереди; `ReproduceIntervalHours: 0` → fallback 24h
- [x] Playtest: луг заполняется после фиксов (2026-05-27)

### v2.11 — Ecology inspect, chunk scan, локализация UI

- [x] **Хоткей и сеть** — `EcologyInspectClientSystem` / `EcologyInspectServerSystem`, protobuf-канал `ecosystemflora-ecologyinspect`; зависимость сборки на `protobuf-net.dll` из папки игры
- [x] **Отчёт** — `EcologyInspectService.TryBuildReport`: живое состояние из реестра и окружения; опционально `EcologyAreaScanner` + `EcologySpacingIndex` для топа видов в радиусе (`EcologyInspectScanRadius`, clamp 4–32)
- [x] **Chunk scan resume (v2.11.2)** — очередь `PendingChunkScan (chunk, lx, lz)` и `ChunkFlowerScanner.ScanChunk`: проход не обрывается серединой чанка из‑за лимита регистраций; удалённые от игрока чанки только возвращаются в хвост очереди
- [x] **Клиентская локализация (v2.11.3)** — сервер шлёт `InspectLineLite[]` (ключ + аргументы; в аргументах префиксы `L:` и `I:` для вложенных ключей и масок почвы); ошибки осмотра — поле `ErrorLangKey`, показ в чате через `Lang.Get` на клиенте; полные ключи причин survival — `inspect-survival-fail-*`
- [x] **Отображаемые имена видов (v2.11.4)** — ключи `ecosystemflora:species-{id}` в `lang/en|ru|de.json` для всех ecology-species из кода; уточнение строки топа долей в осмотре

### Баланс и UX (post-ModDB)

| Пункт | Статус |
|-------|--------|
| `HoldStrength` / `DisplacementHoldMargin` / soft empty preference | [x] v2.10 |
| Пресеты `natural` / `lush` / `sparse` | [x] v1.x |
| **Handbook** — 7 guide-страниц + `EcologyHandbookBehavior` + patch | [x] |
| **Залежь** — восстановление N на пашне под дикими растениями | [x] v2.9 |
| **Trampling** — тропы от игроков (`EnableTrampling`, default off) | [x] v2.6 |
| **Ecology inspect** — хоткей **I**, диалог: состояние растения + топ-3 вида рядом (`SpacingIndex`) | [x] v2.11 |
| **Chunk scan resume** — курсор `(lx,lz)`; незавершённый чанк снова в очереди до полного прохода | [x] v2.11.2 |
| **Ecology inspect i18n** — строки собираются на клиенте (`InspectLineLite`); ошибки по сети с `ErrorLangKey` | [x] v2.11.3 |
| **species-* в lang** — отображаемые имена видов для заголовка и блока «экология рядом» (en / ru / de) | [x] v2.11.4 |
| **Dominant species UX** — карта доминанты по чанку/HUD (без осмотра) | [ ] backlog |
| **Выпас животных / `tallgrass-eaten`** — husbandry, не spread | [ ] backlog (вне scope) |

### v2.0 (устарело → заменено v2.1)

~~DisturbedTracker, colonizer window, покос как отдельное состояние~~ — удалено в пользу §11.

### v2.6 — протаптывание дорожек (trampling)

Растения вблизи игроков накапливают `TramplingExposure`; при достижении порога — stress-check как обычная стресс-смерть. Земля деградирует (`SoilSuccessionEvent.Trampled`): −0.25 fertility tier, −8 moisture. Плотно хоженые тропы становятся бесплодными/сухими.

- [x] `TramplingExposure` на `ReproducerEntry`
- [x] Конфиг: `EnableTrampling`, `TramplingRadius`, `TramplingStressThreshold`, `TramplingSoilDegradation`
- [x] Ветка trampling в `ProcessStress` — `PlayerProximity.IsNearAnyPlayer` с малым радиусом
- [x] `SoilSuccessionEvent.Trampled` + `TrampledImpact` в `WildSpeciesSoilSuccession`
- [x] `RemoveEcologyPlant` принимает `soilEvent` (default `Death`; trampled = `Trampled`)
- [x] Playtest: наблюдаемое протаптывание тропинок при повторном хождении (2026-05-26)

### v3.0 — ягодные кусты 1.22 (trait inheritance)

**Проблема:** VS 1.22 переработал ягодные кусты: новые блоки, block entity с нутриентами/возрастом/стадиями, система **наследственных черт** (traits). Traits хранятся в `TreeAttributes` блок-сущности и передаются через черенки. Текущий spread ставит голый блок через `new ItemStack(spreadBlock)` → `SetBlock` — traits **не клонируются**, нутриенты не инициализируются.

**Цель:** при spread ягодного куста — клонировать traits родителя и молодое состояние как у созревшего черенка (`OnGrownFromCutting`), без случайных `genTraits`.

- [x] Формат traits в BE — `FruitingBushState.Traits`, сериализация `traits` на `BEBehaviorFruitingBush` (Vintagestory.GameContent; репозиторий vssurvivalmod)
- [x] После `SetBlock` в `ReproducePlacement.PlaceSpreadBlock` — сервер вызывает `BerrySpreadTraitCloner.TryCloneFromParent` для `fruitingbush-wild-*`
- [x] Применить traits через reflection: `OnGrownFromCutting(traitsCsv)` на дочернем behavior (совпадает с путём после mature cutting)
- [x] Конфиг — `CloneBerryTraits` (default true; false → ванильные случайные wild-traits после spread)
- [x] `BerryTraitMutationChance` (v3.1.1) — опциональная мутация trait при spread
- [ ] Playtest в игре: рост до mature/fruit с совпадающими traits родителя (пользователь — через I/логи)

**Зависимость:** VS 1.22+ API; для 1.21 — текущее поведение без traits.

### v3.1 — attribute-based participant contract (сторонние моды)

**Проблема:** сейчас все участники экосистемы жёстко захардкожены: `PlantCodeHelper` парсит коды `game:flower-*`, `game:tallgrass-*` и т.д. Сторонние моды (Wildgrass Fork, контент-паки) не могут участвовать в экосистеме без правок кода.

**Цель:** блок объявляет участие в экосистеме через JSON-атрибуты; экомод парсит их без хардкода видов.

**Контракт на блоке (JSON attributes):** полное описание — **[`docs/THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)**.

```json
{
  "ecologyParticipant": true,
  "ecologySpecies": "bluegrass",
  "ecologyHabitat": "Terrestrial",
  "ecologySpreadBlock": "mygrassmod:wildgrass-bluegrass-free",
  "ecologySpreadRate": 0.6,
  "minTemp": 0,
  "maxTemp": 30,
  "minRain": 0.3,
  "maxRain": 0.8
}
```

- [x] **Парсер атрибутов** — `EcosystemParticipant.TryFromBlock` + `PlantRequirements.FromBlock` (ветка `thirdPartyParticipant` по `ecologyHabitat`)
- [x] **`PlantCodeHelper`** — `IsThirdPartyEcologyBlock`, `ResolveEcologySpecies`, `GetEcologyHabitat(Block)`, `ResolveEcologyAsset`; `IsEcologyPlant` / spread / mature / reed-путь без хардкода `game:` для объявленных блоков
- [x] **`EcologyAttributes.ReproduceEnabled` / Chunk scan** — через `TryFromBlock`, без изменений API
- [x] **Spacing / displacement** — как и раньше по строке `ecologySpecies`
- [x] **Конфиг** — `EnableThirdPartyParticipants` (default true)
- [x] **Документация** — [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)

**Архитектура контент-модов:**

```
[ecosystemflora]  — экосистемное ядро, парсит ecologyParticipant
      ↑
[ecosystemgrass]  — контент-мод: блоки wildgrass в домене game:,
                    JSON с ecologyParticipant атрибутами,
                    текстуры/модели; без кода
```

Контент-мод — чистый ресурсный мод (JSON + текстуры), не нуждается в C#. Экомод — платформа.

### Обратная связь игроку (handbook)

- [x] **Handbook** — 9 статических guide-страниц (overview, flowers, ferns, trees, berries, aquatic, **canopy**, **inspect**, tuning); en + ru
- [x] **EcologyHandbookBehavior** — динамическая экология на страницах блоков (spread rate, climate, niche, season, symbiosis)
- [x] **JSON patch** — handbook-behaviors.json добавляет поведение ко всем участникам экосистемы
- [x] **Ecology inspect** — клавиша **I** (настраивается): диалог с живым состоянием + скан радиуса из конфига; строки собираются на **клиенте** по `InspectLineLite` (v2.11.3)
- [ ] **Dominant species UX** — карта/оверлей доминанты без ручного осмотра (позже)

### Playtest и техдолг

- [x] Базовый playtest (сессия 2026-05)
- [x] v2.1 playtest — плотное зарастание лугов, displacement (2026-05-22)
- [x] v2.2 niche playtest (2026-05): хвощ → eaglefern/tallgrass; доминанты по зонам OK
- [x] Playtest aquatic (2026-05-24): `gravel-*`, spread в воду, берег, лютик; без захвата луга
- [ ] Aquatic edge cases (опционально): камыш в жаре, лютик max depth, торф/ил
- [x] Папоротники `fern-*`, `tallfern`
- [x] Деревья: spread саженцев от `log-grown`
- [x] Ягодные кусты + почвенные предпочтения
- [x] Водяной лютик: колонка только под водой (не выше поверхности)
- [x] **Chunk-scan без BE в патчах** — `ChunkFlowerScanner`; legacy BE через `LegacyBlockEntityMigration` (v3.1.8); мод можно безопасно снять после save
- [x] Push на `origin` / публикация Mod DB (2026-05-26)

### Оптимизация (perf roadmap)

План зафиксирован 2026-05-22. Теория и ограничения VS API: [PROJECT_VISION.md §12](PROJECT_VISION.md#12-производительность-roadmap).

**Контекст:** при ~18k reproducers узкое место — стоимость одного spread (`CollectSpreadCandidates`) и холостой обход global `List` при `OnlyActivateNearPlayers`. Потоки для spread/stress **не планируются** (BlockAccessor только main thread).

#### Фаза 1 — быстрые wins (высокий impact, низкий риск)

- [x] **Spatial tick** — `ProcessDue` / `ProcessStress` по `byChunk` в hot chunks **только когда** `OnlyActivateNearPlayers` или `LimitSpreadNearPlayers`; при defaults (оба false) stress — global round-robin по реестру, spread — chunk-fair RR
- [x] **Climate cache (static)** — `WorldGenValues` rain/forest per колонка XZ (`EnvironmentalColumnCache`); invalidation при SetBlock рядом
- [x] **Split `EnvironmentalContext.Sample`** — `SampleForSpread` без seasonal temp / greenhouse; `SampleForSurvival` для stress
- [x] **Stress skip** — `NextStressCheckAt` для всех записей; healthy plants не проверяются каждый tick
- [x] **Greenhouse** — не вызывается на spread path (`SampleForSpread`)

#### Фаза 2 — средний refactor

- [x] **O(1) remove в registry** — swap-remove + `EntriesIndex` / `ChunkListIndex`
- [x] **Cheap-first candidates** — `SpreadPreflight` до `SampleForSpread` / niche
- [x] **Spacing hash** — `EcologySpacingIndex` per-chunk вместо brute-force scan
- [x] **Отдельный roundRobinIndex** для `ProcessDue` и `ProcessStress` (уже было в registry)

#### Фаза 3 — perf audit (2026-05-25)

- [x] **`CellBlockSnapshot`** — struct, 4 GetBlock вместо ~15 per candidate; threaded через preflight → context → scoring
- [x] **`CanDisplace` dedup** — `EnvironmentalContext` sample 1× вместо 4×
- [x] **Scratch `BlockPos`** — `DownCopy()`/`UpCopy()` → `.Set()` по всем hot paths; 0 dimension warnings
- [x] **`ChunkFlowerScanner`** — scratch pos, `.Copy()` only on hit (~15k allocs/tick → 0)
- [x] **`GreenhouseHelper`** — reflection cached at startup; reuse arg array
- [x] **Scratch collections** — `ReproducePlacement`, `ReproducerRegistry` remove queues
- [x] **`CollectChunksNearPlayers`** — `HashSet<long>` player chunks, O(1) membership
- [x] **`FloraSymbiosis`** — FIFO host cache (4096), invalidation on block break
- [x] **`VerboseLogging`** — master logging switch; suppresses all Notification/Warning except startup/errors
- [ ] ~~Многопоточный spread/displace~~ — **не делать** (chunk unload, гонки)

#### Фаза 4 — tick starvation fix (2026-05-26)

Баг-репорт: мод вызывает rubber-banding/jitter у **всех** мобов (включая ванильных). Причина — server tick starvation: тик-хендлеры мода съедали слишком много бюджета главного потока.

- [x] **`ChunkFlowerScanner` heightmap** — сканирование от `IMapChunk.RainHeightMap + 2` вместо `MapSizeY` (255); ~43k лишних `GetBlock` на воздух **на чанк** устранены
- [x] **Снижены per-tick лимиты** — `MaxReproduceAttemptsPerTick` 48→32, `MaxStressChecksPerTick` 24→16, `MaxRegistrationsPerTick` 256→192; пресеты обновлены (natural: 32, lush: 48, sparse: 16); `TickBudgetMs` — реальная защита
- [x] **`OnlyActivateNearPlayers`** — default **false** (all loaded chunks); **true** = playtest/perf shortcut near player radius
- [x] **Time-budget (`Stopwatch`)** — `TickBudgetMs` (default 5ms); оба тик-хендлера прерывают обработку при превышении бюджета
- [x] **Кэш `GetClimateAt(NowValues)`** — температура кэшируется per-column per-tick через generation counter в `EnvironmentalColumnCache`; стресс-проверки пачкой не дублируют дорогие вызовы
- [x] **`OnChunkScanTick` интервал** — 500ms → 2000ms; сканирование чанков не срочная операция

#### Фаза 5 — split tick budgets (2026-05-26)

Проблема: с общим `TickBudgetMs = 5ms` stress отъедал бюджет у spread → даже при `MaxReproduceAttemptsPerTick = 512` реально проходило 1–3 placement за тик.

- [x] **Отдельный `OnStressTick`** — stress выведен в свой тик-хендлер (`StressTickIntervalMs`, default 6000ms)
- [x] **`OnReproduceTick` — только spread** — получает весь `TickBudgetMs` без конкуренции со stress
- [x] **`StressBudgetMs`** — свой бюджет для stress (default = `TickBudgetMs`)
- [x] **`MaxReproduceAttemptsPerTick`** убран из пресетов — performance-knob, не баланс; default повышен до 64
- [x] Budget check в stress callback — stress прерывается при исчерпании собственного бюджета

#### Конфиг как throttle (уже есть)

| Параметр | Эффект |
|----------|--------|
| `OnlyActivateNearPlayers` | Playtest/perf — default **false** (v3.6+); true = радиус игрока |
| `TickBudgetMs` | Потолок ms/тик для **spread** (default **30**); 0 = без лимита |
| `SpreadBudgetMs` | Отдельный потолок spread (default **30**; 0 = `TickBudgetMs`) |
| `RegistrationBudgetMs` | Бюджет chunk-scan тика (default **25**; 0 = `TickBudgetMs`) |
| `StressBudgetMs` | Потолок ms для **stress** (default = TickBudgetMs); 0 = use TickBudgetMs |
| `ReproduceTickIntervalMs` | Интервал spread-тика (default **2000** ms) |
| `ChunkScanTickIntervalMs` | Интервал registration scan (default **2300** ms; не кратен reproduce — меньше aligned spikes) |
| `StressTickIntervalMs` | Интервал stress-тика (default **5500** ms); stress не конкурирует со spread |
| `MaxReproduceAttemptsPerTick` / `MaxStressChecksPerTick` | CPU ceiling |
| `FloraContextCacheHours` | Меньше tree-neighbor scan |
| `ReproduceRadius` | O(r²) в spread; 4→3 ≈ −40% колонок |
| `PlantSpacingEnabled: false` | Большой win, ломает баланс |

**Первый PR:** spatial tick + static climate cache — ✅ реализовано (2026-05-22).

#### Фаза 6 — simulation engine (2026-06-14)

План: [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md). Цель — полный scope loaded chunks без geographic cutoffs; умный планировщик вместо throttle.

- [x] **6.1 Chunk-fair spread** — `SpreadChunkScheduler`, RR по **всем** registry chunks; `EnableChunkFairSpread` (default true), `MaxSpreadAttemptsPerChunkPerTick`, `MaxSpreadChunksVisitedPerTick`
- [x] **6.3 Event wake** — `WakeAround` на break/place/displacement/succession/spread; calendar fallback; `EnableEventDrivenSpread` (default true)
- [x] **Due-heap** — O(k) due collection (legacy path when chunk-fair off)
- [x] **Reproduce tick profiling** — `EnableReproduceTickProfiling`, phase timings in server log
- [x] **6.4 EcologyColumnState** — `SpreadColumnSnapshot` per cell; hub invalidation in `InvalidateEnvironmentAround`; `EnableEcologyColumnCache` (default true)
- [x] **6.5 Two-phase placement** — `PendingSpreadQueue` evaluate + chunk-fair commit with revalidation (terrestrial/aquatic mat spread; **не** mycelium/vine — прямой SetBlock в reproduce callback)
- [x] **6.6 Season coarse wake** — month-boundary wake for seasonal species; handbook + tuning docs
- [x] **6.7 Registration + spread collect** — `RegistrationScanQueue` priority/burst; `PendingRegistrationQueue` paced apply; background column scan; `EnableEmptyFirstSpreadCollect` + `EcologyColumnOccupancy` hint (displacement when no vacancy); foliage sync decoupled when background scan on

| Параметр | Эффект |
|----------|--------|
| `EnableChunkFairSpread` | RR spread по loaded registry chunks (default **true**) |
| `EnableEventDrivenSpread` | Wake соседей при изменении мира (default **true**) |
| `MaxSpreadAttemptsPerChunkPerTick` | Cap per chunk (default 2) — fairness, не cut scope |
| `EcologyWakeRadiusBlocks` | 0 = auto из radius + spacing + flora context |
| `EnableEcologyColumnCache` | Spread cell snapshot cache (default **true**) |
| `EnableTwoPhaseSpreadPlacement` | Evaluate queue + chunk-fair SetBlock commit для mat/terrestrial spread (default **true**); mycelium/vine — вне очереди |
| `LimitSpreadNearPlayers` | Spread + stress + tree aging near players; registration scans unchanged (default **false**) |
| `MaxSpreadCommitsPerTick` | Commit cap; 0 = `MaxReproduceAttemptsPerTick` |
| `EnableSeasonCoarseWake` | Wake seasonal species each in-game month (default **true**) |
| `EnablePlayerPriorityRegistration` | Player-vicinity chunk scans before background queue (default **true**) |
| `EnableBurstRegistrationNearPlayers` | Finish one nearby chunk per load callback (default **true**) |
| `PlayerRegistrationPriorityRadiusBlocks` | Priority/burst radius (default **16**) |
| `EnableBackgroundRegistrationScan` | Worker-thread ecology column classify (default **true**) |
| `MaxRegistrationSnapshotCellsPerTick` | Block ids copied on main per tick (default **8192**) |
| `MaxRegistryAppliesPerTick` | Paced registry applies per chunk-scan tick (default **512**) |
| `MaxPriorityRegistryAppliesPerTick` | Extra applies for player-vicinity chunks (default **2048**) |
| `MaxPriorityChunkScansPerTick` | Priority queue scan passes per chunk-scan tick (default **48**) |
| `MaxPriorityRegistrationsPerTick` | Legacy sync cap for priority queue (default **8192**) |
| `PriorityRegistrationBudgetMs` | Per-pass ms for priority scans (default **80**) |
| `RegistrationBudgetMs` | Chunk-scan tick budget ms (default **25**; 0 = `TickBudgetMs`) |
| `BurstRegistrationBudgetMs` | Burst scan budget per load (default **80**) |
| `EnableEmptyFirstSpreadCollect` | Empty/vacancy cells first; displacement only if none (default **true**) |
| `EnableSpreadColumnOccupancyHint` | Skip known-occupied columns on empty-first pass (default **true**) |

#### Фаза 6.7c — registration perf & fixes (2026-06-18)

- [x] **Priority radius** — `PlayerRegistrationPriorityRadiusBlocks` 384→**16** (меньше «priority storm» при load)
- [x] **Burst budget** — `BurstRegistrationBudgetMs` 250→**80** ms
- [x] **Desynced ticks** — reproduce **2000** ms, chunk scan **2300** ms, stress **5500** ms
- [x] **Worker null-safety** — snapshot air fallback; classify skip on null/air; try/catch clears in-flight
- [x] **Fallen sticks** — `CanopyFallenSticks` → `SurfacePlacement.TryFindSurfaceCellBelow` (не висит на tallgrass)
- [x] **Break wake** — `OnDidBreakBlock` без `WakeEcologyAround`, если блок не ecology plant, не в registry, не forest-context и не event-target (напр. `loosestick-free`; `leaves-*` / `log-grown` — по-прежнему могут будить)

### Завершено (контент)

- [x] Деревья, ягоды, папоротники, почва (`SoilKind`), aquatic (код)

---

## Git (история)

| Коммит | Содержание |
|--------|------------|
| `bacb506` | Ecosystem v1: participants, rain/forest, spread rates, candidate pool |
| `4b78497` | Concurrent propagation queue |
| `99d88f6` | Plant spacing, calendar spread, config |
| `92ec0cf` | Aquatic flora, muddygravel substrate, `EcosystemParticipant` |
| `1eb2548` | Water plants: reed column rules, crowfoot, land/water variants |
| `05e3dbb` | Trees, berries, soil types |
| `3574c2a` | Ferns; water crowfoot underwater placement fix |
| `ee35e62` | Ecosystem v2.1: cell competition, displacement, stress, symbiosis |
| `b06e53b` | `LocalForestCover` вместо worldgen `ForestDensity` |
| `ba6c64c` | Aquatic spread (`SpreadVacancy`), `gravel-*` reed bed, land reed у воды |
| `2fd0c09` | Seasonal ecology v2.3: spread and stress follow game calendar |
| `ba52c8e` | Strip legacy JakeCool code, assets, and artifacts |
| `48b07cc` | Rename EcosystemPlant to EcoSystemLife |
| `e06c31b` | Perf audit: CellBlockSnapshot, scratch BlockPos, reflection cache, HashSet chunks |
| `7fceb4b` | VerboseLogging toggle — suppress all non-startup log I/O |
| *(prev)* | Tech debt: split BlockFluidHelper → ReedColumnHelper + WaterColumnHelper; xUnit tests (46); docs/modinfo 2.5.0 |
| *(done)* | Perf phase 4–5: tick budgets, faster defaults, caches and scan pacing (rolled into v2.7–v2.11) |
| *(done)* | Handbook: static pages + `EcologyHandbookBehavior` + JSON patches |
| *(done)* | v2.9: fallow, farmland spread, MaxFertility fix, player-placed register |
| *(done)* | v2.10: mycelium BE, `PlantVacancyRules`, displacement/hold tuning |
| *(done)* | v2.11: Ecology inspect (I), protobuf channel, `SpacingIndex` area scan |
| *(done)* | v2.11.2–2.11.3: chunk scan resume; inspect i18n (`InspectLineLite`, `ErrorLangKey`) |
| *(done)* | v2.11.4: `ecosystemflora:species-*` в `lang/` (en/ru/de); подпись долей в топе осмотра |
| *(done)* | 12-month `WildSpeciesSeason` curves; tule in `WildAquaticEcology` |

---

## Быстрые ссылки

- Конфиг: `%AppData%\VintagestoryData\ModConfig\ecosystemflora.json`
- Лог: `%AppData%\VintagestoryData\Logs\server-main.log` (`VerboseLogging: true` + `ReproduceDebug: true`)
- Сборка: `dotnet build` → `bin\Debug\Mods\ecosystemflora\` (копируется в `Mods\ecosystemflora` при закрытой игре)
- Сторонние моды / JSON-участники: **[THIRD_PARTY_ECOLOGY.md](THIRD_PARTY_ECOLOGY.md)**
