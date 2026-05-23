# Agent prompt — Wild Farming / Ecosystem

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md). Чеклист: [`PROGRESS.md`](PROGRESS.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка на ванильных game:flower-*, не клон Wild Farming Revival.

Принципы:
- В мире только ванильные блоки (game:flower-*); мод не подменяет блоки wildplant.
- Живое = зарегистрировано в EcosystemSystem и периодически spread (IEcosystemParticipant / FlowerEcosystemParticipant).
- Среда из API: EnvironmentalContext (температура, WorldgenRainfall, ForestDensity, почва, жидкость).
- Spread только на клетки с fitness >= MinFitness; кандидаты — все свободные в радиусе, выбор weighted.
- Скорость spread: SpreadRate per-species (SpeciesSpread × конфиг).

Не расширять без явного запроса: living trees, vines, mushrooms, Harmony, legacy wildplant/WildSeed.

Код: src/Ecosystem/, BlockEntity/EcosystemPlant.cs, assets/wildfarming/patches/enabledpatches.json.

Порт: VS 1.21+, .NET 10. Стадия: Ecosystem v1 завершён — см. docs/PROGRESS.md.

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla flowers, worldgen climate maps, candidate-pool spread, per-species SpreadRate; not Revival.
