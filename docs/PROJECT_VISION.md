# Wild Farming / Ecosystem Mod — видение проекта

Документ для разработчиков и AI-агентов: **теория**, **целевая архитектура**, **отношение к оригинальному Wild Farming и Revival**, **текущая стадия репозитория**.

Последнее обновление: 2026-05-23 (стадия: MVP-alpha).

---

## 1. Зачем этот проект

Цель — не клон [Wild Farming - Revival](https://mods.vintagestory.at/wildfarmingrevival), а **узкая экосистемная прослойка** для Vintage Story:

- объекты **включены в мир** (климат, почва, соседи), а не живут как изолированные мини-игры;
- **живой** объект обязан **размножаться**; без размножения это декор или культура игрока, не член экосистемы;
- взаимодействие через **интерфейсы (capabilities / interests)**, а не через жёсткие ссылки на классы вроде `BlockEntityTrunk`;
- размножение **только на пригодных клетках** → читаемая «история» на ландшафте (поляны у воды, границы биомов), без отдельного worldgen.

Использовать наработки JakeCool19 / Revival **как идеи и паттерны**, не переносить весь feature set.

---

## 2. Принципы дизайна

### 2.1. Объект не самостоятельный

Сущность не симулирует весь мир внутри себя. Она:

- **предоставляет** capabilities (что даёт соседям и среде);
- **объявляет** interests (что ищет у среды и соседей);
- при взаимодействии другие участники смотрят на **наличие интерфейсов**, а не на конкретный тип блока.

### 2.2. Живость = размножение

| Статус | Критерий |
|--------|----------|
| Живой участник экосистемы | Реализует `IReproducible` (или эквивалент) — периодические попытки дать потомство |
| Не живой | Только рост по таймеру, декор, предмет игрока — без экосистемного размножения |

Рост «семя → взрослый» может быть у обоих; **маркер жизни — именно reproduce**.

### 2.3. Среда уже есть в игре — не дублировать

Климат, fertility, влажность worldgen, сезон — брать из API Vintage Story (`GetClimateAt`, атрибуты блока почвы и т.д.). Мод **не подменяет** игру на этапе посадки; он реагирует на среду **после** действия игрока или при диком размножении.

### 2.3.1. Ванильные блоки, без mod-артефактов в мире

| Принцип | Реализация |
|---------|------------|
| **Объект в мире** | Только `game:flower-*` (и позже другие `game:*` по списку). Никаких `wildfarming:wildplant-*` в дикой экосистеме. |
| **Мод при снятии** | Патч добавляет `entityClass: EcosystemPlant` к ванильному `flower.json`. Без мода — снова обычный цветок, блоки в сохранении не ломаются. |
| **Семена мода** | Не часть дикой экосистемы; культивация игроком — ванильные/игровые семена по желанию. |
| **Дикое размножение** | `EcosystemPlant` BE на сервере регистрирует `game:flower` в реестре; потомство — соседние клетки с тем же `game:flower-*`, если `Score >= MinFitness`. |

«Как отслеживать» — при загрузке чанка у каждого ванильного цветка с BE вызывается регистрация (без всплеска посадки при загрузке). Размножение по таймеру `EcosystemSystem`.

### 2.4. Минимум механик

Не тащить из Revival/оригинала:

- «живые деревья» (здоровье ствола, листва, POI, термиты как подсистема);
- лианы, Gas API helper, десятки независимых тогглов;
- Harmony-патчи генератора деревьев — если propagation идёт через экосистемный сервис.

Брать:

- цепочка **семя → промежуточный блок → ванильный блок**;
- **климат / теплица** замедляют или останавливают созревание;
- **размножение только при прохождении suitability** (идея `TreeFriend.TryToPlant`);
- **данные в JSON-атрибутах**, логика в общих сервисах.

---

## 3. Целевая архитектура

### 3.1. Схема

```
[Климат, почва, вода, соседи]  →  EnvironmentalContext (снимок в BlockPos)
                                        ↓
Участник (BE / Block behavior)  →  interests + capabilities
                                        ↓
                         EcosystemSystem (ModSystem)
                           • Match interests ↔ capabilities
                           • Suitability.Score(context) → 0..1
                           • IReproducible.TryReproduce → блок только если score ≥ threshold
```

### 3.2. Минимальные контракты (C#)

Не раздувать: **4–5 интерфейсов** + композиция.

| Интерфейс | Назначение |
|-----------|------------|
| `IEcosystemParticipant` | Участник в точке мира: списки interests и capabilities |
| `IEnvironmentalContext` | Readonly снимок среды в `BlockPos` (климат, fertility, блок снизу, ключевые соседи) |
| `ISuitability` | `float Score(IEnvironmentalContext)` — можно ли здесь жить / размножаться |
| `IReproducible` | `bool TryReproduce(...)` — **единственный обязательный маркер «живости»** для экосистемы |
| `IGrowthStage` (опционально) | Семя → ювениль → взрослый (аналог `wildplant` → `game:*`) |

**Interests (примеры):** `TempRange`, `MinFertility`, `RequiresSolidGround`, `MaxLight`, `NearWater`, `HostTreeType`.

**Capabilities (примеры):** `Shade`, `MoistureRetention`, `OrganicMatter`, `SeedDispersal`, `IHost` (для будущих деревьев).

Соседний код не знает `BlockEntityTrunk` — только «есть ли capability X в радиусе R».

### 3.3. Естественная «история» на карте

1. Раз в N игровых часов у `IReproducible` — попытка размножения.
2. Кандидаты — клетки в радиусе (позже: склон, ветер, вода).
3. Для каждой клетки: `Score(context) >= MinFitness`.
4. Успех → ставится **ювенильная** стадия (`wildplant` или саженец), не сразу взрослый блок.
5. Провал → **ничего не ставить** (без призрачных блоков).

Игрок остаётся внешним агентом (сажает семена); экосистема **дополняет** мир без обязательного микроменеджмента.

### 3.4. Планируемая структура кода (цель)

```
src/
  Ecosystem/
    IEcosystemParticipant.cs
    IEnvironmentalContext.cs
    ISuitability.cs
    IReproducible.cs
    EnvironmentalContext.cs      # сбор снимка из API
    SuitabilityFromAttributes.cs # interests из block attributes
    EcosystemSystem.cs           # ModSystem: тики, регистрация
  BlockEntity/
    WildPlant.cs                 # рефактор под ISuitability
  Item/
    WildSeed.cs
  ... (остальное — по мере необходимости MVP)
```

Папка `Ecosystem/` реализована — актуальный прогресс: **[`PROGRESS.md`](PROGRESS.md)**.

---

## 4. MVP (первая вертикаль)

Один вид растения (например, один дикий цветок), без живых деревьев.

| Шаг | Содержание |
|-----|------------|
| 1 | `EcosystemSystem` + `EnvironmentalContext` + `SuitabilityFromAttributes` |
| 2 | Рефактор `WildPlantBlockEntity`: созревание через suitability, без дублирования климата |
| 3 | На **взрослом** блоке (patch + BE или block behavior): `IReproducible`, 1–2 попытки/день, низкий шанс |
| 4 | Конфиг: `Radius`, `Chance`, `MinFitness` — три параметра, не страница тогглов Revival |

Критерий готовности MVP:

| Критерий | Статус |
|----------|--------|
| Посадка → рост → взрослый цветок | ✅ |
| Reproduce catmint | ✅ playtest |
| Расширение на другие виды | ⏳ |

---

## 5. Сравнение с Wild Farming (оригинал) и Revival

### 5.1. Оригинал (этот репозиторий, v1.2.0, ~2022)

| | |
|---|---|
| **modid** | `wildfarming` |
| **Исходники** | Да, `src/`, .NET Framework 4.6.1 |
| **Точка входа** | `src/WF.cs` — `WildFarming` ModSystem |
| **Конфиг** | `WildFarmingConfig.json` / `BotanyConfig.cs` |
| **Состояние** | Не собирается под современный VS без портирования |

Ключевые классы для изучения (не копировать целиком):

- `WildPlantBlockEntity` — таймер, климат, теплица, замена на `game:*`
- `WildSeed` — посадка `wildplant-*`
- `TreeFriend` — посадка рядом с деревом (обобщить в propagation)
- `BlockEntityRegenSapling`, `BlockEntityTrunk` — **не входят в MVP**
- `Patches.cs` — Harmony на `TreeGen` — **избегать**, если не необходимо
- `WFGasHelper` — **не переносить**

### 5.2. Wild Farming Revival (modid `wildfarmingrevival`)

- Форк для VS 1.19–1.21+, .NET 8, только DLL в релизе.
- Тот же namespace `WildFarming` внутри DLL; ассеты с доменом `wildfarmingrevival:`.
- **Не ставить вместе с оригиналом.**
- По умолчанию выключены: living trees, vines; добавлены seed bags, Config Lib GUI, `ResinGrowth`, `PropogateIntoWater/Claims`, Tall Fern, Silver Torch Cactus.
- Удалён `WFGasHelper`.

При работе в этом репозитории Revival — **референс поведения и баланса**, не источник для merge без лицензии/авторства.

---

## 6. Текущая стадия репозитория

**Стадия: `MVP-beta`** — catmint playtest OK. Детали: **[`PROGRESS.md`](PROGRESS.md)**.

| Компонент | Статус |
|-----------|--------|
| Экосистемное ядро | ✅ |
| Catmint: посадка / рост / reproduce | ✅ playtest |
| Reproduce forgetmenot, cornflower, wilddaisy | ✅ в JSON, playtest ⏳ |
| Рецепты семян (нож + цветок) | ✅ исправлен `{flower}-free` |
| Visual Studio / net10 | ✅ |
| Legacy | ⏸ не в сборке |

### 6.1. Структура репозитория

```
src/
  Ecosystem/          # ModSystem, suitability, config
  BlockEntity/WildPlant.cs
  Item/WildSeed.cs
  WF.cs               # регистрация wildseed + WildPlant
  (legacy…)           # не компилируется
assets/wildfarming/   # JSON, рецепты, patches
docs/                 # vision, prompt, VISUAL_STUDIO.md
wildfarming.sln
```

- `modinfo.json` — `2.0.0-mvp`, game `1.21.0`
- Конфиг: `wildfarming-ecosystem.json` (не legacy `WildFarmingConfig.json`)

### 6.2. Временные артефакты (можно удалять)

При анализе могли появиться `_wfr_extract/`, `_wfr_temp.zip` — распакованный Revival 1.4.3 для сравнения; **не часть продукта**.

---

## 7. Правила для агентов

1. **Не расширять** living trees / vines / mushrooms / termites без явного запроса пользователя.
2. **Новая логика** — в `Ecosystem/` и через интерфейсы; не плодить монолитные `BlockEntity*` на каждую фичу.
3. **JSON attributes** — декларация interests; C# — интерпретация, не хардкод списков видов в коде.
4. **Размножение** — только после `Suitability.Score >= MinFitness`; не ставить блоки «на удачу». **Не** блокировать посадку игроком.
5. **Выживание** — климат после посадки; при длительном несоответствии — смерть, не silent deny.
6. Портирование API — **Vintage Story 1.21+**, **.NET 10** (как у установленной игры).
7. При сомнении между «как в Revival» и «как в §2–3 этого документа» — приоритет у **этого документа**.
8. Коммиты — только по запросу пользователя.

---

## 8. Открытые решения (TBD)

- [ ] `modid` оставить `wildfarming` или новый (например `wildfarmingeco`) при портировании
- [ ] Block behavior vs BlockEntity для `IReproducible` на взрослых ванильных блоках
- [ ] Частота тиков размножения и производительность на больших серверах
- [ ] Интеграция с land claims (`PropogateIntoClaims` из Revival — опционально, по умолчанию false)
- [ ] Совместимость с модом Wildcraft (отключение дублирующих кустов) — только если вернём bush seeds

---

## 9. Ссылки

- Оригинал на Mod DB: https://mods.vintagestory.at/show/mod/53  
- Revival: https://mods.vintagestory.at/wildfarmingrevival  
- Локальный README по атрибутам растений: `README.MD`  
- Исходная точка входа legacy: `src/WF.cs`

---

## 10. Промпт для агентов

Готовый copy-paste блок и one-liner: **[`PROMPT.md`](PROMPT.md)**.
