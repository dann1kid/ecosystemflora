# Прогресс разработки

**Текущая стадия:** `Ecosystem v1.1` — цветы + spacing + календарь + водная флора (код, 2026-05-22).  
**Версия мода:** `2.2.0-ecosystem-v1.1` · **Игра:** Vintage Story 1.21+ · **Сборка:** .NET 10  

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
| **4.2 — Aquatic** | Рогоз, камыш, кувшинка; субстрат `muddygravel` | ✅ код, playtest |
| **5 — Content** | Папоротники, кусты (выборочно) | 📋 опционально |

---

## Текущая модель (Ecosystem v1.1)

Дикая экосистема на **ванильных** блоках. В мире остаются ванильные блоки; при снятии мода сохранения целы.

| Группа | Блоки |
|--------|--------|
| **Цветы** | `game:flower-*` (20 видов) + `flower-lupine-*` |
| **Водная флора** | `tallplant-coopersreed-*` (рогоз), `tallplant-papyrus-*` (камыш), `waterlily` |

| Слой | Поведение |
|------|-----------|
| **Объект** | Ванильные блоки + `entityClass: EcosystemPlant` (патчи) |
| **Участник** | `IEcosystemParticipant` → `EcosystemParticipant` |
| **Среда** | Температура (сезон), `WorldgenRainfall` + `ForestDensity`, почва, жидкость |
| **Клетка-кандидат** | Скан в радиусе → **взвешенный** выбор по fitness |
| **Скорость** | `SpreadRate` per-species; календарь (`ReproduceAttemptsPerYear`) или legacy часы |
| **Spacing** | `WildFlowerSpacing` + конфиг `PlantSpacingEnabled` |
| **Семена мода** | Не в сборке |

### Среда обитания (`EcologyHabitat`)

| Habitat | Виды | Размещение |
|---------|------|------------|
| `Terrestrial` | Цветы, люпин | `SurfacePlacement`, без жидкости в клетке |
| `ReedNearWater` | Рогоз, камыш | `ReedPlacement`: **илистый гравий** (`muddygravel`) под корнями; рост в воде допустим |
| `WaterSurface` | Кувшинка | `WaterPlacement`: поверхность воды |

### Архитектура (код)

- [x] `EcosystemSystem`, `ReproducerRegistry`, `ChunkFlowerScanner`
- [x] `EnvironmentalContext` — `WorldGenValues` для rain/forest
- [x] `SuitabilityEvaluator` — ветки по habitat
- [x] `WildFlowerClimate` + `WildAquaticEcology` — temp/rain/forest + SpreadRate
- [x] `WildFlowerSpacing` + `PlantSpacing`
- [x] `SpeciesSpread` — календарь и legacy
- [x] `ReproducePlacement`, `SurfacePlacement`, `ReedPlacement`, `WaterPlacement`
- [x] `BlockFluidHelper` — жидкость, `HasReedSiltSubstrate` (поиск `muddygravel` вниз)
- [x] `PlantCodeHelper` — species для цветов, тростника, кувшинки
- [x] `EcosystemParticipant` (заменил `FlowerEcosystemParticipant`)
- [x] Legacy не в сборке

### Патчи (`enabledpatches.json`)

- `flower.json`, `flower-lupine.json`, `reedpapyrus.json`, `waterlily.json` → `EcosystemPlant`

### Исправления (сессии после v1)

| Проблема | Решение |
|----------|---------|
| Spread почти не работал | `WorldGenValues` для rain/forest |
| «То есть, то нет» на поляне | Пул кандидатов; fitness = min факторов |
| Реестр сбрасывался зимой | `SameEcologySpecies` для `-free`/`-snow` |
| Одинаковая скорость видов | `SpreadRate` + `UseSpeciesSpreadRates` |
| NLR на calendar в `Init` | Лог календаря на первом reproduce-тике |
| Медленный woad/люпин при тест-конфиге | `MinSpeciesReproduceIntervalHours` default **0** |
| Рогоз без ила при spread | Только `muddygravel` под корнями (скан вниз через воду) |
| Ошибочный запрет «только суша» | Снят; вариант `water`/`land` копируется с родителя |

### Playtest

| Стадия | Результат |
|--------|-----------|
| MVP-vanilla-flowers | ✅ поляны, склоны, производительность |
| Ecosystem v1 | ✅ rain/forest, spread rates, пул кандидатов |
| Spacing + calendar | ✅ в коде; настройка в конфиге |
| Aquatic + muddygravel | ⏳ проверить spread у озёрного берега |

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
| `ReproduceDebug` | Лог spread / Skip |
| `OnlyActivateNearPlayers` | Опция для слабых серверов |

**Рекомендуемая «естественная» база:** `ReproduceAttemptsPerYear: 36–48`, `ReproduceChance: 0.35` (не тестовые `1` / `0.1`).

---

## Виды в экосистеме

**Цветы (20):** catmint, cornflower, forgetmenot, edelweiss, heather, horsetail, orangemallow, wilddaisy, westerngorse, cowparsley, goldenpoppy, lilyofthevalley, woad, redtopgrass, bluebell, ghostpipewhite, ghostpipepink, ghostpipered, daffodil, mugwort.

**Дополнительно:** lupine (`flower-lupine-*`).

**Водная флора:** coopersreed (рогоз), papyrus (камыш), waterlily (кувшинка).

---

## Что ещё не сделано

- [ ] Playtest aquatic на длинной сессии (холод/жара для камыша)
- [ ] Папоротники `game:fern-*`
- [ ] Убрать `entityClass`, только chunk-scan (опционально)
- [ ] Land claims при reproduce
- [ ] Публикация / modid rename (TBD)

---

## Git (история)

| Коммит | Содержание |
|--------|------------|
| `bacb506` | Ecosystem v1: participants, rain/forest, spread rates, candidate pool |
| `4b78497` | Concurrent propagation queue |
| `99d88f6` | Plant spacing, calendar spread, config |
| `92ec0cf` | Aquatic flora, `muddygravel` substrate, `EcosystemParticipant` |

---

## Быстрые ссылки

- Конфиг: `%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`
- Лог: `%AppData%\VintagestoryData\Logs\server-main.log` (`ReproduceDebug: true`)
