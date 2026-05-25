# Agent prompt — Ecosystem - Flora

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md). Чеклист: [`PROGRESS.md`](PROGRESS.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка на ванильных блоках (цветы, трава, папоротники, ягоды, деревья, водная флора), не клон Wild Farming Revival. Название мода: Ecosystem - Flora.

Принципы:
- В мире только ванильные блоки; мод не подменяет блоки wildplant.
- Живое = зарегистрировано в EcosystemSystem и периодически spread (IEcosystemParticipant / EcosystemParticipant).
- Среда из API: EnvironmentalContext (температура, WorldgenRainfall, LocalForestCover, почва, жидкость, ниша).
- v2.1: единая конкуренция за клетку — spread на пустые + displacement занятых (CellCompetition), stress death, symbiosis. БЕЗ DisturbedTracker.
- v2.2: ниша — moisture/light (NicheSampler, WildSpeciesNiche), сукцессия почвы (block-only).
- v2.3: сезонность — WildSpeciesSeason, SeasonEcology; зимняя выживаемость, осеннее отмирание, весенний бонус.
- SpreadScore = fitness × Context × SpreadRate × SeasonMultiplier; HoldScore = fitness × Context × HoldStrength.
- Опушка emergent через FloraContext + displacement, не отдельный биом.

Habitat:
- Terrestrial — game:flower-*, flower-lupine, tallgrass, fern, berries
- TerrestrialTree — log-grown → sapling spread
- ReedNearWater — coopersreed, papyrus
- WaterSurface — waterlily
- UnderwaterColumn — aquatic-watercrowfoot

Не расширять без явного запроса: living trees, vines, mushrooms, Harmony, legacy wildplant/WildSeed.

Код: src/Ecosystem/, BlockEntity/EcoSystemLife.cs, assets/ecosystemflora/patches/enabledpatches.json.
Тесты: tests/WildFarming.Tests.csproj (xUnit, 46 тестов).

Порт: VS 1.21+, .NET 10. Версия: 2.5.0. Стадия: Ecosystem v2.3 — см. docs/PROGRESS.md.

Backlog (post-release): chunk-scan без EcoSystemLife BE; handbook/dominant species (UX); залежь; HoldStrength tuning. Листва зимой — отложено (визуал, не экосистема).

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla plants, seasonal spread, unified cell competition (spread + displace + stress + symbiosis), niche, soil succession, flora context; not Revival.
