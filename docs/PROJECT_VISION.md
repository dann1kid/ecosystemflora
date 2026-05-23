# Wild Farming / Ecosystem Mod — видение проекта

Документ для разработчиков и AI-агентов: **теория**, **целевая архитектура**, **отношение к оригинальному Wild Farming и Revival**, **текущая стадия репозитория**.

Последнее обновление: 2026-05-22 (стадия: **Ecosystem v1** — завершён).

---

## 1. Зачем этот проект

Цель — не клон [Wild Farming - Revival](https://mods.vintagestory.at/wildfarmingrevival), а **узкая экосистемная прослойка** для Vintage Story:

- объекты **включены в мир** (климат, почва, соседи), а не живут как изолированные мини-игры;
- **живой** объект обязан **размножаться**; без размножения это декор, не член экосистемы;
- взаимодействие через **интерфейсы (capabilities / interests)** — целевая модель, пока упрощённая реализация на цветах;
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
| **Объект в мире** | Только `game:flower-*` (20 видов × `free`/`snow`) |
| **Мод при снятии** | Патч добавляет `entityClass: EcosystemPlant`; без мода — обычный `flower.json`, сохранения целы |
| **Дикое размножение** | Потомство = тот же `game:flower-*` на соседней подходящей клетке |
| **Семена / wildplant** | **Не** часть дикой экосистемы; не в сборке MVP-vanilla-flowers |
| **Культивация игроком** | Ванильные механики игры; мод не обязан давать mod-семена |

**Регистрация:** при загрузке чанка (очередь, лимиты) + `EcosystemPlant` BE при появлении блока. **Размножение:** round-robin по реестру, лимит попыток за тик.

### 2.4. Минимум механик (не тащить из Revival)

- living trees, лианы, Gas API, Harmony worldgen, термиты;
- страницы тогглов и mod-артефакты в сохранении.

**Берём:** suitability по климату/почве/воде/торфу, rain/forest из worldgen-карт, размножение с `MinFitness`, скорость spread per-species.

---

## 3. Целевая архитектура

### 3.1. Схема (цель)

```
[Климат, почва, вода]  →  EnvironmentalContext
                                ↓
Участник              →  interests (целевое: IEcosystemParticipant)
                                ↓
EcosystemSystem       →  Score >= MinFitness → SetBlock(тот же game:flower)
```

### 3.2. Минимальные контракты (C#) — целевые

| Интерфейс | Назначение | Статус |
|-----------|------------|--------|
| `IEnvironmentalContext` | Снимок среды | ✅ |
| `IReproducible` / `ReproducerEntry` | Маркер живости + таймер | ✅ упрощённо |
| `ISuitability` / `SuitabilityEvaluator` | Оценка клетки | ✅ температура, rain/forest, почва, вода |
| `IEcosystemParticipant` | Interests + capabilities | ✅ `FlowerEcosystemParticipant` |
| `IGrowthStage` | Семя → ювениль → взрослый | ⏸ не для дикой природы |

### 3.3. Естественная «история» на карте (реализовано)

1. Ванильный цветок в мире → регистрация в реестре (`FlowerEcosystemParticipant`).
2. Периодически (интервал и шанс зависят от `SpreadRate` вида) — попытка spread.
3. Скан всех свободных клеток в радиусе; фильтр rain/forest/почва/жидкость; `fitness >= MinFitness`.
4. Случайный выбор среди кандидатов с весом по fitness → `game:flower-*` на клетке.
5. Неподходящая почва (торф и т.д.) или климат — spread не происходит (читаемо в debug).

### 3.4. Структура кода (фактическая)

```
src/
  WF.cs                          # ModSystem, server-only ecosystem init
  Ecosystem/
    EcosystemSystem.cs           # реестр, тики, chunk queue
    ReproducerRegistry.cs
    ChunkFlowerScanner.cs
    ReproducePlacement.cs
    SurfacePlacement.cs
    BlockFluidHelper.cs
    EnvironmentalContext.cs
    SuitabilityEvaluator.cs
    PlantRequirements.cs
    WildFlowerClimate.cs         # temp + rain/forest + SpreadRate
    SpeciesSpread.cs
    FlowerEcosystemParticipant.cs
    IEcosystemParticipant.cs
    EcologyFlowerSpecies.cs
    EcologyAttributes.cs
    PlantCodeHelper.cs
    EcosystemConfig.cs
  BlockEntity/
    EcosystemPlant.cs            # BE на game:flower (патч)
  (legacy WildPlant, WildSeed…)  # не в csproj
assets/wildfarming/
  patches/enabledpatches.json    # entityClass на flower.json
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

## 5. Ecosystem v1 (завершён)

| Критерий | Статус |
|----------|--------|
| `IEcosystemParticipant` | ✅ |
| `minRain` / `minForest` (worldgen maps) | ✅ |
| Выбор из пула свободных клеток | ✅ |
| `SpreadRate` per-species | ✅ |
| Playtest (почва, торф, разная скорость) | ✅ |

---

## 6. Сравнение с оригиналом и Revival

### 6.1. Оригинал (репозиторий, v1.2.0)

- `wildplant` → `game:*`, mod-семена, living trees — **архив**, не в сборке.
- Полезны как референс: `TreeFriend`, `WildPlant` (таймер, климат).

### 6.2. Revival

- Референс баланса и UX; **не** merge и не co-load.
- Наш modid остаётся `wildfarming`, другая архитектура.

---

## 7. Текущая стадия репозитория

**Стадия: `Ecosystem v1` — завершён.** Следующая (опционально): **Content** (fern, …).

| Компонент | Статус |
|-----------|--------|
| Экосистема на `game:flower-*` | ✅ |
| 20 видов, климат + spread rate | ✅ |
| Rain/forest + candidate pool | ✅ |
| Legacy в сборке | ⏸ |

- `modinfo.json` — `2.1.0-ecosystem-v1`, game `1.21.0`
- Конфиг: `wildfarming-ecosystem.json`

---

## 8. Правила для агентов

1. **Не расширять** trees / vines / mushrooms без явного запроса.
2. **Дикая экосистема** — только ванильные блоки в мире; не возвращать `wildplant` без запроса.
3. **Размножение** — только `Score >= MinFitness`; не ставить блоки «на удачу».
4. **Производительность** — не сканировать весь мир за тик; очереди и лимиты; не трогать блоки из фоновых потоков.
5. API: **VS 1.21+**, **.NET 10**.
6. Приоритет у этого документа над «как в Revival».
7. Коммиты — только по запросу пользователя.

---

## 9. Открытые решения (TBD)

- [ ] `modid` оставить `wildfarming` или переименовать при публикации
- [ ] Убрать `EcosystemPlant` BE, оставить только chunk-scan (производительность)
- [ ] Land claims при reproduce (по умолчанию false)
- [ ] Папоротники как второй тип участника

---

## 10. Ссылки

- Оригинал: https://mods.vintagestory.at/show/mod/53  
- Revival: https://mods.vintagestory.at/wildfarmingrevival  
- Прогресс: [`PROGRESS.md`](PROGRESS.md)  
- Промпт: [`PROMPT.md`](PROMPT.md)
