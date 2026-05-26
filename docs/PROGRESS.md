# Прогресс разработки

**Текущая стадия:** `Ecosystem v2.3` — сезонность, ниша, perf audit, unit tests; готово к ModDB playtest.  
**Версия мода:** `2.7.0` · **Игра:** Vintage Story 1.21+ · **Сборка:** .NET 10  

Последнее обновление: 2026-05-26.

См. также: [PROJECT_VISION.md](PROJECT_VISION.md) (теория), [PROMPT.md](PROMPT.md) (промпт для агентов).

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

## Текущая модель (Ecosystem v2.1)

Дикая экосистема на **ванильных** блоках. В мире остаются ванильные блоки; при снятии мода сохранения целы.

**Конкуренция за клетку:** spread на пустые клетки + displacement занятых (`CellCompetition`) + stress death + symbiosis. Без `DisturbedTracker` / colonizer window.

| Группа | Блоки |
|--------|--------|
| **Цветы** | `game:flower-*` (20 видов) + `flower-lupine-*` |
| **Луг** | `tallgrass-*` — матрица под цветами |
| **Тростник** | `tallplant-coopersreed-*` (рогоз), `tallplant-papyrus-*` (камыш) |
| **Поверхностная вода** | `waterlily` (кувшинка) |
| **Подводный** | `aquatic-watercrowfoot-*` (водяной лютик: section / tip / top) |
| **Деревья** | `log-grown-{wood}-*` → spread `sapling-{wood}-free` (14 пород; бамбук/aged — нет) |
| **Ягоды** | `fruitingbush-wild-*` (10 видов) → тот же блок; почва + `LocalForestCover` |
| **Папоротники** | `fern-*` (4) + `tallfern` — лес/влажность, свет ≥7 под кроной |

| Слой | Поведение |
|------|-----------|
| **Объект** | Ванильные блоки; регистрация — `ChunkFlowerScanner` при загрузке чанка (без `entityClass` патчей); **деревья** — скан чанка по `log-grown` |
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
| `TerrestrialTree` | Зрелый ствол `log-grown` | `TreePlacement` — саженец на почве, солнце ≥11; рост — **ванильный** treegen |

### Деревья (вариант A)

- **Родитель:** основание колонны `log-grown-{wood}-*` (`WildTreeEcology` — климат, rain/forest, `SpreadRadius`, spacing).
- **Потомство:** `sapling-{wood}-free`; вырастет/погибнет — только игра.
- **Регистрация:** при загрузке чанка (скан колонки) + `PendingTreeSaplings` для саженцев, посаженных модом, пока не появится `log-grown`.
- **Без** living trunk / mod-блоков ствола; при снятии мода — обычные бревна и саженцы.

### Рогоз и камыш (правила spread)

Колонка снизу вверх для **мелководья**:

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

**Камыш:** высота 2 блока; в воде — ровно 1 водный слой над илом (`ExactWaterDepth: 1`).

### Водяной лютик

- Spread ставит колонку от `aquatic-watercrowfoot-section`, сверху `tip` или `top` (~35% с цветами).
- Глубина воды над илом/почвой: 2–8 блоков (как worldgen).
- Реестр привязан к **нижнему** блоку колонки (`GetColumnBase`).

### Архитектура (код)

- [x] `EcosystemSystem`, `ReproducerRegistry`, `ChunkFlowerScanner`
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

Пусто — патчи `entityClass` удалены (регистрация через `ChunkFlowerScanner`). Старые BE самоудаляются при загрузке чанка.

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

### Playtest

| Область | Статус |
|---------|--------|
| MVP-vanilla-flowers | ✅ |
| Ecosystem v1 (rain, rates) + local forest | ✅ |
| Spacing + calendar | ✅ в коде |
| **v2.1 — spread + displacement на лугах** | ✅ сессия 2026-05-22: пустая/редкая поверхность пышно зарастает; видимое замещение блоков |
| Рогоз: ил → 1 вода → рогоз | ✅ по отчёту пользователя |
| **Aquatic** (рогоз, камыш, лютик, `gravel-*`) | ✅ 2026-05-24: spread в воду/берег; луг не захватывается |
| Покос → быстрые виды → вытеснение | ⏳ |
| Symbiosis cascade при сломе дерева | ⏳ |

