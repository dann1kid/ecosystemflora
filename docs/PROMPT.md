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
- v3.6: **Wild tree maturation** — calendar age (persisted), grown-block growth, **phased senescence** (leaves → skeleton → snag → stump/logs); inspect (I); `EnableTreeSenescenceRemains`, `TreeSenescenceFallenLogCount`; docs [`TREE_AGING.md`](TREE_AGING.md).
- v3.7: **Tree fern** (`ferntree-normal-*`) — register, spread, aging, senescence — [`FERNTREE.md`](FERNTREE.md). **Canopy** — partial branchy strip, fallen sticks, spring branchy × tree age — [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md). **Wild vines** — tip spread down + wall capture — [`WILD_VINE.md`](WILD_VINE.md).
- v3.8: **Phase 6** — chunk-fair spread, event wake, two-phase placement (terrestrial/mat only; mycelium/vine direct), season coarse wake; registration priority/burst, background column scan (`BackgroundRegistrationScanner`, `PendingRegistrationQueue`), foliage sync decoupled (`FoliageChunkSyncPass`); **vines** — column pass; **mycelium anchors** — `MyceliumChunkRegistrar` at chunk load; desynced ticks (2000/2300/5500 ms); `LimitSpreadNearPlayers` limits spread/stress/tree aging (not registration) — [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md).
- Aquatic v3.1.3–6: reeds = RhizomeMat (edge + seed); lily = SurfaceMat; crowfoot = independent (см. GAPS).

Habitat:
- Terrestrial — flowers, tallgrass, fern, berries
- TerrestrialTree — log-grown → sapling
- Ferntree — ferntree-normal-trunk → young column
- WildVine — wildvine-end-* tips
- ReedNearWater — coopersreed, tule, papyrus (rhizome mat default)
- WaterSurface — waterlily (surface mat default)
- UnderwaterColumn — watercrowfoot

Не расширять без явного запроса: living trees, Harmony, legacy wildplant, termites. Mycelium — только vanilla BE (v3.1.12). Ferntree/vines — vanilla blocks (v3.7), playtest before tuning.

Код: src/Ecosystem/ (в т.ч. LegacyBlockEntityMigration.cs; Mycelium*.cs; Phase 6 — SpreadChunkScheduler, PendingSpreadQueue, BackgroundRegistrationPipeline, RegistrationScanQueue, PendingRegistrationQueue, FoliageCellScheduler).
Тесты: 332 (xUnit). Версия: 3.8.0.

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
