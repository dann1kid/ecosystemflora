# Прогресс разработки

**Текущая стадия:** `MVP-beta` — цикл catmint (посадка, рост, reproduce) проверен в игре (2026-05-23).  
**Версия мода:** `2.0.0-mvp` · **Игра:** Vintage Story 1.21+ · **Сборка:** .NET 10  

Последнее обновление: 2026-05-23.

См. также: [PROJECT_VISION.md](PROJECT_VISION.md) (теория), [PROMPT.md](PROMPT.md) (промпт для агентов).

---

## Стадии проекта

| Стадия | Описание | Статус |
|--------|----------|--------|
| **0 — Archive** | Оригинал JakeCool19 v1.2.0, legacy-код | ✅ в репо, не в сборке |
| **1 — MVP-alpha** | Экосистемное ядро, цветы, три слоя | ✅ завершено |
| **2 — MVP-beta** | Playtest catmint, стабильный цикл, документация | **▶ сейчас** |
| **3 — Ecosystem v1** | `IEcosystemParticipant`, rain/forest, ванильные взрослые в реестре | 📋 план |
| **4 — Content** | Кусты/кактусы/грибы (выборочно), без living trees | 📋 опционально |

---

## Что сделано (MVP-alpha)

### Архитектура

- [x] `src/Ecosystem/` — `EcosystemSystem`, `EnvironmentalContext`, `SuitabilityEvaluator`, `PlantRequirements`, `PlantCodeHelper`
- [x] Конфиг `wildfarming-ecosystem.json` (радиус, шанс, MinFitness, смерть, `GrowthHoursMultiplier`)
- [x] Три слоя: посадка без климата → выживание по климату/почве → reproduce только на подходящих клетках
- [x] `wildfarming.sln` + автодеплой в `Mods/wildfarming/`

### Игровой цикл — catmint (playtest 2026-05-23, OK)

- [x] Семена сажаются на плодородную почву
- [x] `wildplant` дорастает до ванильного цветка в подходящем климате
- [x] Подсказка блока: дни до созревания, «too hot/cold»
- [x] На плохом климате — смерть без дропа семян
- [x] Дикое размножение (reproduce) — по отчёту playtest в норме

### Исправленные баги (2026-05-23)

| Проблема | Причина | Решение |
|----------|---------|---------|
| Семена не сажались / сразу выпадали | Неверный код `wildplant-wildseeds-...`; `Unstable`; `drops` с семенами | `PlantCodeHelper.WildPlantCodeFromSeed`; убран `Unstable`; `drops: []` |
| «Бедные условия» на хорошей почве | Проверка `replaceable` на уже посаженном блоке (5000 &lt; 9501) | Отдельно: `MeetsPlacement` vs `MeetsSurvival` |
| «&lt; 1 день», но ждать ещё долго | Каждые 18 ч провал → `blossomAt += 18` | Исправлен survival-check |
| Нереалистичный климат | Старые min/max в JSON | Температуры из `game:worldgen/blockpatches/flower.json` |
| Блокировка посадки по MinFitness | Путаница слоёв | Посадка только физика + claims |

### Контент

- [x] Все цветы из `wildplant.json` — температуры worldgen
- [x] Дикое размножение включено только для **catmint** (`ecologyReproduceByType`)
- [x] Legacy (деревья, грибы, лианы, Harmony) — **не компилируется**

---

## Что ещё не сделано

- [ ] Reproduce для других цветов (не только catmint)
- [ ] Рецепты семян (нож + цветок) — smoke-test на 1.21+
- [ ] `IEcosystemParticipant` / capabilities (vision §3.2)
- [ ] Учёт `minRain` / `minForest` как в worldgen

---

## Следующие шаги

1. `ecologyReproduceByType` для 2–3 популярных цветов.
2. Проверить `flowerseeds.json` в игре.
3. Push на `origin` при готовности.

---

## Git

| Коммит | Содержание |
|--------|------------|
| `7de0354` | Ecosystem MVP, VS solution, docs |
| *(этот коммит)* | Survival fix, worldgen temps, PROGRESS, playtest catmint |

---

## Быстрые ссылки для теста

- Конфиг: `%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`
- Catmint: **5–19 °C**, ~8 суток роста (`192` ч)
- Ускорение: `"GrowthHoursMultiplier": 0.1`
