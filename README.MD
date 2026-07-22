# Ecosystem - Flora

A Vintage Story mod that makes wild flora **alive**. Flowers spread across meadows, ferns creep under forest canopy, reeds colonize shorelines, and trees seed new saplings — all following climate, soil, seasons, and competition. This mod does not add its own block IDs: spread uses **vanilla parents** by default; other mods can opt in via JSON (**[`docs/THIRD_PARTY_ECOLOGY.md`](docs/THIRD_PARTY_ECOLOGY.md)**).

## How it works

Every wild plant registered by the mod periodically tries to reproduce onto a neighboring cell. Whether it succeeds depends on the environment at that cell:

- **Climate** — temperature and rainfall from worldgen maps
- **Soil** — fertility, type (forest floor, peat, sand, clay, gravel)
- **Light and moisture** — shade under canopy, proximity to water
- **Forest density** — counted from nearby tree trunks, not worldgen
- **Season** — spring bloom boost, summer stability, fall die-off, winter dormancy
- **Spacing** — plants respect distance from same/other species

Plants don't just spread — they **compete**. A stronger species can displace a weaker one from an occupied cell. Plants that end up in the wrong niche (too dry, too dark, wrong soil) accumulate stress and eventually die. Forest understory (ferns, lily of the valley) depends on nearby trees — cut the tree, and its understory gradually withers.

The result: meadows fill in naturally, forest edges develop a distinct flora belt, clearings get reclaimed by fast colonizers first and then by perennials, and shorelines grow reeds and water lilies on their own.

**v3.7** adds **tree fern** ecology (`ferntree-normal-*`), **canopy** fallen sticks and age-scaled spring branches, and **wild vine** spread on wall faces. **v3.6** added wild **tree aging** (calendar years, slow growth, **phased senescence**, **stump + fallen logs**), **inspect (I)** on trunk logs, and a rewritten **handbook** (en/ru). Docs: [`docs/CHANGELOG.md`](docs/CHANGELOG.md), [`docs/FERNTREE.md`](docs/FERNTREE.md), [`docs/WILD_VINE.md`](docs/WILD_VINE.md), [`docs/TREE_AGING.md`](docs/TREE_AGING.md).

## Wild tree lifecycle

| Stage | Behaviour |
|-------|-----------|
| **Seed** | Mature trunk spreads a **young `log-grown` seedling** when tree aging is on (else free sapling); no spread Nov–Feb; not on ice/snow |
| **Grow** | Mod yearly growth from seedling to mature size; player-planted saplings still use vanilla treegen |
| **Life** | Each game year: calendar age +1, optional growth/spread; out-of-niche stands accrue **lifespan debt** (shorter senescence); trunks ignore flower-style stress death |
| **Senescence** | After effective species lifespan: 4 years — crown leaves → skeleton → snag → **stump + fallen logs** |
| **Gap fill** | Remains are choppable vanilla blocks; nearby trees seed the empty cell naturally |

Config: `EnableTreeAging`, `EnableTreeSenescence`, `EnableTreeSenescenceRemains`, `EnableTreeNicheLifespanStress` (default on). Docs: [`docs/TREE_AGING.md`](docs/TREE_AGING.md).

**Removing the mod leaves your world intact** — vanilla parents stay vanilla. Other mods may register their own blocks as spread parents via optional JSON attributes (see **`docs/THIRD_PARTY_ECOLOGY.md`**); disable with `EnableThirdPartyParticipants: false` if you want only hardcoded vanilla paths.

## What spreads

| Group | Species | Notes |
|-------|---------|-------|
| **Meadow flowers** | Daisy, cornflower, poppy, heather, catmint, cowparsley, daffodil, and 13 more | 20 species + lupine |
| **Tallgrass** | All vanilla tallgrass variants | Grass matrix under flowers |
| **Ferns** | Eagle fern, cinnamon fern, deer fern, hart's tongue, tall fern | Forest understory; need shade and moisture |
| **Berry bushes** | Blueberry, cranberry, strawberry, and 7 more | 10 wild species; soil + forest edge preference |
| **Trees** | Oak, pine, birch, maple, and 10 more | Mature trunks seed free saplings; **v3.6** calendar age, senescence; **v3.7** canopy sticks + age branches |
| **Tree fern** | `ferntree-normal-*` | Tropical column; spread, aging, senescence (**v3.7**) |
| **Wild vines** | `wildvine-end-*` | Tip grows down and along walls (**v3.7**) |
| **Reeds** | Cooper's reed, papyrus | Shore or shallow water over gravel bed |
| **Water lily** | Waterlily | Open water surface |
| **Water crowfoot** | Underwater column plant | 2–8 block depth, multi-section column |

