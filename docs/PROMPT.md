# Agent prompt — Wild Farming / Ecosystem

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md). Чеклист: [`PROGRESS.md`](PROGRESS.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка на ванильных блоках (цветы + водная флора), не клон Wild Farming Revival.

Принципы:
- В мире только ванильные блоки; мод не подменяет блоки wildplant.
- Живое = зарегистрировано в EcosystemSystem и периодически spread (IEcosystemParticipant / EcosystemParticipant).
- Среда из API: EnvironmentalContext (температура, WorldgenRainfall, ForestDensity, почва, жидкость).
- Spread только на клетки с fitness >= MinFitness; кандидаты — все свободные в радиусе, выбор weighted.
- Скорость spread: SpreadRate per-species (SpeciesSpread × конфиг).

Habitat:
- Terrestrial — game:flower-*, flower-lupine
- ReedNearWater — coopersreed, papyrus: muddygravel; мелководье = ровно 1 водный блок над илом, рогоз ВНУТРИ него (water-normal)
- WaterSurface — waterlily
- UnderwaterColumn — aquatic-watercrowfoot (колонка section → tip/top)

Не расширять без явного запроса: living trees, vines, mushrooms, Harmony, legacy wildplant/WildSeed.

Код: src/Ecosystem/, BlockEntity/EcosystemPlant.cs, assets/wildfarming/patches/enabledpatches.json.

Порт: VS 1.21+, .NET 10. Стадия: Ecosystem v1.1 — см. docs/PROGRESS.md (Roadmap / TODO).

Ближайший backlog: tallgrass в экосистему; патч drygrass с цветов (нож); пресеты баланса. Mod DB — позже.

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla flowers + aquatic plants (reeds on muddygravel, crowfoot columns), worldgen climate, candidate-pool spread; not Revival.
