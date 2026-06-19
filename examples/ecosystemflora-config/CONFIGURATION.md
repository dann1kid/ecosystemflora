> **Public mirror** - synced by `tools/Sync-ConfigExamples.ps1`. Edit canonical `docs/CONFIGURATION.md` only.

# Configuration reference (`ecosystemflora.json`)

**File:** `ModConfig/ecosystemflora.json` (server; client when the file exists).  
**Template:** `ecosystemflora.example.json` in this folder (shipped in the mod as `assets/ecosystemflora/ecosystemflora.example.json`).

**Source of truth:** `src/Ecosystem/EcosystemConfig.cs` (defaults below match C# unless noted).

On load, **missing keys are added** with defaults and the file is rewritten (server always; client when the file already exists).

---

## Balance presets

`BalancePreset` is applied **on every server start** when set to `natural`, `lush`, or `sparse`. It overwrites:

`ReproduceAttemptsPerYear`, `ReproduceChance`, `MinFitness`, `DefaultSameSpeciesSpacing`, `DefaultOtherSpeciesSpacing`.

Use `"custom"` to keep your own spread values across restarts.

| Preset | Attempts/year | Chance | MinFitness | Spacing |
|--------|---------------|--------|------------|---------|
| `natural` (default) | 72 | 0.50 | 0.45 | 1 |
| `lush` | 120 | 0.65 | 0.35 | 1 |
| `sparse` | 36 | 0.30 | 0.60 | 2 |
| `custom` | — | — | — | — |

---

## Server quick recipes

| Goal | Settings |
|------|----------|
| Slower spread everywhere | `"BalancePreset": "sparse"` or `custom` + lower `ReproduceAttemptsPerYear` / `ReproduceChance` |
| No wild tree death | `"EnableTreeSenescence": false` (optionally `"EnableTreeAging": false` for no growth either) |
| Treehouse-friendly | `"EnableTreeSenescence": false`; only natural `log-grown` trunks are in ecology — player `log-placed` builds are not |
| Ecology only near players | `"OnlyActivateNearPlayers": true` (spread, stress, trees, **and** chunk scans) |
| Spread/stress/trees near players only | `"LimitSpreadNearPlayers": true` (registration scans **unchanged**) |
| Disable mod | `"EcosystemEnabled": false` |

Press **I** on plants / trunks for live diagnostics. In-game handbook: *Configuration Guide*.

---

## Key reference

Types: `bool`, `int`, `float`, `double`, `string`.

### Master & climate

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `BalancePreset` | string | `natural` | `natural` / `lush` / `sparse` / `custom` — see presets above |
| `EcosystemEnabled` | bool | true | Master switch for spread, competition, stress, most ecology ticks |
| `HarshWildPlants` | bool | true | Survival checks use species climate bounds |
| `ApplyWorldgenRainForest` | bool | true | Spread uses worldgen rain; forest cover uses **neighbor trees** (`LocalForestCover`), not worldgen forest map |

### Spread — core

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ReproduceRadius` | int | 4 | Horizontal spread search radius (blocks) |
| `ReproduceVerticalSearch` | int | 5 | Vertical search for surface placement |
| `ReproduceChance` | float | 0.5 | Base success gate per spread attempt |
| `MinFitness` | float | 0.45 | Minimum fitness to place offspring |
| `ReproduceIntervalHours` | double | 24 | Legacy: hours between attempts when `UseCalendarScaledSpread` is false |
| `ReproduceAttemptsPerYear` | double | 72 | Calendar mode: attempts per in-game year at species `SpreadRate` 1 |
| `UseCalendarScaledSpread` | bool | true | Scale intervals from `DaysPerYear` / `HoursPerDay` |
| `UseSpeciesSpreadRates` | bool | true | Per-species ecology `SpreadRate` scales interval and chance |
| `MinSpeciesReproduceIntervalDays` | double | 0 | Floor between attempts (calendar mode); 0 = none |
| `MinSpeciesReproduceIntervalHours` | double | 0 | Floor between attempts (legacy mode only) |
| `MaxFailedSurvivalChecks` | int | 5 | Failed survival checks before stress removal |
| `GrowthHoursMultiplier` | float | 1 | **Not wired in code** — reserved; vanilla sapling growth unchanged |
| `StaggerReproduceAttempts` | bool | true | Random initial delay on registration to spread tick load |

### Spread — aquatic mats

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseRhizomeSpreadForReeds` | bool | true | Cattail / tule / papyrus: mat-edge spread; false = legacy radius spread |
| `RhizomeSeedDispersalEnabled` | bool | true | Rare virtual seed/fragment jumps for reed & lily mats |
| `RhizomeSeedDispersalChanceScale` | float | 1 | Multiplier on per-species seed jump chance |
| `RhizomeSeedDispersalFitnessScale` | float | 0.25 | Fitness scale for distant seed landing sites |
| `UseSurfaceMatSpreadForLilies` | bool | true | Water lily: floating pad mat; false = legacy radius spread |

### Spacing

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `PlantSpacingEnabled` | bool | true | Enforce Chebyshev spacing between spread plants |
| `ApplyCrossHabitatSpacing` | bool | false | When false, terrestrial vs aquatic spacing is not cross-checked |
| `DefaultSameSpeciesSpacing` | int | 1 | Used when species table has `SameSpeciesSpacing` 0 |
| `DefaultOtherSpeciesSpacing` | int | 1 | Used when species table has `OtherSpeciesSpacing` 0 |
| `SpacingVerticalSearch` | int | 2 | ±Y when scanning for spacing conflicts |

### Competition & displacement

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseCellDisplacement` | bool | true | Stronger species can displace weaker occupants |
| `DisplacementHoldMargin` | float | 1.18 | Challenger score must exceed incumbent hold × this |
| `PreferSpreadToEmptyCells` | bool | true | Weight empty cells when mixed with displacement candidates |
| `EnableEmptyFirstSpreadCollect` | bool | true | Collect empty-cell candidates before displacement pass |
| `EnableSpreadColumnOccupancyHint` | bool | true | Skip columns known occupied (spacing index) on empty-first pass |
| `EmptySpreadFitnessMultiplier` | float | 2.5 | Fitness multiplier for empty cells when `PreferSpreadToEmptyCells` |

### Flora context (forest edge / interior)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseFloraContext` | bool | true | Neighbor tree/log/leaves context affects spread fitness |
| `FloraContextNeighborRadius` | int | 2 | Horizontal radius for forest neighbor count |
| `FloraContextInteriorThreshold` | int | 4 | Neighbors ≥ this → forest interior |
| `FloraOpenInteriorPenalty` | float | 0.35 | Open-field species penalty in forest interior |
| `FloraContextCacheHours` | double | 12 | Cache TTL for local forest cover |

### Niche (local moisture & light)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseNicheContext` | bool | true | Local moisture/light niche multipliers on spread fitness |
| `NicheCacheHours` | double | 12 | Cache TTL per cell |
| `NicheStressThreshold` | float | 0.45 | Niche multiplier below this counts as failed survival |

### Stress, symbiosis, seasons

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableStressDeath` | bool | true | Remove plants after repeated failed survival checks |
| `StressRecheckHours` | double | 18 | Hours between stress evaluations per plant |
| `MaxStressChecksPerTick` | int | 16 | Stress evaluations per stress tick |
| `EnableSymbiosis` | bool | true | Forest symbionts need tree hosts; cascade on host loss |
| `SymbiosisCascadeRadius` | int | 4 | Radius when host tree removed |
| `UseSeasonalEcology` | bool | true | Monthly spread multipliers from `WildSpeciesSeason` |
| `SeasonalStressEnabled` | bool | true | Seasonal stress die-off rolls (terrestrial) |

### Soil succession & farmland

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `UseSoilSuccession` | bool | true | Spread/death can change soil tier blocks |
| `SoilSuccessionStrength` | float | 1 | Scale all succession deltas |
| `SoilSuccessionSkipWhenBuiltAbove` | bool | true | Skip soil swaps when slabs/builds occupy column above ground |
| `UseFarmlandNutrientBridge` | bool | true | Till adds N/P/K from dominant wild plant role |
| `FarmlandNutrientBridgeStrength` | float | 1 | Scale till nutrient bonus |
| `EnableFallowRestoration` | bool | true | Empty farmland near wild plants slowly regains nutrients |
| `FallowRestorationStrength` | float | 1 | Scale fallow restoration |

### Land claims

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `RespectLandClaims` | bool | true | Block spread, displacement, stress, tree growth/senescence in claims |

### Wild trees — discovery & saplings

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MaxPendingTreeChecksPerTick` | int | 12 | Mod-placed saplings polled until `log-grown` appears |
| `EnableCyclicTreeDiscovery` | bool | true | Round-robin scan for new `log-grown` trunks after load |
| `MaxTreeRescanColumnsPerTick` | int | 16 | Columns scanned per tick for cyclic discovery |

### Wild trees — aging & senescence

See [`TREE_AGING.md`](../../docs/TREE_AGING.md). Species lifespans are **hardcoded** (`WildTreeGrowthProfiles.cs`), not config keys.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableTreeAging` | bool | true | Calendar age + yearly structure growth; **required** for senescence scheduler |
| `MaxTreeGrowthAttemptsPerTick` | int | 6 | Trees advanced per reproduce tick (global round-robin) |
| `TreeGrowthActivityScale` | float | 1 | Growth pace vs reference size |
| `EnableTreeSenescence` | bool | true | Phased death after species lifespan |
| `TreeSenescenceSnagBlocks` | int | 3 | `log-grown` blocks left during snag year |
| `EnableTreeSenescenceRemains` | bool | true | Final year: vanilla stump + fallen logs |
| `TreeSenescenceFallenLogCount` | int | 3 | Horizontal debarked logs near stump (0 = stump only) |

### Tree fern & wild vines

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableFerntreeEcology` | bool | true | `ferntree-normal-*` register, spread, age, senescence |
| `FerntreeSenescenceSnagSegments` | int | 2 | Trunk segments during ferntree snag phase |
| `EnableWildVineEcology` | bool | true | `wildvine-end-*` spread down and along walls |
| `WildVineWallCaptureRadius` | int | 4 | Horizontal wall-face capture radius |
| `WildVineWallCaptureHeight` | int | 6 | Vertical wall-face capture span |

### Mycelium ecology

Vanilla `BlockEntityMycelium` only.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableMyceliumNiche` | bool | true | Meadow spread penalty / forest bonus near anchors |
| `MyceliumZoneRadius` | int | 7 | Chebyshev niche radius (vanilla growRange 7) |
| `MyceliumMeadowSpreadPenalty` | float | 0.35 | Meadow fitness at anchor (tapers to 1.0 at edge) |
| `MyceliumForestSpreadBonus` | float | 1.22 | Forest understory bonus at anchor |
| `MyceliumSkipSoilSuccession` | bool | true | No soil succession on mycelium anchor cells |
| `EnableMyceliumEcology` | bool | true | Register anchors; niche stress and death |
| `MyceliumTreeHostRadius` | int | 4 | Tree-host search for forest mycelium survival |
| `MyceliumForestMinForestCover` | float | 0.12 | Forest anchor stressed below this cover in open |
| `MyceliumMeadowMaxForestCover` | float | 0.45 | Meadow anchor stressed above this cover |
| `EnableMyceliumNetworkSpread` | bool | true | Slow orthogonal network spread from mat edge |
| `MyceliumSpreadRate` | float | 0.12 | Network interval scale (lower = slower) |
| `MyceliumSpreadAttemptsPerYear` | double | 4 | Network attempts per year at rate 1.0 |
| `MyceliumSpreadMinFitness` | float | 0.35 | Min fitness to colonize / displace neighbor anchor |

### Seasonal canopy (foliage)

See [`CANOPY_PHENOLOGY.md`](../../docs/CANOPY_PHENOLOGY.md).

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableSeasonalFoliage` | bool | true | Deciduous autumn strip / spring bud on `log-grown` skeleton |
| `FoliageSyncMode` | string | `chunk` | `chunk` / `hybrid` / `random` |
| `MaxFoliageCellsTickedPerTick` | int | 0 | Random foliage cells per reproduce tick (hybrid/random); 0 = off |
| `FoliageBudgetMs` | int | 10 | Wall-time cap for foliage random tick |
| `FoliageChunkSyncBudgetMs` | int | 12 | Wall-time per chunk foliage sync pass |
| `FoliageChunkWorkPerTick` | int | 4 | Chunks resumed per chunk-scan tick |
| `FoliageCatchUpOnChunkLoad` | bool | true | Sync foliage to current season on chunk scan |
| `MaxFoliageCatchUpPerChunk` | int | 2048 | Max strip+bud ops per chunk per pass (0 = unlimited) |
| `FoliageColumnScanHeightAboveSurface` | int | 0 | Scan depth above surface (0 = full column) |
| `FoliagePeakAutumnBranchyStripActivity` | float | 0.35 | Peak autumn activity before partial branchy strip (0 = keep all) |
| `EnableCanopyFallenSticks` | bool | true | Drop `loosestick-free` when branchy foliage strips |
| `CanopyFallenStickChance` | float | 0.42 | Stick drop chance scale at peak autumn |
| `EnableSpringBranchyAgeBoost` | bool | true | Spring branchy buds scale with tree calendar age |
| `SpringBranchyAgeBoostYearsToMax` | float | 60 | Years to max spring branchy boost |
| `SpringBranchyAgeBoostMax` | float | 1.5 | Max spring branchy bud multiplier from age |
| `FoliageRestoreBareSkeleton` | bool | true | Winter repair: branchy leaves on bare log-grown pillars |
| `CanopyActivityScale` | float | 1 | Multiplier on per-wood monthly defol/bud curves |
| `CanopyBudMinTemperature` | float | 5 | °C at cell for spring bud attempts |
| `CanopyLatitudeInfluence` | float | 0.35 | Polar slowdown (0 = off) |

### Canopy ambience (client only)

See [`CANOPY_AMBIENCE.md`](../../docs/CANOPY_AMBIENCE.md).

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableCanopyAmbience` | bool | true | Client particles under tall deciduous crowns |
| `CanopyAmbienceMinHeightBlocks` | int | 2 | Min foliage height above player feet |
| `CanopyAmbienceMoteRate` | float | 1 | Green mote spawn rate scale |
| `CanopyAmbienceLeafDriftRate` | float | 1 | Autumn leaf drift rate scale |
| `CanopyAmbienceSampleIntervalSeconds` | double | 2 | Seconds between density re-samples |
| `CanopyAmbienceSuppressInRain` | bool | true | Suppress particles during heavy rain |

### Phase 6 — spread simulation

See [`PHASE6_SIMULATION.md`](../../docs/PHASE6_SIMULATION.md).

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableChunkFairSpread` | bool | true | Round-robin spread across registry chunks |
| `MaxSpreadAttemptsPerChunkPerTick` | int | 2 | Spread attempts per chunk per reproduce tick |
| `MaxSpreadChunksVisitedPerTick` | int | 32 | Registry chunks visited per reproduce tick |
| `EnableEventDrivenSpread` | bool | true | Wake neighbors on ecology-relevant block changes |
| `EnableSeasonCoarseWake` | bool | true | Wake seasonal species once per in-game month |
| `EcologyWakeRadiusBlocks` | int | 0 | Wake radius; 0 = derive from spread radius / spacing |
| `EnableEcologyColumnCache` | bool | true | Cache spread column snapshots |
| `EnableTwoPhaseSpreadPlacement` | bool | true | Evaluate spread, then commit `SetBlock` in fair pass |
| `MaxSpreadCommitsPerTick` | int | 0 | Commits per tick (0 = `MaxReproduceAttemptsPerTick`) |
| `MaxSpreadCommitChunksVisitedPerTick` | int | 0 | Commit pass chunks (0 = `MaxSpreadChunksVisitedPerTick`) |
| `MaxSpreadCommitsPerChunkPerTick` | int | 0 | Commits per chunk (0 = `MaxSpreadAttemptsPerChunkPerTick`) |

### Registration & performance

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `MaxReproduceAttemptsPerTick` | int | 64 | Spread evaluation attempts per reproduce tick |
| `MaxChunkColumnsScannedPerTick` | int | 16 | Sync column scan when background scan off |
| `MaxRegistrationsPerTick` | int | 2048 | Sync registrations when background scan off |
| `EnablePlayerPriorityRegistration` | bool | true | Drain player-vicinity chunks before background queue |
| `EnableBurstRegistrationNearPlayers` | bool | true | Finish nearby chunk registration on load (ms budget) |
| `PlayerRegistrationPriorityRadiusBlocks` | int | 16 | Priority / burst registration radius |
| `MaxPriorityChunkScansPerTick` | int | 48 | Priority queue passes per chunk-scan tick |
| `MaxPriorityRegistrationsPerTick` | int | 8192 | Registration cap for priority queue per tick |
| `PriorityRegistrationBudgetMs` | int | 80 | Ms budget per priority registration pass |
| `BurstRegistrationBudgetMs` | int | 80 | Ms budget to finish one burst chunk on load |
| `MaxBurstRegistrationsPerChunk` | int | 4096 | Max registrations per burst chunk completion |
| `MaxRegistryAppliesPerTick` | int | 512 | Paced `RegisterReproducer` applies per chunk-scan tick |
| `MaxPriorityRegistryAppliesPerTick` | int | 2048 | Extra applies for player-vicinity chunks |
| `EnableBackgroundRegistrationScan` | bool | true | Classify columns on worker from main-thread snapshot |
| `MaxRegistrationSnapshotCellsPerTick` | int | 8192 | Block ids copied to snapshot per main tick |
| `TickBudgetMs` | int | 30 | Default ms cap per reproduce tick (0 = unlimited) |
| `SpreadBudgetMs` | int | 30 | Spread cap (0 = `TickBudgetMs`) |
| `RegistrationBudgetMs` | int | 25 | Chunk-scan cap (0 = `TickBudgetMs`) |
| `StressBudgetMs` | int | 0 | Stress cap (0 = `TickBudgetMs`) |
| `ReproduceTickIntervalMs` | int | 2000 | Spread / foliage / tree-growth tick interval |
| `ChunkScanTickIntervalMs` | int | 2300 | Registration + foliage chunk sync tick (desynced from spread) |
| `StressTickIntervalMs` | int | 5500 | Stress tick interval |
| `OnlyActivateNearPlayers` | bool | false | Limit spread, stress, trees, **and chunk scans** to player radius |
| `LimitSpreadNearPlayers` | bool | false | Limit spread, stress, tree aging to radius; **registration unchanged** |
| `PlayerActivationRadiusBlocks` | int | 192 | Radius for the two flags above |
| `EnableReproduceTickProfiling` | bool | false | Log phase timings when registry is large |
| `ReproduceTickProfilingMinRegistry` | int | 2000 | Min registry size before profiling logs |
| `ReproduceTickProfilingIntervalMs` | int | 30000 | Min ms between profiling log lines |
| `VerboseLogging` | bool | false | Master switch for Notification/Warning diagnostics |
| `ReproduceDebug` | bool | false | Log spread attempts (use with `VerboseLogging` for tuning) |

### Trampling

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableTrampling` | bool | false | Player proximity accumulates trampling stress |
| `TramplingRadius` | int | 1 | Horizontal distance for trampling exposure |
| `TramplingStressThreshold` | int | 8 | Exposure ticks before trampling counts as failed survival |
| `TramplingSoilDegradation` | bool | false | Degrade soil when plant dies from trampling |

### Meadow harvest

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableFlowerDrygrass` | bool | true | Empty hand → flower block; knife/scythe → drygrass; tallgrass no loot |

### Ecology inspect

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableEcologyInspect` | bool | true | Hotkey **I** ecology dialog |
| `EcologyInspectCooldownSeconds` | double | 2 | Min seconds between inspect requests per player |
| `EcologyInspectScanRadius` | int | 16 | Radius for nearby-species tally |
| `EnableEcologyAreaScan` | bool | true | Include area species mix in report |

### Wild berries (VS 1.22+)

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `CloneBerryTraits` | bool | true | Spread copies parent bush genetic traits |
| `BerryTraitMutationChance` | double | 0 | Chance to lose one random trait on spread (0 = off) |

### Third-party mods

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `EnableThirdPartyParticipants` | bool | true | Blocks with `ecologyParticipant` JSON join ecology |

Block JSON (not in `ecosystemflora.json`): `ecologySpreadMode` — `rhizome`, `surfacemat`, or `independent`. See [`THIRD_PARTY_ECOLOGY.md`](../../docs/THIRD_PARTY_ECOLOGY.md).

### Legacy JSON aliases

| Alias | Maps to |
|-------|---------|
| `MaxCanopyUpdateOpsPerTick` | `MaxFoliageCellsTickedPerTick` |
| `CanopyBudgetMs` | `FoliageBudgetMs` |

Prefer the primary names in new configs.

---

## Changelog of config keys (by version)

For release history see [`CHANGELOG.md`](../../docs/CHANGELOG.md). Keys added since **3.1.2** are listed there and in older ModDB paste blocks; this file is the **complete** current set.