## Seasons

Each species has a seasonal profile:

- **Spring** — boosted spread rate; early spring has an extra ramp
- **Summer** — baseline activity
- **Fall** — annual meadow flowers (daisy, poppy, heather) have a die-off chance; perennials slow down
- **Winter** — most plants go dormant; annuals die, hardy grasses and aquatic plants survive better

## Config

Per-world settings: `%AppData%\VintagestoryData\ModConfig\ecosystemflora\worlds\<WorldName_Seed>\` (category JSON). First-run **setup wizard** (`/ecosetup`) and light auto-tune (`/ecoautotune`). Legacy flat `ecosystemflora.json` migrates automatically.

The easiest way to tune the experience is the **`BalancePreset`** setting:

| Preset | Feel | Spread attempts/year | Chance | Min fitness |
|--------|------|---------------------|--------|-------------|
| `natural` (default) | Full ecology on, balanced | 72 | 0.50 | 0.45 |
| `lush` | Same features, denser | 120 | 0.65 | 0.35 |
| `sparse` | Same features, patchier | 36 | 0.30 | 0.60 |
| `vanilla-minimal` | Natural without juvenile/phenology | 72 | 0.50 | 0.45 |

Set `BalancePreset` to `custom` to keep your own feature toggles and numbers across restarts.

**Full key reference:** [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md) (defaults match `EcosystemConfig.cs`). Example JSON: `assets/ecosystemflora/ecosystemflora.example.json`.

| Setting | Default | What it does |
|---------|---------|-------------|
| `UseSeasonalEcology` | true | Plants follow the game calendar |
| `UseCellDisplacement` | true | Stronger species can replace weaker ones |
| `EnableStressDeath` | true | Plants die when stuck in the wrong niche |
| `EnableSymbiosis` | true | Forest understory depends on nearby trees |
| `RespectLandClaims` | true | No wild changes inside your land claims |
| `VerboseLogging` | false | Detailed logs for diagnostics (keep off in play) |
| `CloneBerryTraits` | true | Wild berry spread copies parent **traits** (VS 1.22+) |
| `EnableThirdPartyParticipants` | true | Other mods can register blocktypes with `ecologyParticipant` |
| `EnableTreeAging` | true | Wild trunks gain calendar age and grow slowly each game year |
| `EnableTreeSenescence` | true | Phased natural death after species lifespan (4 game years) |
| `TreeSenescenceSnagBlocks` | 3 | Standing trunk blocks during snag year |
| `EnableTreeSenescenceRemains` | true | Final year leaves choppable stump + fallen logs |
| `TreeSenescenceFallenLogCount` | 3 | Ground logs near stump (0 = stump only) |
| `OnlyActivateNearPlayers` | false | Ecology in all **loaded chunks**; set true only for local perf testing |
| `EnableSeasonalFoliage` | true | Deciduous autumn leaf drop and spring bud |
| `EnableCanopyAmbience` | true | Client leaf particles near tall crowns |

## Installation

1. Download the latest release
2. Place the `ecosystemflora` folder into your `Mods` directory
3. Start the game — default settings work well for most worlds

**Requirements:** Vintage Story 1.22+
**Do not** run alongside [Wild Farming Revival](https://mods.vintagestory.at/wildfarmingrevival).

## For developers

**.NET 10 SDK** required. Open `wildfarming.sln` in Visual Studio, or use the CLI:

| Task | Command |
|------|---------|
| Build | `dotnet build` |
| Tests | `dotnet test tests/WildFarming.Tests.csproj` |
| Release zip | `.\tools\package.ps1` |

`package.ps1` runs tests first, then builds **Release** and writes `ecosystemflora-<version>.zip` in the repo root. Use `-SkipTests` only for local throwaway packs. VS setup: [`docs/VISUAL_STUDIO.md`](docs/VISUAL_STUDIO.md).

Docs: [`docs/PROGRESS.md`](docs/PROGRESS.md) · [`docs/CHANGELOG.md`](docs/CHANGELOG.md) · [`docs/CONFIGURATION.md`](docs/CONFIGURATION.md) · [`docs/PROJECT_VISION.md`](docs/PROJECT_VISION.md) · [`docs/PROMPT.md`](docs/PROMPT.md) · **[`docs/THIRD_PARTY_ECOLOGY.md`](docs/THIRD_PARTY_ECOLOGY.md)** (API для авторов модов через `ecologyParticipant`)

Originally inspired by JakeCool19's Wild Farming (v1.2.0). Fully rewritten.

## License

MIT — see [`LICENSE`](LICENSE). Vintage Story itself remains proprietary; this license covers this mod's source and assets only. Third-party ecology tables / extracted upstream data (if present) remain under their original terms.
