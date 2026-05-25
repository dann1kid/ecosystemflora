# Wild Farming (ecosystem fork)

Vintage Story mod — **natural spread** for vanilla wild plants: climate, rainfall, forest density, spacing, seasonal ecology, and habitat-specific placement.

**Docs:** [`docs/PROGRESS.md`](docs/PROGRESS.md) · [`docs/PROJECT_VISION.md`](docs/PROJECT_VISION.md) · [`docs/PROMPT.md`](docs/PROMPT.md) · [`docs/VISUAL_STUDIO.md`](docs/VISUAL_STUDIO.md)

**Stage:** `Ecosystem v2.3` (`2.5.0`) — seasonal spread/stress, cell competition, niche, soil succession, perf audit done, 46 unit tests.

**Build:** `dotnet build` → `bin/Debug/Mods/wildfarming/` (copies to `Mods\wildfarming` when client is closed).
**Tests:** `dotnet test tests/WildFarming.Tests.csproj`


| Requirement | Version                |
| ----------- | ---------------------- |
| Game        | 1.21+ (`modinfo.json`) |
| .NET SDK    | 10.x                  |


## What it does

Vanilla blocks stay in the world (`game:flower-*`, reeds, water lily, water crowfoot, ferns, berries, tree saplings). The mod patches `entityClass: EcoSystemLife` and registers mature plants for **wild reproduction**:

- scan neighbors in radius → filter by climate / soil / water / niche / spacing
- seasonal multipliers per species (spring boost, winter dormancy, fall die-off)
- weighted pick among valid cells → place the same vanilla block type
- cell competition: challenger displaces weaker incumbents; stress death outside niche
- symbiosis: forest understory dies when host tree is removed

**Not in build:** mod seeds, `wildplant` blocks, living trees (legacy under `src/`, excluded from csproj).

## Plants in ecosystem


| Group        | Examples                                                                     |
| ------------ | ---------------------------------------------------------------------------- |
| Flowers      | 20 × `game:flower-*` + lupine (knife/scythe → `drygrass`)                   |
| Meadow grass | `game:tallgrass-*` (matrix under flowers)                                    |
| Ferns        | `fern-*` (4 species) + `tallfern` — forest understory, shade, moisture       |
| Berries      | `fruitingbush-wild-*` (10 species) — soil + `LocalForestCover`               |
| Trees        | `log-grown-{wood}` → spread `sapling-{wood}-free` (14 species)               |
| Reeds        | coopersreed (рогоз), papyrus (камыш) — on `muddygravel`, shallow water      |
| Surface      | waterlily — on open water surface                                            |
| Underwater   | water crowfoot (водяной лютик) — multi-block column, 2–8 depth              |


Details and playtest checklist: [`docs/PROGRESS.md`](docs/PROGRESS.md).

## Config

`%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`

Reference template: [`assets/wildfarming/wildfarming-ecosystem.example.json`](assets/wildfarming/wildfarming-ecosystem.example.json)

Set **`BalancePreset`** to `natural` (default), `lush`, or `sparse` — spread tuning is applied on server start. Use `custom` to keep manual numbers only.


| Preset  | Attempts/year | Chance | MinFitness |
| ------- | ------------- | ------ | ---------- |
| natural | 42            | 0.35   | 0.5        |
| lush    | 60            | 0.45   | 0.4        |
| sparse  | 24            | 0.2    | 0.65       |


### Key config options


| Key                        | Default | Effect                                            |
| -------------------------- | ------- | ------------------------------------------------- |
| `UseCellDisplacement`      | true    | Spread can replace weaker ecology plants          |
| `DisplacementHoldMargin`   | 1.25    | Challenger must beat hold × margin                |
| `EnableStressDeath`        | true    | Plants die after repeated failed survival checks  |
| `EnableSymbiosis`          | true    | Ferns/forest flowers die when nearby tree removed |
| `UseFloraContext`          | true    | Forest-edge fitness multipliers                   |
| `UseSeasonalEcology`       | true    | Spread and stress follow game calendar            |
| `SeasonalStressEnabled`    | true    | Winter kill / fall die-off for terrestrial plants  |
| `UseNicheContext`          | true    | Moisture + light niche multipliers                |
| `RespectLandClaims`        | true    | No spread/displace/stress inside land claims      |
| `VerboseLogging`           | false   | Enable detailed log output (perf cost)            |


Logs: use `VerboseLogging: true` + `ReproduceDebug: true` for diagnostics. Keep both `false` for normal play.

## Legacy code

Older systems remain in the repo but are **excluded from the build**. See `docs/PROJECT_VISION.md`.

## Original mod

Based on JakeCool19's Wild Farming (v1.2.0). Do not run alongside [Wild Farming Revival](https://mods.vintagestory.at/wildfarmingrevival).
