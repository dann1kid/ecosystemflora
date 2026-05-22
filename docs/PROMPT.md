# Agent prompt — Wild Farming / Ecosystem

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка, не клон Wild Farming Revival.

Принципы:
- Объекты не автономны: объявляют interests (что ищут) и capabilities (что дают); взаимодействие только через эти контракты, без жёстких ссылок на классы вроде BlockEntityTrunk.
- Живое = размножается (IReproducible). Рост по таймеру без reproduce — не «жизнь» экосистемы.
- Среда из игры — не дублировать worldgen. Игрок сажает где угодно (физика); выживание/смерть после посадки; дикое reproduce только при Score >= MinFitness (не блокировать посадку).
- Цепочка игрока: семя → ювениль (wildplant) → ванильный блок. Экосистема дополняет мир редкими успешными reproduce.

Не расширять без явного запроса: living trees, vines, mushrooms, termites, WFGasHelper, Harmony TreeGen.

Новый код — в src/Ecosystem/ (EcosystemSystem, EnvironmentalContext, SuitabilityFromAttributes, интерфейсы из vision).

MVP: один вид цветка; рефактор WildPlant под suitability; IReproducible на взрослом блоке; конфиг Radius / Chance / MinFitness.

Порт: VS 1.21+, .NET 8. Legacy src/ — референс JakeCool19 v1.2.0, не мержить wildfarmingrevival.

Стадия репо: MVP-alpha — см. docs/PROGRESS.md. Приоритет у docs/PROJECT_VISION.md над Revival.

Коммиты — только по запросу пользователя.
```

---

## One-liner

Экосистемный мод VS: interests/capabilities; живое = reproduce; игрок сажает везде — растение выживает или погибает; дикая природа = реестр взрослых + suitability; не клон Revival.
