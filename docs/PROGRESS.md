# Прогресс разработки

**Текущая стадия:** `Ecosystem v2.1` — единая конкуренция за клетку (spread + displacement + stress + symbiosis).  
**Версия мода:** `2.4.1-ecosystem-v2.1` · **Игра:** Vintage Story 1.21+ · **Сборка:** .NET 10  

Последнее обновление: 2026-05-22.

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
| **4.2 — Aquatic** | Рогоз, камыш, кувшинка, водяной лютик | ✅ код; ⏳ длинный playtest |
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
| **Ягоды** | `fruitingbush-wild-*` (10 видов) → тот же блок; почва/лес по worldgen |
| **Папоротники** | `fern-*` (4) + `tallfern` — лес/влажность, свет ≥7 под кроной |

| Слой | Поведение |
|------|-----------|
| **Объект** | Ванильные блоки; цветы/водные — патч `EcosystemPlant`; **деревья** — скан чанка по `log-grown` (без патча на ствол) |
| **Участник** | `IEcosystemParticipant` → `EcosystemParticipant` |
| **Среда** | Температура (сезон), `WorldgenRainfall` + `ForestDensity`, почва, жидкость |
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
[ илистый гравий — muddygravel ]
```

| Случай | Клетка растения | Вариант блока |
|--------|-----------------|---------------|
| Берег, без водной колонки | `gravel.Y + 1`, под ногами ил | `land-normal` |
| Мелководье, ровно 1 вода над илом | тот же `gravel.Y + 1`, **внутри** водного блока | `water-normal` |
| 2+ воды между илом и поверхностью | — | spread **запрещён** |
| Ил не `muddygravel` | — | spread **запрещён** |
| Уже есть рогоз в колонке (X/Z) | — | spread **запрещён** |

**Камыш:** высота 2 блока; в воде — ровно 1 водный слой над илом (`ExactWaterDepth: 1`).

### Водяной лютик

- Spread ставит колонку от `aquatic-watercrowfoot-section`, сверху `tip` или `top` (~35% с цветами).
- Глубина воды над илом/почвой: 2–8 блоков (как worldgen).
- Реестр привязан к **нижнему** блоку колонки (`GetColumnBase`).

### Архитектура (код)

- [x] `EcosystemSystem`, `ReproducerRegistry`, `ChunkFlowerScanner`
- [x] `EnvironmentalContext` — `WorldGenValues` для rain/forest
- [x] `SuitabilityEvaluator` — ветки по `EcologyHabitat`
- [x] `WildFlowerClimate` + `WildAquaticEcology`
- [x] `WildFlowerSpacing` + `PlantSpacing` (горизонталь + запрет стека тростника в колонке)
- [x] `SpeciesSpread` — календарь и legacy
- [x] `ReproducePlacement`, `SurfacePlacement`, `ReedPlacement`, `WaterPlacement`
- [x] `CrowfootPlacement`, `CrowfootColumnPlacer`
- [x] `BlockFluidHelper` — `IsDedicatedWaterCell`, `CountWaterLayersAboveGravel`
- [x] `PlantCodeHelper` — species, `ResolveReedSpreadBlock` (land/water)
- [x] `EcosystemParticipant`
- [x] `CellCompetition`, `EcologySpreadFitness`, `FloraContextSampler`, `FloraSymbiosis`, `WildSpeciesModifiers`
- [x] Legacy не в сборке

### Патчи (`enabledpatches.json`)

| Файл | `entityClass` |
|------|----------------|
| `flower.json` | `EcosystemPlant` |
| `flower-lupine.json` | `EcosystemPlant` |
| `tallgrass.json` | `EcosystemPlant` |
| `reedpapyrus.json` | `EcosystemPlant` |
| `waterlily.json` | `EcosystemPlant` |
| `aquatic/watercrowfoot.json` | `EcosystemPlant` |

### Исправления (после v1)

| Проблема | Решение |
|----------|---------|
| Spread почти не работал | `WorldGenValues` для rain/forest |
| «То есть, то нет» на поляне | Пул кандидатов; fitness = min факторов |
| Реестр сбрасывался зимой | `SameEcologySpecies` для `-free`/`-snow` |
| Одинаковая скорость видов | `SpreadRate` + `UseSpeciesSpreadRates` |
| NLR на calendar в `Init` | Лог календаря на первом reproduce-тике |
| Рогоз без ила | Только `muddygravel`; привязка к колонке ила |
| Рогоз в воздухе / столб воды | Только `gravel+1`; один водный слой; без `gravel+2` |
| Рогоз на рогозе | Запрет стека в колонке; не считать чужой тростник «водой» |
| Spread не в водный блок | `IsDedicatedWaterCell` + `water-normal` по клетке |
| Камыш / лютик | `VerticalBlocks`, `UnderwaterColumn`, колонка лютика |
| `ProcessStress` IndexOutOfRange при stress death | Удаление растений отложено до конца обхода; live `entries.Count` в round-robin |

### Playtest

| Область | Статус |
|---------|--------|
| MVP-vanilla-flowers | ✅ |
| Ecosystem v1 (rain/forest, rates) | ✅ |
| Spacing + calendar | ✅ в коде |
| **v2.1 — spread + displacement на лугах** | ✅ сессия 2026-05-22: пустая/редкая поверхность пышно зарастает; видимое замещение блоков |
| Рогоз: ил → 1 вода → рогоз | ✅ по отчёту пользователя |
| Камыш, кувшинка, лютик | ⏳ длинная сессия, жаркий/холодный климат |
| Покос → быстрые виды → вытеснение | ⏳ |
| Symbiosis cascade при сломе дерева | ⏳ |

**Чеклист aquatic:** мелководье у озера; нет второго блока воды под рогозом; камыш только при temp ≥ 24; лютик в глубине 2+; `ReproduceDebug: false` для финала.

---

## Конфиг (`wildfarming-ecosystem.json`)

| Параметр | Назначение |
|----------|------------|
| `ReproduceRadius` / `ReproduceVerticalSearch` | Поиск клеток |
| `ReproduceChance` | Базовый шанс (× SpreadRate вида) |
| `ReproduceAttemptsPerYear` | Попыток spread за **игровой год** при SpreadRate=1 |
| `UseCalendarScaledSpread` | Интервал в игровых днях |
| `ReproduceIntervalHours` | Legacy, если calendar off |
| `MinSpeciesReproduceIntervalDays` / `Hours` | Пол между попытками (0 = без пола) |
| `MinFitness` | Порог fitness (0–1) |
| `ApplyWorldgenRainForest` | Rain/forest из worldgen |
| `UseSpeciesSpreadRates` | Per-species `SpreadRate` |
| `PlantSpacingEnabled` | Дистанция между растениями |
| `DefaultSameSpeciesSpacing` / `DefaultOtherSpeciesSpacing` | Базовые дистанции |
| `SpacingVerticalSearch` | ±Y при проверке соседей |
| `MaxReproduceAttemptsPerTick` | Лимит CPU |
| `MaxChunkColumnsScannedPerTick` / `MaxRegistrationsPerTick` | Очередь чанков |
| `ReproduceDebug` | Лог spread / Skip |
| `OnlyActivateNearPlayers` | Опция для слабых серверов |
| `UseCellDisplacement` / `DisplacementHoldMargin` | Вытеснение занятых ecology-клеток |
| `EnableStressDeath` / `MaxFailedSurvivalChecks` | Стресс-смерть при несоответствии нише |
| `EnableSymbiosis` / `UseFloraContext` | Симбиоз с деревьями; локальный forest-edge контекст |
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

Mod DB и публикация — **пока рано** (нужны баланс, «полнота» луга, playtest).

### v1.x — контент и баланс

- [x] **Tallgrass** в экосистеме
- [x] Патч **drygrass** с цветов (нож/коса)
- [x] Пресеты баланса `natural` / `lush` / `sparse`

### v2.1 — единая конкуренция за клетку (§11 PROJECT_VISION)

**Один механизм:** spread + displacement + stress death + symbiosis. Без `DisturbedTracker` / colonizer window.

- [x] `CellCompetition` — spreadScore vs holdScore, displacement
- [x] `FloraContext` как множитель fitness (опушка emergent)
- [x] `EnableStressDeath` на `EcosystemPlant`
- [x] `FloraSymbiosis` — каскад при смерти дерева/хоста
- [x] `WildSpeciesModifiers`: `HoldStrength` вместо disturbed/colonizer
- [x] Playtest v2.1 (2026-05-22): луга и опушки визуально «живые», spread + displacement заметны
- [ ] Playtest: покос → быстрые виды → вытеснение; symbiosis cascade
- [x] Playtest покос (2026-05): зарастание пустых клеток работает
- [x] Playtest опушка: полевые цветы у леса останавливаются
- [x] Playtest symbiosis + stress (2026-05): ландыш/папоротники под стволом после срубки
- [x] Playtest aquatic colonization после spread rate bump — работает; ⏳ край водоёма: spacing/соседи на непригодном субстрате (берег без ила)
- [ ] Land claims при displacement
- [ ] Тюнинг `HoldStrength` / `DisplacementHoldMargin` по playtest

### v2.0 (устарело → заменено v2.1)

~~DisturbedTracker, colonizer window, покос как отдельное состояние~~ — удалено в пользу §11.

### Обратная связь игроку (позже)

- [ ] Handbook / dominant species по зоне (опционально)

### Playtest и техдолг

- [x] Базовый playtest (сессия 2026-05)
- [x] v2.1 playtest — плотное зарастание лугов, displacement (2026-05-22)
- [ ] Длинный playtest aquatic (камыш в жаре, лютик в глубине, основание у ила)
- [x] Папоротники `fern-*`, `tallfern`
- [x] Деревья: spread саженцев от `log-grown`
- [x] Ягодные кусты + почвенные предпочтения
- [x] Водяной лютик: колонка только под водой (не выше поверхности)
- [ ] Убрать `entityClass`, только chunk-scan (опционально)
- [ ] Land claims при reproduce
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

- [ ] **O(1) remove в registry** — swap-remove + index map вместо `List.Remove` O(n)
- [ ] **Cheap-first candidates** — в `CollectSpreadCandidates` сначала air + solid ground + fluid, тяжёлый Sample только после
- [ ] **Spacing hash** — per-chunk индекс ecology-растений для `PlantSpacing` вместо brute-force scan на каждого кандидата
- [ ] **Отдельный roundRobinIndex** для `ProcessDue` и `ProcessStress` (логика, не perf)

#### Фаза 3 — опционально / позже

- [ ] **Top-block cache per column** в chunk scan (`ChunkFlowerScanner`) + invalidation на SetBlock
- [ ] Меньше аллокаций: reuse `BlockPos`, struct context где возможно
- [ ] ~~Многопоточный spread/displace~~ — **не делать** (chunk unload, гонки)

#### Конфиг как throttle (уже есть)

| Параметр | Эффект |
|----------|--------|
| `OnlyActivateNearPlayers` | Главный рычаг; без spatial tick в коде — половинчатый |
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
| (local) | Fix ProcessStress round-robin crash; docs + playtest notes |

---

## Быстрые ссылки

- Конфиг: `%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`
- Лог: `%AppData%\VintagestoryData\Logs\server-main.log` (`ReproduceDebug: true`)
- Сборка: `dotnet build` → `bin\Debug\Mods\wildfarming\` (копируется в `Mods\wildfarming` при закрытой игре)
