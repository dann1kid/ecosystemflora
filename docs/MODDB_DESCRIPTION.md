# Ecosystem - Flora — ModDB description

> Текст ниже — черновик описания для страницы на ModDB / mods.vintagestory.at.

---

## Short description (one-liner)

Living wild flora: flowers, grass, ferns, berries, reeds, and trees spread naturally, compete for space, and follow seasons — all on vanilla blocks.

---

## What changed in 3.1.x (for players)

**3.1.0** — Other mods can register plants via JSON (`ecologyParticipant`). Optional berry trait mutation on spread.

**3.1.2** — Soil succession rebalanced: meadows enrich soil; heather and western gorse slightly dry poor soils. Soil changes skip columns with slabs/builds above (Terrain Slabs friendly).

**3.1.3–3.1.5** — **Reeds, tule, and papyrus** spread from the **edge** of a stand (rhizome mat), not random radius jumps. Rare seed/fragment jumps colonize distant banks. **Water lily** spreads as a **floating pad mat** on open water with similar rare seed jumps. **Water crowfoot** unchanged (legacy radius spread).

**3.1.6** — Handbook and **inspect (I)** show spread mode, mat edge yes/no, and seed chance. Tune mat spread in config: `UseRhizomeSpreadForReeds`, `UseSurfaceMatSpreadForLilies`, `RhizomeSeedDispersal*`.

**3.1.7** — **Meadow harvest:** empty hotbar slot → flower or tallgrass **block**; **knife** or **scythe** → **drygrass** only. Scythe can mow all meadow flowers (vanilla: horsetail only). Toggle: `EnableFlowerDrygrass`.

Press **I** on any wild plant to debug spread timing, stress, and mat status. Enable **`VerboseLogging`** + **`ReproduceDebug`** in config for server log detail.

---

## Full description

### Your world comes alive

Ecosystem - Flora turns static worldgen flora into a living ecosystem. Flowers spread across meadows, ferns creep under the forest canopy, reeds colonize shorelines, and mature trees seed new saplings — all without replacing any vanilla blocks.

Install the mod, load your world, and watch it change over the seasons.

### What spreads

- **20 flower species** — daisies, cornflowers, poppies, catmint, heather, and more
- **Tallgrass** — fills in as a grass matrix under flowers
- **5 fern species** — forest understory that needs shade and moisture
- **10 wild berry bushes** — blueberry, cranberry, strawberry… (in **1.22+**, wild berry spread can **clone parent traits** when `CloneBerryTraits` is on — see config)
- **14 tree species** — mature trunks spread free saplings; growth is vanilla
- **Reeds, tule, and papyrus** — shore and shallow water over gravel beds
- **Water lily** — spreads across open water surfaces
- **Water crowfoot** — underwater column plant, 2–8 blocks deep (legacy radius spread; mat logic not applied yet)

### Not just spreading — competing

Plants don't blindly fill every empty cell. They **compete**:

- A stronger species can **displace** a weaker one from an occupied cell
- Plants stuck in the wrong niche (too dry, wrong soil, not enough light) accumulate **stress and die**
- Forest flowers and ferns depend on nearby trees — cut the tree, and its understory gradually **withers**
- Fast colonizers grab cleared ground first, but get **replaced** by slower perennials over time

### Seasons matter

Each species follows its own **12-month seasonal curve** — spread peaks in species-appropriate months and stress accumulates in harsh ones:

- **Spring** — meadows bloom; colonizers rush cleared ground
- **Summer** — steady peak for most species
- **Fall** — annuals die off; perennials slow down
- **Winter** — dormancy; annuals die, hardy perennials survive

### Paths form where you walk

Players trample nearby plants over time. Walk the same route often enough and flora dies off, soil degrades, and a natural path appears. Leave the area alone and the ecosystem will reclaim it.

*Trampling is experimental and off by default — enable it in the config.*

### Ecology inspect (hotkey **I**)

Aim at any wild ecosystem plant and press **I** for a report: succession role, registry status, stress, next spread timing, seasonal activity, niche fit, symbiosis, climate survival, **spread mode / mat edge / seed chance** (reeds & lily), and dominant species nearby.

*Tunable in `ModConfig/ecosystemflora.json`*: `EnableEcologyInspect`, `EcologyInspectCooldownSeconds`, `EcologyInspectScanRadius`, `EnableEcologyAreaScan`.

### Harvest balance (flowers)

Break wildflowers or tallgrass with an **empty hand** to collect the **plant block**. **Knife** or **scythe** mows **drygrass** (flowers and tallgrass; scythe also mows all meadow flowers via mod patch). Turn off with `EnableFlowerDrygrass: false`.

