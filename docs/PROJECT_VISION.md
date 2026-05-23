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

**Стадия: `Ecosystem v1.1`.** Следующая (опционально): **Content** (fern, …).

| Компонент | Статус |
|-----------|--------|
| Экосистема на цветах + люпине | ✅ |
| Водная флора (4 вида) | ✅ код |
| Rain/forest + candidate pool + spacing | ✅ |
| Legacy в сборке | ⏸ |

- `modinfo.json` — `2.2.0-ecosystem-v1.1`, game `1.21.0`
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

## 9. Открытые решения (TBD)

- [ ] `modid` оставить `wildfarming` или переименовать при публикации
- [ ] Убрать `EcosystemPlant` BE, оставить только chunk-scan
- [ ] Land claims при reproduce
- [ ] Папоротники как второй тип участника

---

## 10. Ссылки

- Оригинал: https://mods.vintagestory.at/show/mod/53  
- Revival: https://mods.vintagestory.at/wildfarmingrevival  
- Прогресс: [`PROGRESS.md`](PROGRESS.md)  
- Промпт: [`PROMPT.md`](PROMPT.md)