**Aquatic (закрыто 2026-05-24):** `gravel-*` под водой; берег — land-normal у воды; `SpreadVacancy`; без spread на луг. Опционально позже: камыш в жаре, лютик на глубине 8+.

---

## Конфиг (`ecosystemflora.json`)

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
| `TickBudgetMs` | Жёсткий потолок ms/тик (default 5); 0 = без лимита |
| `MaxReproduceAttemptsPerTick` | Лимит CPU (spread) |
| `MaxStressChecksPerTick` | Лимит CPU (stress) |
| `MaxChunkColumnsScannedPerTick` / `MaxRegistrationsPerTick` | Очередь чанков |
| `ReproduceDebug` / `VerboseLogging` | Лог spread / Skip; master-switch логирования |
| `OnlyActivateNearPlayers` | Default **true** — реестр только в радиусе игроков |
| `PlayerActivationRadiusBlocks` | Радиус активации (192 по умолчанию) |
| `UseCellDisplacement` / `DisplacementHoldMargin` | Вытеснение занятых ecology-клеток |
| `EnableStressDeath` / `MaxFailedSurvivalChecks` | Стресс-смерть при несоответствии нише |
| `EnableSymbiosis` / `UseFloraContext` | Симбиоз с деревьями; локальный forest-edge контекст |
| `RespectLandClaims` | Нет spread/displace/stress/soil внутри land claim |
| `UseSeasonalEcology` / `SeasonalStressEnabled` | Spread и зимняя/осенняя stress по сезону (`WildSpeciesSeason`) |
| `EnableTrampling` / `TramplingRadius` / `TramplingStressThreshold` | Протаптывание (default **off**): растения гибнут рядом с часто ходящими игроками |
| `TramplingSoilDegradation` | Деградация почвы на протоптанных тропах (default **off**) |
| `EnableFlowerDrygrass` | Дропс drygrass с цветов при срезании ножом/косой |
| `BalancePreset` | `natural` / `lush` / `sparse` — пресеты spread |

**Рекомендуемая «естественная» база:** `BalancePreset: natural` или `lush` для более плотного луга; не тестовые `ReproduceChance: 1` / `MinFitness: 0.1` в финальной игре.

---

## Виды в экосистеме

**Цветы (20):** catmint, cornflower, forgetmenot, edelweiss, heather, horsetail, orangemallow, wilddaisy, westerngorse, cowparsley, goldenpoppy, lilyofthevalley, woad, redtopgrass, bluebell, ghostpipewhite, ghostpipepink, ghostpipered, daffodil, mugwort.

**Дополнительно:** lupine (`flower-lupine-*`).

**Водная флора:**

| species | RU | Примечание |
|---------|-----|------------|
| `coopersreed` | рогоз | ил + 0–1 вода над илом |
| `papyrus` | камыш | жаркий климат; 2 блока высотой |
| `waterlily` | кувшинка | поверхность воды |
| `watercrowfoot` | водяной лютик | подводная колонка |

---

## Roadmap / TODO

Mod DB — после сезонного MVP, strip legacy, rename BE, короткого perf-pass (см. **Дорога к ModDB**).

**Следующие итерации:**

| Версия | Содержание | Статус |
|--------|------------|--------|
| **v3.0** | Ягодные кусты 1.22 — trait inheritance при spread | [ ] план |
| **v3.1** | Attribute-based participant contract — сторонние моды без хардкода | [ ] план |

### Дорога к ModDB (план)

| Шаг | Содержание | Статус |
|-----|------------|--------|
| 1 | **Сезонность** — множители spread/stress по месяцу/сезону, зимняя выживаемость per species | [x] MVP |
| 2 | **Strip** legacy JakeCool (мёртвый код/assets вне сборки) | [x] done |
| 3 | **Rename** `EcosystemPlant` → `EcoSystemLife` | [x] done |
| 4 | Рефактор + **perf-анализ** (фаза 3 — по результатам) | [x] done |
| 5 | **Unit-тесты** — чистые функции (scoring, classification, season) | [x] 46 tests |
| 6 | **Разбить `BlockFluidHelper`** → `ReedColumnHelper`, `WaterColumnHelper` | [x] done |
| 7 | ModDB + hotfix по отзывам | [ ] |

**Отложено (не экосистема):** зимняя **листва** на `log-grown` — визуал/ассеты; частичное срезание leaves без понятного regrow = отдельный мод или companion, не v1.

### v2.3 — сезонность

