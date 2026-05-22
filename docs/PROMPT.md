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
- Среда из игры (климат, fertility, соседи) — не дублировать генерацию мира. Размножение и рост только где Suitability.Score >= порога; при провале блок не ставить.
- Цепочка игрока: семя → ювениль (wildplant) → ванильный блок. Экосистема дополняет мир редкими успешными reproduce.

Не расширять без явного запроса: living trees, vines, mushrooms, termites, WFGasHelper, Harmony TreeGen.

Новый код — в src/Ecosystem/ (EcosystemSystem, EnvironmentalContext, SuitabilityFromAttributes, интерфейсы из vision).

MVP: один вид цветка; рефактор WildPlant под suitability; IReproducible на взрослом блоке; конфиг Radius / Chance / MinFitness.

Порт: VS 1.21+, .NET 8. Legacy src/ — референс JakeCool19 v1.2.0, не мержить wildfarmingrevival.

Стадия репо: pre-MVP, папки Ecosystem/ ещё нет. Приоритет у docs/PROJECT_VISION.md над поведением Revival.

Коммиты — только по запросу пользователя.
```

---

## One-liner

Экосистемный мод VS: участники через interests/capabilities; живое = размножается; среда из игры; размещение только на подходящих клетках; MVP — один вид цветка без living trees; код оригинала — референс, не копировать Revival целиком.
