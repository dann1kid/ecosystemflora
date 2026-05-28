# Agent prompt — Ecosystem - Flora

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md). Чеклист: [`PROGRESS.md`](PROGRESS.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка на ванильных блоках (цветы, трава, папоротники, ягоды, деревья, водная флора), не клон Wild Farming Revival. Название мода: Ecosystem - Flora.

Принципы:
- В мире ванильные блоки родителей; дополнительно сторонние blocktypes могут входить через JSON `ecologyParticipant` (`docs/THIRD_PARTY_ECOLOGY.md`); мод не подменяет wildplant для ванили.
- Живое = зарегистрировано в EcosystemSystem и периодически spread (IEcosystemParticipant / EcosystemParticipant).
- Среда из API: EnvironmentalContext (температура, WorldgenRainfall, LocalForestCover, почва, жидкость, ниша).
- v2.1: единая конкуренция за клетку — spread на пустые + displacement занятых (CellCompetition), stress death, symbiosis. БЕЗ DisturbedTracker.
- v2.2: ниша — moisture/light (NicheSampler, WildSpeciesNiche), сукцессия почвы (block-only).
- v2.3: сезонность — WildSpeciesSeason (12 месяцев), SeasonEcology; интерполяция spread по году.
- v2.9: залежь (FallowRestoration), spread на пустую пашню, player-placed register.
- v2.10: PlantVacancyRules, mycelium только active BE; displacement/hold tuning.
- v2.11.x: Ecology inspect (хоткей I, protobuf канал); chunk-scan с курсором до конца чанка; отчёт на клиенте — `InspectLineLite`, `ErrorLangKey`; имена видов — ключи `ecosystemflora:species-{id}` в `assets/ecosystemflora/lang/` (en/ru/de).
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
Тесты: tests/WildFarming.Tests.csproj (xUnit, 99 тестов).

- v3.0: berry spread clones parent fruit traits (`CloneBerryTraits`, `BerrySpreadTraitCloner` → vanilla `BEBehaviorFruitingBush.OnGrownFromCutting`).
- v3.1: third-party blocks declare `ecologyParticipant` / `ecologySpecies` / `ecologySpreadBlock` / `ecologyHabitat` on blocktype JSON; `EnableThirdPartyParticipants` (see `docs/THIRD_PARTY_ECOLOGY.md`).

Порт: VS 1.22+, .NET 10. Версия: 3.1.0. Стадия: Ecosystem v3.1 — см. docs/PROGRESS.md.

Backlog: dominant species UX; aquatic edge cases; выпас/tallgrass-eaten (husbandry). Листва зимой — отложено (визуал).

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla plants, seasonal spread, unified cell competition (spread + displace + stress + symbiosis), niche, soil succession, flora context; not Revival.