- [x] `WildSpeciesSeason` — spread по `EnumSeason`, `WinterSurvival`, `FallDieoffChance`
- [x] `SeasonEcology` — chance, interval, fitness; весенний ramp (`GetSeasonRel`)
- [x] Зимнее/осеннее отмирание — `SeasonalStressEnabled` (terrestrial stress)
- [ ] (позже) кривая 12 месяцев per species

Mod DB и публикация — **после** шагов 1–4 выше; длинные playtest-сессии не блокер.

### v1.x — контент и баланс

- [x] **Tallgrass** в экосистеме
- [x] Патч **drygrass** с цветов (нож/коса)
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
- [ ] Тюнинг `HoldStrength` / `DisplacementHoldMargin` по playtest

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
- [ ] **Залежь** — farmland без культуры + ванильные сорняки → медленный N (опционально)

См. [PROJECT_VISION.md §14](PROJECT_VISION.md#14-ниша-почва-влажность-освещение-v22).

### v2.0 (устарело → заменено v2.1)

~~DisturbedTracker, colonizer window, покос как отдельное состояние~~ — удалено в пользу §11.

### v2.6 — протаптывание дорожек (trampling)

Растения вблизи игроков накапливают `TramplingExposure`; при достижении порога — stress-check как обычная стресс-смерть. Земля деградирует (`SoilSuccessionEvent.Trampled`): −0.25 fertility tier, −8 moisture. Плотно хоженые тропы становятся бесплодными/сухими.

- [x] `TramplingExposure` на `ReproducerEntry`
- [x] Конфиг: `EnableTrampling`, `TramplingRadius`, `TramplingStressThreshold`, `TramplingSoilDegradation`
- [x] Ветка trampling в `ProcessStress` — `PlayerProximity.IsNearAnyPlayer` с малым радиусом
- [x] `SoilSuccessionEvent.Trampled` + `TrampledImpact` в `WildSpeciesSoilSuccession`
- [x] `RemoveEcologyPlant` принимает `soilEvent` (default `Death`; trampled = `Trampled`)
- [ ] Playtest: наблюдаемое протаптывание тропинок при повторном хождении

### v3.0 — ягодные кусты 1.22 (trait inheritance)

**Проблема:** VS 1.22 переработал ягодные кусты: новые блоки, block entity с нутриентами/возрастом/стадиями, система **наследственных черт** (traits). Traits хранятся в `TreeAttributes` блок-сущности и передаются через черенки. Текущий spread ставит голый блок через `new ItemStack(spreadBlock)` → `SetBlock` — traits **не клонируются**, нутриенты не инициализируются.

**Цель:** при spread ягодного куста — клонировать traits родителя, инициализировать BE как при посадке черенка.

- [ ] Определить формат traits в BE ванильного куста 1.22 (`BlockEntityBerryBush` / TreeAttributes)
- [ ] В `PlaceSpreadBlock`: если habitat = berry, читать traits из BE родителя (`parentOrigin`)
- [ ] Записать traits в BE дочернего куста после `SetBlock`; инициализировать нутриенты/возраст в начальные значения
- [ ] Конфиг-флаг `CloneBerryTraits` (default true; false = совместимость с 1.21)
- [ ] Опционально: мутация — шанс потерять/приобрести trait при spread (природная вариативность)
- [ ] Playtest: убедиться что кусты от spread нормально растут/плодоносят с клонированными traits

**Зависимость:** VS 1.22+ API; для 1.21 — текущее поведение без traits.

### v3.1 — attribute-based participant contract (сторонние моды)

**Проблема:** сейчас все участники экосистемы жёстко захардкожены: `PlantCodeHelper` парсит коды `game:flower-*`, `game:tallgrass-*` и т.д. Сторонние моды (Wildgrass Fork, контент-паки) не могут участвовать в экосистеме без правок кода.

**Цель:** блок объявляет участие в экосистеме через JSON-атрибуты; экомод парсит их без хардкода видов.

**Контракт на блоке (JSON attributes):**

```json
{
  "ecologyParticipant": true,
  "ecologySpecies": "bluegrass",
  "ecologyHabitat": "Terrestrial",
  "ecologySpreadBlock": "game:wildgrass-bluegrass-0-free",
  "ecologyMatureStages": ["3", "4"],
  "ecologySpreadRate": 0.6,
  "minTemp": 0, "maxTemp": 30,
  "minRain": 0.3, "maxRain": 0.8
}
```

- [ ] **Парсер атрибутов** — `EcosystemParticipant.TryFromBlock`: если `ecologyParticipant == true`, строить участника из атрибутов блока (fallback на текущий хардкод)
- [ ] **`PlantCodeHelper`** — убрать gate `domain == "game"` для блоков с `ecologyParticipant`; species из `ecologySpecies` атрибута
- [ ] **`ChunkFlowerScanner`** — `ReproduceEnabled` уже вызывает `TryFromBlock`, подхватит автоматически
- [ ] **Spacing / displacement** — работает по species name, без изменений
- [ ] **Конфиг** `EnableThirdPartyParticipants` (default true)
- [ ] **Документация** для авторов контент-модов: какие атрибуты ставить на blocktype JSON

**Архитектура контент-модов:**

```
[ecosystemflora]  — экосистемное ядро, парсит ecologyParticipant
      ↑
[ecosystemgrass]  — контент-мод: блоки wildgrass в домене game:,
                    JSON с ecologyParticipant атрибутами,
                    текстуры/модели; без кода
```

Контент-мод — чистый ресурсный мод (JSON + текстуры), не нуждается в C#. Экомод — платформа.

### Обратная связь игроку (позже, post-ModDB)

- [ ] **Handbook / dominant species** — UX: подсказка «кто доминирует» в зоне (niche + flora context), не механика spread

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
- [x] **Chunk-scan без `EcoSystemLife` BE** — техдолг: убраны патчи `entityClass`; BE самоудаляется при загрузке старых чанков; мод можно безопасно снять
- [ ] Push на `origin` (когда будет доступ) / публикация Mod DB (позже)

### Оптимизация (perf roadmap)

План зафиксирован 2026-05-22. Теория и ограничения VS API: [PROJECT_VISION.md §12](PROJECT_VISION.md#12-производительность-roadmap).

**Контекст:** при ~18k reproducers узкое место — стоимость одного spread (`CollectSpreadCandidates`) и холостой обход global `List` при `OnlyActivateNearPlayers`. Потоки для spread/stress **не планируются** (BlockAccessor только main thread).

#### Фаза 1 — быстрые wins (высокий impact, низкий риск)

- [x] **Spatial tick** — `ProcessDue` / `ProcessStress` по `byChunk` только в hot chunks (радиус игроков), не global round-robin по всему реестру
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
- [x] **`OnlyActivateNearPlayers = true`** по умолчанию — реестр обрабатывает только растения в радиусе 192 блоков от игроков
- [x] **Time-budget (`Stopwatch`)** — `TickBudgetMs` (default 5ms); оба тик-хендлера прерывают обработку при превышении бюджета
- [x] **Кэш `GetClimateAt(NowValues)`** — температура кэшируется per-column per-tick через generation counter в `EnvironmentalColumnCache`; стресс-проверки пачкой не дублируют дорогие вызовы
- [x] **`OnChunkScanTick` интервал** — 500ms → 2000ms; сканирование чанков не срочная операция

#### Конфиг как throttle (уже есть)

| Параметр | Эффект |
|----------|--------|
| `OnlyActivateNearPlayers` | Главный рычаг — default **true** с v2.7 |
| `TickBudgetMs` | Жёсткий потолок ms/тик (default 5); 0 = без лимита |
| `MaxReproduceAttemptsPerTick` / `MaxStressChecksPerTick` | CPU ceiling |
| `FloraContextCacheHours` | Меньше tree-neighbor scan |
| `ReproduceRadius` | O(r²) в spread; 4→3 ≈ −40% колонок |
| `PlantSpacingEnabled: false` | Большой win, ломает баланс |

**Первый PR:** spatial tick + static climate cache — ✅ реализовано (2026-05-22).

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
| *(pending)* | Perf phase 4: heightmap chunk scan, tick budget, lower defaults, NowValues cache; v2.7.0 |

---

## Быстрые ссылки

- Конфиг: `%AppData%\VintagestoryData\ModConfig\ecosystemflora.json`
- Лог: `%AppData%\VintagestoryData\Logs\server-main.log` (`VerboseLogging: true` + `ReproduceDebug: true`)
- Сборка: `dotnet build` → `bin\Debug\Mods\ecosystemflora\` (копируется в `Mods\ecosystemflora` при закрытой игре)
