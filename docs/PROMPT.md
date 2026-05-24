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
- v2.1: единая конкуренция за клетку — spread на пустые + displacement занятых (CellCompetition), stress death, symbiosis. БЕЗ DisturbedTracker.
- SpreadScore = fitness × Context × SpreadRate; HoldScore = fitness × Context × HoldStrength.
- Опушка emergent через FloraContext + displacement, не отдельный биом.

Habitat:
- Terrestrial — game:flower-*, flower-lupine, tallgrass, fern, berries
- ReedNearWater — coopersreed, papyrus
- WaterSurface — waterlily
- UnderwaterColumn — aquatic-watercrowfoot

Не расширять без явного запроса: living trees, vines, mushrooms, Harmony, legacy wildplant/WildSeed.

Код: src/Ecosystem/, BlockEntity/EcosystemPlant.cs, assets/wildfarming/patches/enabledpatches.json.

Порт: VS 1.21+, .NET 10. Стадия: Ecosystem v2.1 — см. docs/PROGRESS.md.

Backlog: playtest mowing/symbiosis; land claims; balance tuning; perf roadmap (§12 PROJECT_VISION). Mod DB — позже. v2.1 meadow playtest ✅ 2026-05-22.

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla plants, unified cell competition (spread + displace + stress + symbiosis), flora context at forest edge; not Revival.
