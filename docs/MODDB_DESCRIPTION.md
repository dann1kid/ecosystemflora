# Ecosystem - Flora — ModDB description

> Текст ниже — черновик описания для страницы на ModDB / mods.vintagestory.at.

---

## Short description (one-liner)

Living wild flora: flowers, grass, ferns, berries, reeds, and trees spread naturally, compete for space, and follow seasons — all on vanilla blocks.

---

## Full description

### Your world comes alive

Ecosystem - Flora turns static worldgen flora into a living ecosystem. Flowers spread across meadows, ferns creep under the forest canopy, reeds colonize shorelines, and mature trees seed new saplings — all without replacing any vanilla blocks.

Install the mod, load your world, and watch it change over the seasons.

### What spreads

- **20 flower species** — daisies, cornflowers, poppies, catmint, heather, and more
- **Tallgrass** — fills in as a grass matrix under flowers
- **5 fern species** — forest understory that needs shade and moisture
- **10 wild berry bushes** — blueberry, cranberry, strawberry...
- **14 tree species** — mature trunks spread free saplings; growth is vanilla
- **Reeds, tule, and papyrus** — shore and shallow water over gravel beds
- **Water lily** — spreads across open water surfaces
- **Water crowfoot** — underwater column plant, 2–8 blocks deep

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

Aim at any wild ecosystem plant and press **I** to open a small report: succession role, registry status, stress, next spread timing, seasonal activity, niche fit, symbiosis, climate survival, and a short tally of dominant species nearby (uses the mod’s spacing index). All UI strings follow **your client language**; inspect errors also show localized chat messages instead of the server locale.

*Tunable in `ModConfig/ecosystemflora.json`*: `EnableEcologyInspect`, `EcologyInspectCooldownSeconds`, `EcologyInspectScanRadius`, `EnableEcologyAreaScan`.

### Bonus: drygrass from flowers

Cut any wildflower with a knife or scythe and it drops drygrass along with its usual drops — no need to hunt for plain grass.

### Your world stays safe

Every block placed by the mod is a vanilla game block. **Removing the mod leaves your world completely intact** — no orphaned custom blocks, no broken chunks.

### Respects your builds

Wild spread, displacement, and stress death are blocked inside **land claims**. Your gardens and farms are safe.

Plants never spread onto **mycelium blocks** (mushroom spawning ground) — mushrooms continue to regrow naturally. Unclaimed empty **farmland** can be colonized by wild plants over time, and those plants slowly restore soil nutrients (fallow restoration).

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
| `UseSoilSuccession` | true | Plants gradually change soil quality |
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
| `EnableFlowerDrygrass` | true | Flowers drop drygrass when cut with knife/scythe |

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
| `SoilSuccessionStrength` | 1.0 | Speed of soil changes |
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
| `EnableEcologyInspect` | true | Hotkey **I**: in-world ecology report for aimed plant |
| `EnableEcologyAreaScan` | true | Include nearby-species mix in the inspect dialog |
| `OnlyActivateNearPlayers` | true | Limit activity to player radius |
| `PlayerActivationRadiusBlocks` | 192 | Radius if above is true |
| `VerboseLogging` | false | Detailed server log output |

All settings work together — presets give a good baseline, toggles let you disable features you don't want, and number fields let you fine-tune the balance.

### Requirements

- Vintage Story **1.22+**
- Do **not** run alongside Wild Farming Revival

### Credits

Originally inspired by JakeCool19's Wild Farming (v1.2.0). Fully rewritten.