### Your world stays safe

The mod does not replace vanilla worldgen blocks. Vanilla parents stay vanilla when you remove the mod. **Optional integration:** other mods can register their own plant blocktypes via JSON (`ecologyParticipant`, etc.); see the repo **`docs/THIRD_PARTY_ECOLOGY.md`** or turn it off with `EnableThirdPartyParticipants: false` if you want only built-in vanilla paths.

### Respects your builds

Wild spread, displacement, and stress death are blocked inside **land claims**. Your gardens and farms are safe.

Plants never spread onto **mycelium blocks** (mushroom spawning ground) — mushrooms continue to regrow naturally. Unclaimed empty **farmland** can be colonized by wild plants over time, and those plants slowly restore soil nutrients (fallow restoration).

### Soil succession (optional)

With **`UseSoilSuccession`** on (default), meadow spread and natural plant death **enrich** soil tiers over time. **Heather** and **western gorse** are exceptions — they slightly dry poor soils while growing. Prefer spread and competition only, with no soil block swaps? Set **`UseSoilSuccession: false`**. **`SoilSuccessionSkipWhenBuiltAbove`** (default on) skips soil changes under slabs and builds — compatible with **Terrain Slabs** and similar mods.

### Aquatic spread (reeds & lily)

**Cattail, bulrush, and papyrus** use **rhizome mat** spread: only plants on the **edge** of a stand step one block along the shore. Rare **seed/fragment** jumps (~6–10% of attempts) can colonize distant bank cells. **Water lily** uses a **floating pad mat** (eight-connected edge on open water) with ~5% seed jumps. Press **I** on any plant for mat edge and seed chance in the inspect report.

Turn off mat logic and restore legacy radius-4 spread for reeds: **`UseRhizomeSpreadForReeds: false`**. Lily mat: **`UseSurfaceMatSpreadForLilies`**.

### Greenhouse support

Flowers planted inside a fully enclosed glass-roofed room (a greenhouse) are protected from temperature stress — no land claim required. Grow tropical flowers in cold biomes inside your greenhouse.

### Easy to tune

Edit `ModConfig/ecosystemflora.json` (created on first launch).

#### Quick start — balance presets

Set `"BalancePreset"` to one of:

| Preset | Style | Attempts/yr | Chance | Fitness | Spacing |
|--------|-------|:---:|:---:|:---:|:---:|
| `"natural"` *(default)* | Realistic | 72 | 0.50 | 0.45 | 1 block |
| `"lush"` | Greener, denser | 120 | 0.65 | 0.35 | 1 block |
| `"sparse"` | Minimal, subtle | 36 | 0.30 | 0.60 | 2 blocks |
| `"custom"` | Manual | — | — | — | — |

Presets overwrite **5 fields** on startup: `ReproduceAttemptsPerYear`, `ReproduceChance`, `MinFitness`, `DefaultSameSpeciesSpacing`, `DefaultOtherSpeciesSpacing`. Set `"custom"` to use your own values.

#### Feature toggles (true/false)

