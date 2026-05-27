# Ecosystem - Flora — видение проекта

Документ для разработчиков и AI-агентов: **теория**, **целевая архитектура**, **текущая стадия репозитория**.

Последнее обновление: 2026-05-27 (стадия: **Ecosystem v3.1**, версия **`3.1.0`**; актуальный чеклист — [`PROGRESS.md`](PROGRESS.md)).

---

## 1. Зачем этот проект

Цель — **узкая экосистемная прослойка** для Vintage Story (не клон [Wild Farming Revival](https://mods.vintagestory.at/wildfarmingrevival)):

- объекты **включены в мир** (климат, почва, соседи), а не живут как изолированные мини-игры;
- **живой** объект обязан **размножаться**; без размножения это декор, не член экосистемы;
- взаимодействие через **интерфейсы (capabilities / interests)** — целевая модель;
- размножение **только на пригодных клетках** → читаемая «история» на ландшафте без отдельного worldgen.

Оригинальный Wild Farming (JakeCool19 v1.2.0) — только **вдохновение**; код полностью переписан, оригинал не в сборке.

---

## 2. Принципы дизайна

### 2.1. Объект не самостоятельный

Сущность не симулирует весь мир внутри себя. Она объявляет, что нужно от среды, и реагирует на снимок `EnvironmentalContext` в точке.

### 2.2. Живость = размножение

| Статус | Критерий |
|--------|----------|
| Живой участник экосистемы | Зарегистрирован в `EcosystemSystem`, периодически пытается дать потомство |
| Не живой | Статичный декор без регистрации |

### 2.3. Среда из игры

Климат, fertility, сезон — из API Vintage Story. Мод не подменяет worldgen; он **реагирует** на условия в каждой клетке-кандидате.

### 2.3.1. Ванильные блоки в мире (текущая реализация)

| Принцип | Реализация |
|---------|------------|
| **Объект в мире** | `game:flower-*`, `tallplant-coopersreed/papyrus`, `waterlily`, `aquatic-watercrowfoot` |
| **Мод при снятии** | Патч `entityClass: EcoSystemLife`; без мода — обычные блоки, сохранения целы |
| **Дикое размножение** | Тот же ванильный блок (или корректный land/water для тростника) на соседней клетке |
| **Семена / wildplant** | **Не** часть дикой экосистемы; не в сборке |
| **Культивация игроком** | Ванильные механики игры |

**Регистрация:** очередь сканирования чанков + `EcoSystemLife` BE. **Размножение:** round-robin по реестру, лимит попыток за тик.

### 2.4. Минимум механик (не тащить из Revival)

- living trees, лианы, Gas API, Harmony worldgen, термиты;
- страницы тогглов и mod-артефакты в сохранении.

**Берём:** suitability по климату/почве/воде, rain/forest из worldgen-карт, размножение с `MinFitness`, скорость spread per-species, spacing, календарные интервалы.

---

## 3. Целевая архитектура

### 3.1. Схема (цель)

```
[Климат, почва, вода]  →  EnvironmentalContext
                                ↓
Участник              →  IEcosystemParticipant (PlantRequirements, habitat)
                                ↓
EcosystemSystem       →  Score >= MinFitness → SetBlock (ванильный код)
```

### 3.2. Минимальные контракты (C#)

| Интерфейс | Назначение | Статус |
|-----------|------------|--------|
| `IEnvironmentalContext` | Снимок среды | ✅ |
| `IReproducible` / `ReproducerEntry` | Маркер живости + таймер | ✅ |
| `SuitabilityEvaluator` | Оценка клетки | ✅ по habitat |
| `IEcosystemParticipant` | Interests + spread codes | ✅ `EcosystemParticipant` |
| `IGrowthStage` | Семя → ювениль → взрослый | ⏸ не для дикой природы |

### 3.3. Естественная «история» на карте

**Цветы:** ванильный цветок → регистрация → spread на соседнюю клетку с подходящим климатом/почвой/rain/forest.

**Тростник:** привязка к `muddygravel`; в мелководье — ровно один водный блок между илом и поверхностью, рогоз **внутри** этого блока (`water-normal`).

**Водяной лютик:** колонка section с tip/top в подводной толще (2–8 блоков воды над дном).

### 3.4. Структура кода (фактическая)

```
src/
  WF.cs
  Ecosystem/
    EcosystemSystem.cs          # main system, tick scheduling
    ReproducerRegistry.cs       # spatial registry, round-robin
    ChunkFlowerScanner.cs       # очередь PendingChunkScan, ScanChunk — регистрация при загрузке чанка
    EcologyInspectService.cs    # снимок для UI «осмотр экологии»
    EcologyInspectServerSystem.cs
    EcologySpacingIndex.cs      # учёт позиций для скана «видов рядом»
    EcologyAreaScanner.cs
    EcologyInspectLineFormat.cs # клиентское разрешение InspectLineLite (не на сервере)
    ReproducePlacement.cs       # spread orchestration
    SurfacePlacement.cs         # terrestrial placement
    ReedPlacement.cs            # reed shore/shallow placement
    WaterPlacement.cs           # water lily placement
    CrowfootPlacement.cs        # crowfoot spread validation
    CrowfootColumnPlacer.cs     # crowfoot column builder
    BlockFluidHelper.cs         # core fluid/substrate primitives
    ReedColumnHelper.cs         # reed column: site, depth, stacking
    WaterColumnHelper.cs        # crowfoot column: snap, measure, depth
    EnvironmentalContext.cs     # env sampling (split: spread/survival)
    EnvironmentalColumnCache.cs # worldgen rain/forest cache per XZ
    CellBlockSnapshot.cs        # pre-fetched block layers (struct)
    SuitabilityEvaluator.cs     # fitness, survival, reproduce checks
    CellCompetition.cs          # spread vs hold scoring, displacement
    PlantRequirements.cs        # per-species requirements
    SoilClassification.cs       # soil kind + fertility mapping
    NicheSampler.cs             # local moisture + light
    WildSpeciesNiche.cs         # niche preferences per species
    WildSpeciesSeason.cs        # seasonal profiles per species
    SeasonEcology.cs            # season multipliers for spread/stress
    WildFlowerClimate.cs        # flower climate profiles
    WildAquaticEcology.cs       # aquatic ecology profiles
    WildFlowerSpacing.cs        # per-species spacing
    PlantSpacing.cs             # spacing enforcement
    SpeciesSpread.cs            # interval/chance calculation
    FloraContextSampler.cs      # open/edge/forest context
    FloraSymbiosis.cs           # host-tree dependency + cascade
    EcologySpreadFitness.cs     # context + niche fitness multipliers
    WildSpeciesModifiers.cs     # HoldStrength, ContextAffinity
    EcosystemParticipant.cs     # IEcosystemParticipant impl
    EcologyHabitat.cs           # habitat enum
    PlantCodeHelper.cs          # species parsing, block code helpers
    EcosystemConfig.cs          # config loading + presets
    PlayerProximity.cs          # active chunk detection
    GreenhouseHelper.cs         # greenhouse room detection (cached)
    SpreadPreflight.cs          # cheap-first candidate filter
    SpreadVacancy.cs            # aquatic vacancy check
    WildSoilGroundRules.cs      # farmland + mycelium spread gates
    LandClaimGuard.cs           # land claim respect
  Client/
    EcologyInspectClientSystem.cs
    EcologyInspectDialog.cs
  Network/
    EcologyInspectPackets.cs   # InspectLineLite, protobuf channel
  Handbook/
    EcologyHandbookBehavior.cs  # dynamic ecology info on block pages
  BlockEntity/
    EcoSystemLife.cs            # thin BE: register on load
tests/
  WildFarming.Tests.csproj     # xUnit, 72 tests
  SeasonProfileTests.cs
  SoilClassificationTests.cs
  SuitabilityEvaluatorTests.cs
assets/ecosystemflora/
  patches/enabledpatches.json
docs/
```

Актуальный чеклист: **[`PROGRESS.md`](PROGRESS.md)**.

---

## 4. MVP-vanilla-flowers (завершён)

| Критерий | Статус |
|----------|--------|
| Все 20 `game:flower-*` в реестре | ✅ |
| Размножение на подходящих клетках | ✅ |
| Склоны, вода, оптимизация | ✅ |
| Без mod-блоков в мире | ✅ |

---

## 5. Ecosystem v1 / v1.1

| Критерий | Статус |
|----------|--------|
| `IEcosystemParticipant` | ✅ |
| `minRain` / `minForest` (worldgen maps) | ✅ |
| Выбор из пула свободных клеток | ✅ |
| `SpreadRate` per-species | ✅ |
| Spacing + calendar spread | ✅ |
| Aquatic (reeds, lily, crowfoot) | ✅ код |
| Длинный playtest aquatic | ✅ (2026-05-24) |

---

## 6. Сравнение с оригиналом и Revival

### 6.1. Оригинал (репозиторий, v1.2.0)

- `wildplant` → `game:*`, mod-семена, living trees — **архив**, не в сборке.

### 6.2. Revival

- Референс баланса и UX; **не** merge и не co-load.
- Наш modid: `ecosystemflora`, другая архитектура.

---

## 7. Текущая стадия репозитория

**Стадия: `Ecosystem v3.1`, версия `3.1.0`.** Помимо ванильной флоры: **v3.0** — клонирование **traits** диких ягодников при spread (`CloneBerryTraits`); **v3.1** — участники через **JSON атрибуты** на blocktype (`ecologyParticipant`, см. **[`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)**), конфиг **`EnableThirdPartyParticipants`**. ModDB: [ecosystemflora](https://mods.vintagestory.at/ecosystemflora).

| Компонент | Статус |
|-----------|--------|
| Ванильная экосистема (цветы, tallgrass, ferns, berries, trees, aquatic…) | ✅ см. [`PROGRESS.md`](PROGRESS.md) |
| Осмотр экологии (**I**), chunk-scan, i18n имен видов | ✅ v2.11.x |
| Perf (отдельный stress/spread budget, spatial tick, …) | ✅ |
| Юнит-тесты (xUnit) | ✅ **72** |
| Сторонние blocktypes как участники | ✅ v3.1 + [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md) |
| Legacy JakeCool в сборке | ⏸ удалён |

- **`modinfo.json`** — `ecosystemflora`, game `1.22.0`, версия см. поле `version` (сейчас **3.1.0**).
- **Конфиг:** `%VintagestoryData%/ModConfig/ecosystemflora.json` (шаблон — `assets/ecosystemflora/ecosystemflora.example.json`).

---

## 8. Правила для агентов

1. **Не расширять** trees / vines / mushrooms без явного запроса.
2. **Дикая экосистема** — в мире **ванильные блоки** родителей от `game:` и **другие blocktypes**, если мод-автор объявил [`ecologyParticipant`](THIRD_PARTY_ECOLOGY.md) (без подмены vanilla `wildplant`).
3. **Размножение** — только `Score >= MinFitness` и habitat-specific placement.
4. **Тростник** — не ломать правило «ил + один водный блок»; не ставить `gravel+2` над столбом воды.
5. **Производительность** — очереди и лимиты; не трогать блоки из фоновых потоков.
6. API: **VS 1.22+**, **.NET 10**.
7. Коммиты — только по запросу пользователя.

---

## 9. Открытые решения и roadmap

Актуальный чеклист: **[`PROGRESS.md` → Roadmap / TODO](PROGRESS.md#roadmap--todo)**.

Кратко:

- [x] **Mod DB** — опубликовано 2026-05-26: https://mods.vintagestory.at/ecosystemflora
- [x] **v1.x** — tallgrass, drygrass-патч, пресеты баланса — ✅ в main.
- [x] **v2.1** — единая конкуренция за клетку (§11); playtest лугов ✅ (2026-05-22).
- [x] **v2.2** — ниша (moisture/light), сукцессия почвы — ✅.
- [x] **v2.3** — сезонность spread/stress — ✅.
- [x] **12-месячные кривые** — `WildSpeciesSeason` + `SeasonEcology` (интерполяция по году) — ✅.
- [x] **Perf audit** (фазы 1–5) — spatial tick, split stress/spread budget — ✅.
- [x] **Unit tests** — 72 теста (season, classification, suitability, vacancy, ecology inspect survival, third-party attrs) — ✅.
- [x] **Refactor** — `BlockFluidHelper` → `ReedColumnHelper` + `WaterColumnHelper` — ✅.
- [x] Land claims — `RespectLandClaims` / `LandClaimGuard` — ✅.
- [x] `modid` → `ecosystemflora`
- [x] **Handbook** — статические guide-страницы + `EcologyHandbookBehavior` — ✅.
- [x] **Залежь** — `FallowRestoration` на пустой пашне под диким растением — ✅.
- [x] **v2.10** — spread hotfixes (`PlantVacancyRules`, active mycelium BE only) — ✅.
- [x] **v2.11** — Ecology inspect (хоткей I, protobuf), chunk-scan до конца чанка, строки отчёта локализуются на клиенте — ✅.
- [x] **v2.11.4** — ключи **`ecosystemflora:species-{id}`** для заголовка осмотра и топа «экология рядом» (en / ru / de); пояснение доли в строке топа — ✅.
- [x] **v3.0** — spread диких ягодников **копирует traits** родителя (`CloneBerryTraits`, рефлексия `OnGrownFromCutting`) — ✅.
- [x] **v3.1** — JSON-контракт для сторонних модов (`EnableThirdPartyParticipants`, `PlantCodeHelper.ResolveEcologySpecies`, …); гайд **[`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)** — ✅.
- [x] Chunk-scan без BE в патчах — `ChunkFlowerScanner`; legacy `EcoSystemLife` самоудаляется — ✅.
- [ ] **Dominant species UX** — подсказка «кто доминирует» в зоне — backlog.
- [ ] **Выпас / `tallgrass-eaten`** — husbandry — backlog (не spread).
- [ ] Зимняя листва на стволах — **отложено** (визуал, не экосистема)

---

## 10. Ecosystem v2 — теория границ флоры (дизайн)

**Идея:** опушка, луг и лес — не три отдельных биома в коде, а **зоны конкуренции**, которые **самоорганизуются** из соседей и worldgen. Граница леса = там, где меняется локальный контекст, а не отдельный worldgen-патч.

### 10.1. Связь с v1.1

Уже есть глобальные ареалы: `WorldgenRainfall`, климат, почва (`SoilKind`). **Лесность для видов** — `LocalForestCover` (соседние `log-grown`/листва), не worldgen `ForestDensity`.  
v2 добавляет **локальный множитель** при выборе клетки для spread и при оценке fitness:

```
fitness_effective = ReproduceFitness(req, ctx) × ContextMultiplier(local)
```

`ContextMultiplier` — из кольца соседей (radius 1–2), не из отдельного типа блока «опушка».

### 10.2. Локальный контекст (`FloraContext`)

| Контекст | Условие (эвристика) | Эффект |
|----------|---------------------|--------|
| `ForestInterior` | ≥4 «лесных» соседа в radius 2 (`log-grown`, листва, ствол) | Подавление открытых видов; бонус тенелюбивым |
| `ForestEdge` | 1–3 лесных соседа, остальное открыто | Бонус edge-видам (папоротник, кусты, высокотрава) |
| `Open` | 0 лесных соседей | Стандарт луга / поля |

Псевдокод:

```
forest_neighbors = CountForestNeighbors(x, z, radius: 2)

if forest_neighbors >= 4  → ForestInterior  // suppression для open-видов
if forest_neighbors >= 1  → ForestEdge      // edge_bonus
else                      → Open            // 1.0
```

«Лесной» сосед — ванильные блоки дерева (не mod-trunk). Список кодов: `log-grown-*`, `leaves-*`, опционально плотный `forestfloor`.

### 10.3. Таблица видов (расширение ecology)

К существующим полям (`SpreadRate`, soil, rain/forest) добавить:

| Поле | Назначение |
|------|------------|
| `ContextAffinity` | `open` / `edge` / `forest` — предпочитаемый контекст |
| `ContextBonus` | Множитель при совпадении affinity с локальным контекстом (напр. папоротник edge ×2.5) |
| `ForestSuppression` | Опционально: множитель &lt;1 в `ForestInterior`, если вид не forest-affinity |

Пример (иллюстративно):

| species | SpreadRate | ContextAffinity | ContextBonus |
|---------|------------|-----------------|--------------|
| horsetail | 2.8 | open | 1.0 |
| cornflower | 1.4 | open | 1.2 |
| eaglefern | 1.4 | edge | **2.5** |
| blueberry | 0.65 | edge | **2.0** |
| lilyofthevalley | 0.95 | forest | **3.0** |

**Опушка получается автоматически** — без отдельного habitat и без отдельного worldgen: только таблица + подсчёт соседей при spread.

### 10.4. Реализация (принципы)

- Ванильные блоки, простой алгоритм, без Harmony.
- `ContextMultiplier` считать в `SuitabilityEvaluator` или при сборе `SpreadCandidate`.
- Кеш на колонку XZ + инвалидация при изменении соседнего дерева (раз в N тиков или по событию `BlockBreak`/`SetBlock` рядом) — не каждый тик по всему миру.
- **`LocalForestCover`** (0–1, до 8 соседей) заменяет worldgen `ForestDensity` в `MinForest`/`MaxForest`; `FloraContext` — тот же сигнал для множителей.

---

## 11. Ecosystem v2.1 — единая конкуренция за клетку (реализовано)

**Идея:** один механизм вместо отдельных «покоса», «disturbed» и «опушки-биома». Растения конкурируют за клетки; покос игрока = просто освобождение клетки в общем пуле.

### 11.1. Формула

На клетке-кандидате для вида-challenger:

```
spreadScore = ReproduceFitness × ContextMultiplier × SpreadRate × SeasonMultiplier
holdScore   = ReproduceFitness × ContextMultiplier × HoldStrength
```

- **Пустая клетка** — победитель с max `spreadScore` (weighted random среди прошедших `MinFitness`).
- **Занятая** ecology-клетка — challenger **вытесняет** incumbent, если  
  `spreadScore ≥ holdScore × DisplacementHoldMargin` (конфиг, default 1.18).
- **Стресс-смерть** — incumbent не проходит `MeetsSurvivalRequirements` (или потерял host-симбиота) N раз → блок снимается → снова конкуренция.

Опушка **не отдельный биом**: edge-виды выигрывают у open-видов у леса через `ContextMultiplier` + displacement.

Покос / слом игроком **не помечает** disturbed — клетка пустая → быстрые виды (высокий `SpreadRate`, низкий `HoldStrength`) занимают первыми; их позже вытесняют виды с высоким hold и лучшим контекстом.

### 11.2. Поля в таблице видов (`WildSpeciesModifiers`)

| Поле | Назначение |
|------|------------|
| `ContextAffinity` / `ContextBonus` | Локальный контекст (§10) |
| `HoldStrength` | Защита занятой клетки (colonizers ~0.65, climax ~1.2) |
| `SpreadRate` | В `Wild*Ecology` — частота и vigor spread |

Убрано: `IsColonizer`, `DisturbedBonus`, `DisturbedTracker`, colonizer window.

### 11.3. Симбиоз (`FloraSymbiosis`)

Симбионты привязаны к **дереву** (`log-grown`) в радиусе 2–4 блоков (папоротники, ландыш, ягоды).

- При смерти/сломе **хоста** → симbionты в радиусе каскадно снимаются.
- При стресс-проверке: симbionт без host → накапливает failed checks → смерть.

### 11.4. Конфиг

| Key | Default |
|-----|---------|
| `UseCellDisplacement` | true |
| `DisplacementHoldMargin` | 1.18 |
| `EmptySpreadFitnessMultiplier` | 2.5 (with `PreferSpreadToEmptyCells`) |
| `EnableStressDeath` | true |
| `StressRecheckHours` | 18 |
| `EnableSymbiosis` | true |
| `UseFloraContext` | true |

### 11.5. Код

| Компонент | Файл |
|-----------|------|
| Spread / displace | `CellCompetition`, `ReproducePlacement` |
| Context | `FloraContextSampler`, `EcologySpreadFitness` |
| Stress | `EcosystemSystem.ProcessStress` |
| Symbiosis | `FloraSymbiosis` |
| Species tuning | `WildSpeciesModifiers` |

### 11.6. Playtest (2026-05-22)

На живом мире (~18k reproducers) после v2.1:

- **До:** поверхность выглядела «лысой» — редкие worldgen-вкрапления, мало динамики.
- **После:** луга и опушки **пышно зарастают**; трава и цветы spread + **замещают** блоки соседей; визуально «живая» экосистема.
- Логи: `Spread`, `Displaced` на подходящих клетках.
- Исправлен краш `ProcessStress` при stress death (удаление из реестра во время round-robin).

Остаётся проверить: покос → быстрые colonizers → вытеснение; symbiosis cascade.

---

## 12. Производительность (roadmap)

Чеклист реализации: **[`PROGRESS.md` → Оптимизация](PROGRESS.md#оптимизация-perf-roadmap)**.

### 12.1. Где CPU сейчас

Один reproduce-тик (каждые ~2 с):

```
ProcessStress     → round-robin по List<ReproducerEntry> (весь реестр)
ProcessDue        → round-robin, до MaxReproduceAttemptsPerTick spread
  └── CollectSpreadCandidates → O(r² × verticalSearch) × (Sample + spacing + FloraContext)
ChunkScan         → очередь, лимитирован
```

При ~18k reproducers и `OnlyActivateNearPlayers` конфиг **фильтрует** далёкие растения, но код всё равно **сканирует** global list — много холостых итераций. Горячий путь — **один spread**, не размер реестра сам по себе.

### 12.2. Потоки — что не делаем

| Подход | Вердикт |
|--------|---------|
| Spread / displace / stress в worker threads | ❌ `BlockAccessor` не thread-safe; chunk unload → гонки |
| `Parallel.For` по кандидатам spread | ❌ тот же accessor |
| Фоновый precompute без snapshot | ❌ invalidation при unload |

**Правило (§8):** блоки и spread — только main/server thread. Выигрыш — **меньше работы на тик**, spatial indexing, кэши.

### 12.3. Асимптотика — приоритеты

| # | Проблема | Сейчас | Цель |
|---|----------|--------|------|
| 1 | Tick scope | O(registry) round-robin | O(active chunks × plants) через `byChunk` + радиус игроков |
| 2 | `List.Remove` | O(n) | Swap-remove + index map |
| 3 | `CollectSpreadCandidates` | O(r²v) × heavy Sample | Cheap-first reject; Sample только на прошедших |
| 4 | `PlantSpacing` | Brute-force O(r²Y) на кандидата | Per-chunk ecology hash |
| 5 | `GetClimateAt` | 2× на каждый Sample | Static worldgen cache per XZ |

### 12.4. Актуальность расчётов

| Данные | Частота изменений | Spread нужен? | Stress нужен? | Кэш |
|--------|-------------------|---------------|---------------|-----|
| Worldgen rain / forest | Никогда | ✅ | ✅ | Permanent per XZ |
| Ground fertility / soil | Редко (SetBlock) | ✅ | ✅ | Per XZ + invalidation |
| Seasonal temperature | Медленно | ❌ | ✅ (harsh) | TTL ~игровые сутки |
| FloraContext (trees) | При сломе дерева | ✅ | — | ✅ 12 h (`FloraContextCacheHours`) |
| Greenhouse (RoomRegistry) | Редко | ❌ | ✅ | По необходимости |

**Split sample:** `SampleForSpread` — rain, forest, soil, fluid; без temp/greenhouse.  
`SampleForSurvival` — полный, только stress path и реже.

### 12.5. Фазы (кратко)

1. **Быстрые wins:** spatial tick, static climate cache, split sample, stress skip, no greenhouse on spread — ✅.
2. **Средний refactor:** O(1) registry remove, cheap-first candidates, spacing hash — ✅.
3. **Perf audit:** `CellBlockSnapshot`, scratch `BlockPos`, reflection cache, scratch collections, `HashSet<long>` player chunks, `FloraSymbiosis` FIFO cache, `VerboseLogging` toggle — ✅.
4. **Tick starvation fix (v2.7):** heightmap chunk scan, per-tick time-budget (`Stopwatch`), lower defaults, NowValues temperature cache, `OnlyActivateNearPlayers` default true — ✅.

Многопоточность — **не планируется** (`BlockAccessor` не thread-safe).

Все четыре фазы завершены (2026-05-26).

### 12.6. Конфиг-throttle

`OnlyActivateNearPlayers` (default true), `TickBudgetMs` (default 5), `MaxReproduceAttemptsPerTick`, `MaxStressChecksPerTick`, `FloraContextCacheHours`, `ReproduceRadius` — см. таблицу в PROGRESS.

---

## 14. Ниша: почва, влажность, освещение (v2.2)

**Проблема:** symbiosis и `LocalForestCover` не покрывают микрониши — хвощ любит **тень и влагу**, полевые цветы — **сухой открытый луг**, ландыш — **влажная тень**. Сейчас частично: `SoilKind`, `MinSunlight` (fern/berry/tallgrass), `FloraContext` (open/edge/forest).

**Идея:** явная **трёхосевая ниша** на клетке spread/stress:

```
nicheScore = f(soilKind, moistureLevel, lightLevel) × speciesPreference
```

### 14.1. Оси

| Ось | Источник (vanilla API) | Уровни (пример) |
|-----|------------------------|-----------------|
| **Почва** | `SoilClassification`, `block.Fertility` | уже есть `SoilKind` — расширить профили |
| **Влажность** | торф, `muddygravel`, соседняя вода/жидкость, rain локально | dry → mesic → wet → shoreline |
| **Свет** | `TreePlacement.HasEnoughSunlight`, соседи `leaves`/`log-grown` | open (≥11) → partial → shade (7–10) → deep shade (&lt;7) |

### 14.2. Виды (пример таблицы)

| Группа | Почва | Влажность | Свет |
|--------|-------|-----------|------|
| Полевые colonizers (daisy, horsetail open) | meadow, medium | mesic–dry | open |
| Лесная understory (lily, fern, horsetail wet) | forest floor, peat | wet–mesic | shade–partial |
| Опушка (catmint, heather) | medium, low | mesic | partial–open |
| Aquatic margin | gravel/mud | wet–submerged | open |

### 14.3. Интеграция с v2.1

- **Spread:** множитель к `ReproduceFitness` (как `FloraContext`), не отдельный biome
- **Stress:** ускоренный failed check вне ниши (дополняет symbiosis)
- **Displacement:** сильный вид может вытеснить, но долго не держится вне ниши
- **Философия (playtest):** мягче — spread вне ниши допустим, приживание хуже; не обязательно hard gate по niche

### 14.4. Сукцессия почвы (v2.2)

**Дикая почва (block-only, без RAM):**

| Параметр | Где | В игре |
|----------|-----|--------|
| **Tier** | `Block.Fertility` + смена блока | `game:soil-*` / `forestfloor` / `peat` |
| **Влажность** | `NicheSampler` на момент события | торф у `WetlandHerb` при высокой влажности |
| **Лесная подстилка** | лесные роли / колонизаторы | только вариант `forestfloor-*` (насыщенность) |
| **Луг на вырубке** | death + гумус (perennial, tallgrass, lupine) | `forestfloor` → `soil-*` при росте tier |

**N/P/K только на пашне:** `FarmlandTillBridge` — при вспашке снимок: tier с **блока soil до замены**, роль из **растений над клеткой** (`WildSoilAgroSampler`). Бонус поверх ванильного `OnCreatedFromSoil`.

**Death:** только естественное снятие mod (`RemoveEcologyPlant` — stress, displacement, symbiosis), не ручной слом.

**Роли** (`PlantSoilRole`) — `WildSpeciesSoilSuccession`: MeadowColonizer, MeadowPerennial, ForestUnderstory, WetlandHerb, GrassMatrix, **NitrogenFixer** (lupine), …

**События:** spread (register) и death (`RemoveEcologyPlant`). **Колонизация пустой пашни** разрешена (farmland как опора; без культуры сверху). **Сукцессия tier почвы** на `farmland-*` не применяется (`IsWildSpreadGround` → false). **Активная грибница** (mycelium BE) — spread запрещён.

**Конфиг:** `UseSoilSuccession`, `SoilSuccessionStrength`, `UseFarmlandNutrientBridge`, `FarmlandNutrientBridgeStrength`, `EnableFallowRestoration`, `FallowRestorationStrength`.

**TODO (опционально):** persist agro/tier в chunk moddata; отдельно — восстановление N/P/K от **ванильных** сорняков без `IEcosystemParticipant` (залежь через участников уже есть — `FallowRestoration`).

### 14.5. Реализация (принципы)

- Кеш per XZ/Y как `FloraContextSampler` / `EnvironmentalColumnCache`; invalidation при SetBlock воды/дерева/почвы
- Не дублировать worldgen rain/forest — влажность **локальная** (блок + соседи)
- Чеклист: [`PROGRESS.md` → v2.2](PROGRESS.md#v22--ниша-почва-влажность-освещение)

---

## 15. Ягодные кусты 1.22 — trait inheritance (v3.0)

VS 1.22 переработал ягодные кусты: новые блоки, block entity с нутриентами, возрастом и **наследственными чертами** (traits). Traits хранятся в `TreeAttributes` BE и передаются через черенки.

### 15.1. Текущее поведение

`ReproducePlacement.PlaceSpreadBlock` ставит блок через `new ItemStack(spreadBlock)` → `SetBlock`. Это не копирует данные BE родителя → дочерний куст **не наследует traits**, нутриенты не инициализируются.

### 15.2. Целевое поведение

При spread ягодного куста:

1. Прочитать `TreeAttributes` BE родителя (traits)
2. `SetBlock` для дочернего блока
3. Скопировать traits в BE дочернего куста
4. Инициализировать нутриенты/возраст как при посадке черенка

Опционально: мутация — шанс потерять/приобрести trait при spread (природная вариативность vs клонирование).

### 15.3. Совместимость

- VS 1.21: ягодные кусты — `fruitingbush-wild-*` без traits; текущее поведение сохраняется
- VS 1.22+: новые блоки + trait system; конфиг `CloneBerryTraits` (default true)

---

## 16. Attribute-based participant contract (v3.1)

**Реализовано** (см. **[`docs/THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md)**): конфиг `EnableThirdPartyParticipants`, `PlantCodeHelper.IsThirdPartyEcologyBlock` / `ResolveEcologySpecies`, `EcosystemParticipant.TryFromBlock`, `PlantRequirements.FromBlock` (ветка по `ecologyHabitat`).

### 16.1. Проблема

Все участники экосистемы захардкожены: `PlantCodeHelper` парсит коды `game:flower-*`, `game:tallgrass-*` и т.д.; gate `domain == "game"` отсекает сторонние моды. Добавление нового вида требует правки C#-кода.

### 16.2. Решение: контракт через JSON-атрибуты

Блок объявляет участие в экосистеме через атрибуты на blocktype:

```json
{
  "attributes": {
    "ecologyParticipant": true,
    "ecologySpecies": "bluegrass",
    "ecologyHabitat": "Terrestrial",
    "ecologySpreadBlock": "game:wildgrass-bluegrass-0-free",
    "ecologyMatureStages": ["3", "4"],
    "ecologySpreadRate": 0.6,
    "minTemp": 0, "maxTemp": 30,
    "minRain": 0.3, "maxRain": 0.8,
    "ecologyMinForest": 0, "ecologyMaxForest": 0.5,
    "ecologySameSpeciesSpacing": 1,
    "ecologyOtherSpeciesSpacing": 1,
    "ecologyContextAffinity": "Open",
    "ecologyHoldStrength": 0.8
  }
}
```

### 16.3. Точки интеграции

| Компонент | Изменение |
|-----------|-----------|
| `EcosystemParticipant.TryFromBlock` | Если `ecologyParticipant == true` → строить участника из атрибутов; иначе → текущий хардкод |
| `PlantCodeHelper.IsEcologyPlant` | Убрать gate `domain == "game"` для блоков с `ecologyParticipant` |
| `PlantCodeHelper.GetEcologySpecies` | Атрибут `ecologySpecies` как приоритет перед парсингом кода |
| `PlantRequirements.FromBlock` | Уже читает `minTemp`/`maxTemp`/`ecologySpreadRate` из атрибутов — подхватит |
| `ChunkFlowerScanner` | `ReproduceEnabled` → `TryFromBlock` → автоматически |

### 16.4. Архитектура контент-модов

```
[ecosystemflora]  — ядро: spread, stress, displacement, chunk scan
      ↑ парсит ecologyParticipant
[ecosystemgrass]  — ресурсный мод (JSON + текстуры, без C#):
                    blocktypes в домене game:,
                    атрибуты ecologyParticipant,
                    шейпы/текстуры трав
```

Контент-мод не содержит C#-кода. Любой мод-автор может добавить виды, следуя контракту.

### 16.5. Обратная совместимость

Текущие хардкоженные виды (цветы, tallgrass, ferns, berries, деревья, aquatic) продолжают работать через старый путь. Attribute-based путь — приоритетный, хардкод — fallback.

---

## 13. Ссылки

- Оригинал: https://mods.vintagestory.at/show/mod/53  
- Revival: https://mods.vintagestory.at/wildfarmingrevival  
- Прогресс: [`PROGRESS.md`](PROGRESS.md)  
- Промпт: [`PROMPT.md`](PROMPT.md)
