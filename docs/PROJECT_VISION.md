# Wild Farming / Ecosystem Mod — видение проекта

Документ для разработчиков и AI-агентов: **теория**, **целевая архитектура**, **отношение к оригинальному Wild Farming и Revival**, **текущая стадия репозитория**.

Последнее обновление: 2026-05-22 (стадия: **Ecosystem v1.1**).

---

## 1. Зачем этот проект

Цель — не клон [Wild Farming - Revival](https://mods.vintagestory.at/wildfarmingrevival), а **узкая экосистемная прослойка** для Vintage Story:

- объекты **включены в мир** (климат, почва, соседи), а не живут как изолированные мини-игры;
- **живой** объект обязан **размножаться**; без размножения это декор, не член экосистемы;
- взаимодействие через **интерфейсы (capabilities / interests)** — целевая модель;
- размножение **только на пригодных клетках** → читаемая «история» на ландшафте без отдельного worldgen.

Использовать наработки JakeCool19 / Revival **как идеи**, не переносить весь feature set.

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
| **Мод при снятии** | Патч `entityClass: EcosystemPlant`; без мода — обычные блоки, сохранения целы |
| **Дикое размножение** | Тот же ванильный блок (или корректный land/water для тростника) на соседней клетке |
| **Семена / wildplant** | **Не** часть дикой экосистемы; не в сборке |
| **Культивация игроком** | Ванильные механики игры |

**Регистрация:** очередь сканирования чанков + `EcosystemPlant` BE. **Размножение:** round-robin по реестру, лимит попыток за тик.

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
    EcosystemSystem.cs
    ReproducerRegistry.cs
    ChunkFlowerScanner.cs
    ReproducePlacement.cs
    SurfacePlacement.cs
    ReedPlacement.cs
    WaterPlacement.cs
    CrowfootPlacement.cs
    CrowfootColumnPlacer.cs
    BlockFluidHelper.cs
    EnvironmentalContext.cs
    SuitabilityEvaluator.cs
    PlantRequirements.cs
    WildFlowerClimate.cs
    WildAquaticEcology.cs
    WildFlowerSpacing.cs
    PlantSpacing.cs
    SpeciesSpread.cs
    EcosystemParticipant.cs
    EcologyHabitat.cs
    PlantCodeHelper.cs
    EcosystemConfig.cs
  BlockEntity/
    EcosystemPlant.cs
assets/wildfarming/
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
| Длинный playtest aquatic | ⏳ |

---

## 6. Сравнение с оригиналом и Revival

### 6.1. Оригинал (репозиторий, v1.2.0)

- `wildplant` → `game:*`, mod-семена, living trees — **архив**, не в сборке.

### 6.2. Revival

- Референс баланса и UX; **не** merge и не co-load.
- Наш modid остаётся `wildfarming`, другая архитектура.

---

## 7. Текущая стадия репозитория

**Стадия: `Ecosystem v2.1`.** Единая конкуренция за клетку (§11); v1.x контент (tallgrass, drygrass, пресеты) — в main.

| Компонент | Статус |
|-----------|--------|
| Экосистема: цветы, люпин, tallgrass, ferns, berries, trees | ✅ |
| Водная флора (4 вида) | ✅ код |
| Rain/forest + candidate pool + spacing | ✅ |
| Cell competition (displace, stress, symbiosis, flora context) | ✅ |
| Legacy в сборке | ⏸ |

- `modinfo.json` — `2.4.1-ecosystem-v2.1`, game `1.21.0`
- Конфиг: `wildfarming-ecosystem.json`

---

## 8. Правила для агентов

1. **Не расширять** trees / vines / mushrooms без явного запроса.
2. **Дикая экосистема** — только ванильные блоки в мире.
3. **Размножение** — только `Score >= MinFitness` и habitat-specific placement.
4. **Тростник** — не ломать правило «ил + один водный блок»; не ставить `gravel+2` над столбом воды.
5. **Производительность** — очереди и лимиты; не трогать блоки из фоновых потоков.
6. API: **VS 1.21+**, **.NET 10**.
7. Коммиты — только по запросу пользователя.

---

## 9. Открытые решения и roadmap

Актуальный чеклист: **[`PROGRESS.md` → Roadmap / TODO](PROGRESS.md#roadmap--todo)**.

Кратко:

- **Mod DB** — отложено до баланса и длинного playtest (aquatic, покос, symbiosis).
- **v1.x** — tallgrass, drygrass-патч, пресеты баланса — ✅ в main.
- **v2.1** — единая конкуренция за клетку (§11); playtest лугов ✅ (2026-05-22).
- [ ] `modid` оставить `wildfarming` или переименовать при публикации
- [ ] Убрать `EcosystemPlant` BE, оставить только chunk-scan
- [ ] Land claims при reproduce

---

## 10. Ecosystem v2 — теория границ флоры (дизайн)

**Идея:** опушка, луг и лес — не три отдельных биома в коде, а **зоны конкуренции**, которые **самоорганизуются** из соседей и worldgen. Граница леса = там, где меняется локальный контекст, а не отдельный worldgen-патч.

### 10.1. Связь с v1.1

Уже есть глобальные ареалы: `WorldgenRainfall`, `ForestDensity`, климат, почва (`SoilKind`).  
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
- Глобальный `ForestDensity` из climate **остаётся**; локальный контекст — **уточнение**, не замена.

---

## 11. Ecosystem v2.1 — единая конкуренция за клетку (реализовано)

**Идея:** один механизм вместо отдельных «покоса», «disturbed» и «опушки-биома». Растения конкурируют за клетки; покос игрока = просто освобождение клетки в общем пуле.

### 11.1. Формула

На клетке-кандидате для вида-challenger:

```
spreadScore = ReproduceFitness × ContextMultiplier × SpreadRate
holdScore   = ReproduceFitness × ContextMultiplier × HoldStrength × min(SpreadRate, 2)
```

- **Пустая клетка** — победитель с max `spreadScore` (weighted random среди прошедших `MinFitness`).
- **Занятая** ecology-клетка — challenger **вытесняет** incumbent, если  
  `spreadScore ≥ holdScore × DisplacementHoldMargin` (конфиг, default 1.25).
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
| `DisplacementHoldMargin` | 1.25 |
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

Остаётся проверить: покос → быстрые colonizers → вытеснение; symbiosis cascade; длинный aquatic playtest.

---

## 12. Ссылки

- Оригинал: https://mods.vintagestory.at/show/mod/53  
- Revival: https://mods.vintagestory.at/wildfarmingrevival  
- Прогресс: [`PROGRESS.md`](PROGRESS.md)  
- Промпт: [`PROMPT.md`](PROMPT.md)
