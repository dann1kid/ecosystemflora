# Прогресс разработки

**Текущая стадия:** `Ecosystem v1` — завершён (код + playtest, 2026-05-22).  
**Версия мода:** `2.1.0-ecosystem-v1` · **Игра:** Vintage Story 1.21+ · **Сборка:** .NET 10  

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
| **4 — Ecosystem v1** | Участники, rain/forest, выбор клеток, скорость по виду | ✅ **завершено** |
| **5 — Content** | Папоротники, кусты (выборочно) | 📋 опционально |

---

## Текущая модель (Ecosystem v1)

Дикая экосистема на **ванильных** `game:flower-*` (20 видов). В мире остаются ванильные блоки; при снятии мода сохранения целы.

| Слой | Поведение |
|------|-----------|
| **Объект** | `game:flower-{вид}-free` / `-snow` |
| **Участник** | `IEcosystemParticipant` → `FlowerEcosystemParticipant` (requirements + spread codes) |
| **Среда** | Температура (сезон), `WorldgenRainfall` + `ForestDensity` (карты worldgen), почва, жидкость |
| **Клетка-кандидат** | Скан всех свободных клеток в радиусе → выбор **взвешенный по fitness** |
| **Скорость** | `SpreadRate` per-species: интервал `base/SpreadRate`, шанс `base×SpreadRate` |
| **Семена мода** | Не в сборке |

### Архитектура (код)

- [x] `EcosystemSystem`, `ReproducerRegistry`, `ChunkFlowerScanner`
- [x] `EnvironmentalContext` — `WorldGenValues` для rain/forest, `NowValues` для температуры
- [x] `SuitabilityEvaluator` — rain/forest, `ReproduceFitness`, физика
- [x] `WildFlowerClimate` — temp + rain/forest envelope + **SpreadRate** (20 видов)
- [x] `SpeciesSpread` — эффективный интервал/шанс из конфига и вида
- [x] `ReproducePlacement` — сбор кандидатов, weighted pick (не «первая в shuffle»)
- [x] `FlowerEcosystemParticipant`, `IEcosystemParticipant`
- [x] `PlantCodeHelper` — species match `free`/`snow` для реестра
- [x] Legacy не в сборке

### Playtest

| Стадия | Результат |
|--------|-----------|
| MVP-vanilla-flowers | ✅ поляны, склоны, вода, производительность |
| Ecosystem v1 | ✅ торф/почва блокируют spread (видно в игре); разная скорость по видам; кандидаты из пула |

### Исправления после v1 (сессия)

| Проблема | Решение |
|----------|---------|
| Spread почти не работал | `WorldGenValues` для rain/forest (не `NowValues`) |
| «То есть, то нет» на одной поляне | Выбор из **всех** валидных клеток; fitness = min факторов; без сезонного temp на offspring |
| Реестр сбрасывался зимой | `IsMatureBlock`: тот же species для `-free`/`-snow` |
| Одинаковая скорость у всех видов | `SpreadRate` + `UseSpeciesSpreadRates` |

### SpreadRate (ориентир при base 48h / chance 0.35)

| SpreadRate | Примеры видов |
|------------|----------------|
| **2.2–2.8** | horsetail, heather, westerngorse, redtopgrass |
| **1.5–2.0** | mugwort, cowparsley, catmint, bluebell |
| **1.0–1.4** | wilddaisy, cornflower, forgetmenot, woad |
| **0.35–0.65** | goldenpoppy, edelweiss, ghostpipe*, daffodil, orangemallow |

---

## Конфиг (`wildfarming-ecosystem.json`)

| Параметр | Назначение |
|----------|------------|
| `ReproduceRadius` / `ReproduceVerticalSearch` | Поиск клеток |
| `ReproduceChance` / `ReproduceIntervalHours` | Базовый шанс и период (масштабируется видом) |
| `MinFitness` | Порог fitness (0–1) |
| `ApplyWorldgenRainForest` | Rain/forest из worldgen |
| `UseSpeciesSpreadRates` | Per-species `SpreadRate` |
| `MinSpeciesReproduceIntervalHours` | Минимум часов между попытками |
| `MaxReproduceAttemptsPerTick` | Лимит CPU |
| `ReproduceDebug` | Лог кандидатов, spreadRate, причин Skip |
| `OnlyActivateNearPlayers` | Опция для слабых серверов |

**Рекомендуемая «естественная» база:** `ReproduceIntervalHours: 48`, `ReproduceChance: 0.35` (не тестовые `1`/`1`).

---

## Все виды цветов (20)

catmint, cornflower, forgetmenot, edelweiss, heather, horsetail, orangemallow, wilddaisy, westerngorse, cowparsley, goldenpoppy, lilyofthevalley, woad, redtopgrass, bluebell, ghostpipewhite, ghostpipepink, ghostpipered, daffodil, mugwort.

---

## Что ещё не сделано

- [ ] Папоротники `game:fern-*`
- [ ] Убрать `entityClass`, только chunk-scan (опционально)
- [ ] Land claims при reproduce
- [ ] Публикация / modid rename (TBD)

---

## Git (история)

| Коммит | Содержание |
|--------|------------|
| `7de0354` | Ecosystem MVP, VS solution, docs |
| `ffff1de` | Survival, worldgen temps |
| `9f8db0f` | Knife recipes fix |
| `b1d5978` | Vanilla propagate system |
| `b04c7c2` | Optimization, all 20 flowers, chunk queue |
| `bacb506` | Ecosystem v1: participants, rain/forest, spread rates, candidate pool |

---

## Быстрые ссылки

- Конфиг: `%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`
- Лог: `%AppData%\VintagestoryData\Logs\server-main.log` (`ReproduceDebug: true`)