| Setting | Default | What it does |
|---------|:-------:|-------------|
| `EcosystemEnabled` | true | Master switch — disable all spread/competition |
| `UseSeasonalEcology` | true | Spread rates follow spring/summer/fall/winter |
| `SeasonalStressEnabled` | true | Winter and fall die-off |
| `UseCellDisplacement` | true | Stronger species displace weaker ones |
| `EnableStressDeath` | true | Plants in wrong niche die over time |
| `EnableSymbiosis` | true | Some species boost each other |
| `UseFloraContext` | true | Forest interior/edge affects spread |
| `UseNicheContext` | true | Local soil + climate niche scoring |
| `UseSoilSuccession` | true | Wild plants gradually enrich soil (humus on spread/death). Set **false** for spread-only — no soil block swaps |
| `SoilSuccessionSkipWhenBuiltAbove` | true | Skip soil swaps when slabs or builds sit on the column (Terrain Slabs compatibility) |
| `UseFarmlandNutrientBridge` | true | Wild plants enrich tilled farmland N/P/K |
| `EnableFallowRestoration` | true | Healthy plants on farmland slowly restore nutrients |
| `RespectLandClaims` | true | No spread/death inside player claims |
| `PlantSpacingEnabled` | true | Minimum distance between plants |
| `HarshWildPlants` | true | Wild plants enforce survival checks |
| `ApplyWorldgenRainForest` | true | Respect worldgen rain/forest values |
| `UseCalendarScaledSpread` | true | Scale intervals to DaysPerYear |
| `UseSpeciesSpreadRates` | true | Per-species spread rates from ecology table |
| `EnableEcologyInspect` | true | Hotkey **I**: in-world ecology report for the plant you aim at |
| `EnableEcologyAreaScan` | true | Include nearby-species mix in the inspect dialog |
| `EnableTrampling` | false | Plants near player paths accumulate stress and die |
| `TramplingSoilDegradation` | false | Trampled paths lose soil fertility |
| `EnableFlowerDrygrass` | true | Empty hand → plant block; knife/scythe → drygrass |
| `CloneBerryTraits` | true | Wild berry spread copies **berry traits** from the parent bush (Vintage Story **1.22+**) |
| `EnableThirdPartyParticipants` | true | Allow other mods to declare ecosystem parents on their blocktypes via JSON attributes |
| `UseRhizomeSpreadForReeds` | true | Cattail/tule/papyrus: mat edge spread (not independent radius-4) |
| `UseSurfaceMatSpreadForLilies` | true | Water lily: floating pad mat on open water |
| `RhizomeSeedDispersalEnabled` | true | Rare seed/fragment jumps for reed and lily mats |
| `RhizomeSeedDispersalChanceScale` | 1.0 | Multiplier on per-species seed chance |
| `RhizomeSeedDispersalFitnessScale` | 0.25 | Harder establishment for distant seed sites |

#### Spread tuning (numbers)

| Setting | Default | What it does |
|---------|:-------:|-------------|
| `ReproduceAttemptsPerYear` | 72 | Spread attempts per game year |
| `ReproduceChance` | 0.50 | Base chance per attempt |
| `MinFitness` | 0.45 | Minimum fitness to reproduce |
| `ReproduceRadius` | 4 | Max horizontal spread distance (blocks) |
| `ReproduceVerticalSearch` | 5 | Y-axis search range |
| `MaxFailedSurvivalChecks` | 5 | Failed checks before plant dies |
| `GrowthHoursMultiplier` | 1.0 | Scale sapling → mature growth time |

#### Competition & niche tuning

| Setting | Default | What it does |
|---------|:-------:|-------------|
| `DisplacementHoldMargin` | 1.18 | Challenger must beat incumbent × this |
| `EmptySpreadFitnessMultiplier` | 2.5 | Empty-cell spread weight when mixed with displacement |
| `NicheStressThreshold` | 0.45 | Niche score below this = stress |
| `SoilSuccessionStrength` | 1.0 | Speed of soil tier changes |
| `BerryTraitMutationChance` | 0.0 | Chance wild berry spread drops one parent trait (0 = off) |
| `FarmlandNutrientBridgeStrength` | 1.0 | Scale of till nutrient bonus |
| `FallowRestorationStrength` | 1.0 | Scale of fallow restoration bonus |
| `FloraOpenInteriorPenalty` | 0.35 | Penalty for open-field species in forest |

#### Trampling tuning

| Setting | Default | What it does |
|---------|:-------:|-------------|
| `TramplingRadius` | 1 | Detection range around player (blocks) |
| `TramplingStressThreshold` | 8 | Stress ticks before a trampled plant dies |

#### Performance

| Setting | Default | What it does |
|---------|:-------:|-------------|
| `TickBudgetMs` | 30 | Hard cap on ms per server tick for spread (0 = unlimited) |
| `StressBudgetMs` | 0 | Hard cap for stress tick (0 = use TickBudgetMs) |
| `StressTickIntervalMs` | 6000 | Interval between stress ticks (ms) |
| `MaxReproduceAttemptsPerTick` | 64 | Spread checks per server tick |
| `MaxStressChecksPerTick` | 16 | Stress checks per tick |
| `MaxChunkColumnsScannedPerTick` | 6 | Chunk registration pacing (unfinished chunks stay queued) |
| `MaxRegistrationsPerTick` | 512 | Max new plant registrations per server tick |
| `OnlyActivateNearPlayers` | true | Limit activity to player radius |
| `PlayerActivationRadiusBlocks` | 192 | Radius if above is true |
| `VerboseLogging` | false | Detailed server log output |

All settings work together — presets give a good baseline, toggles let you disable features you don't want, and number fields let you fine-tune the balance.

### Requirements

- Vintage Story **1.22+**
- Do **not** run alongside Wild Farming Revival

### Credits

Originally inspired by JakeCool19's Wild Farming (v1.2.0). Fully rewritten.
