# Agent prompt — Ecosystem - Flora

Скопируй блок ниже в системный промпт, @-упоминание или задачу агенту. Полная теория: [`PROJECT_VISION.md`](PROJECT_VISION.md). Чеклист: [`PROGRESS.md`](PROGRESS.md). Пробелы: [`GAPS.md`](GAPS.md).

---

## Copy-paste prompt

```
Ты работаешь над модом Vintage Story в репозитории vs-wildfarming.

Цель: экосистемная прослойка на ванильных блоках (цветы, трава, папоротники, ягоды, деревья, водная флора), не клон Wild Farming Revival. Название мода: Ecosystem - Flora.

Принципы:
- В мире ванильные блоки родителей; сторонние blocktypes — JSON `ecologyParticipant` (`docs/THIRD_PARTY_ECOLOGY.md`).
- Живое = зарегистрировано в EcosystemSystem и периодически spread.
- Среда: EnvironmentalContext (температура, WorldgenRainfall, LocalForestCover, почва, жидкость, ниша).
- v2.1: spread + displacement + stress + symbiosis. БЕЗ DisturbedTracker.
- v2.2–2.3: niche, soil succession, WildSpeciesSeason.
- v2.11: Ecology inspect (I) — mat edge, seed %, stress, season (InspectLineLite, i18n на клиенте).
- v3.1.7: meadow harvest — пустая рука → блок цветка/tallgrass; нож/коса → drygrass (`PlantHandHarvest`, `EnableFlowerDrygrass`).
- v3.1.8: legacy BE migration (`LegacyBlockEntityMigration`); fix ecology inspect dialog (I).
- v3.1.9: spread targets air only (no torch/loosestone overwrite); `SoilSuccessionGuard` protects `terrainslabs:*` ground.
- v3.1.10: meadow harvest (flowers → world drop, tallgrass clear); fix inspect (I) dialog; client config load.
- v3.1.11: tree spread — ice/snow footing rejected; TerrestrialTree uses terrestrial preflight; winter spread mult = 0.
- v3.1.12: mycelium — soft niche (`MyceliumZone`), stress/death on vanilla BE, network spread, inspect (I) on cap + soil; meadow coexistence; config auto-merge (`StoreModConfig` after load).
- v3.2.0: **Canopy phenology** — deciduous partial autumn defol + spring bud on log-grown skeleton; in-RAM CA; `EnableSeasonalFoliage`; docs [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md).
- v3.5.0: **Canopy ambience** — client crown particles / flutter; docs [`CANOPY_AMBIENCE.md`](CANOPY_AMBIENCE.md).
- v3.6: **Wild tree maturation** — calendar age (persisted across saves/restart), grown-block growth, **senescence = full tree removal** (`EnableTreeSenescence`); inspect (I) on trunk; `OnlyActivateNearPlayers` default **false** (all loaded chunks); docs [`TREE_AGING.md`](TREE_AGING.md).
- Aquatic v3.1.3–6: reeds = RhizomeMat (edge + seed); lily = SurfaceMat; crowfoot = independent (см. GAPS).

Habitat:
- Terrestrial — flowers, tallgrass, fern, berries
- TerrestrialTree — log-grown → sapling
- ReedNearWater — coopersreed, tule, papyrus (rhizome mat default)
- WaterSurface — waterlily (surface mat default)
- UnderwaterColumn — watercrowfoot

Не расширять без явного запроса: living trees, vines, Harmony, legacy wildplant. Mycelium — только vanilla BE (v3.1.12), без своих блоков грибов.

Код: src/Ecosystem/ (в т.ч. LegacyBlockEntityMigration.cs для старых BE; Mycelium*.cs для грибницы).
Тесты: 242 (xUnit). Версия: 3.5.0.

v3.0: CloneBerryTraits, BerryTraitMutationChance.
v3.1: EnableThirdPartyParticipants, ecologySpreadMode (rhizome/surfacemat/independent).
v3.1.2: soil succession balance, SoilSuccessionSkipWhenBuiltAbove.
v3.1.3–6: UseRhizomeSpreadForReeds, UseSurfaceMatSpreadForLilies, RhizomeSeedDispersal*.
v3.1.7: EnableFlowerDrygrass, PlantHandHarvest (DidBreakBlock), scythe patch flower-.

Валидация баланса пользователем: логи (VerboseLogging + ReproduceDebug) + осмотр (I).

Backlog: см. docs/GAPS.md (crowfoot, de handbook, dominant UX, fauna companion).

Коммиты — только по запросу пользователя.
```

---

## One-liner

VS ecosystem mod: vanilla plants, seasonal spread, cell competition, mat spread for aquatic, niche, soil succession; inspect (I) for debugging; not Revival.
