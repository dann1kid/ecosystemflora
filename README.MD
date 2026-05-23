# Wild Farming (ecosystem fork)

Vintage Story mod — **natural spread** for vanilla wild plants: climate, rainfall, forest density, spacing, and habitat-specific placement.

**Docs:** [`docs/PROGRESS.md`](docs/PROGRESS.md) · [`docs/PROJECT_VISION.md`](docs/PROJECT_VISION.md) · [`docs/PROMPT.md`](docs/PROMPT.md) · [`docs/VISUAL_STUDIO.md`](docs/VISUAL_STUDIO.md)

**Стадия:** `Ecosystem v1.2` (`2.2.0-ecosystem-v1.2`) — flowers, tallgrass, aquatic flora, balance presets, drygrass drops.

**Build:** `dotnet build` → `bin/Debug/Mods/wildfarming/` (копируется в `Mods\wildfarming`, если клиент закрыт).

| Requirement | Version |
|-------------|---------|
| Game | 1.21+ (`modinfo.json`) |
| .NET SDK | 10.x |

## What it does

Vanilla blocks stay in the world (`game:flower-*`, reeds, water lily, water crowfoot). The mod patches `entityClass: EcosystemPlant` and registers mature plants for **wild reproduction**:

- scan neighbors in radius → filter by climate / soil / water / spacing
- weighted pick among valid cells → place the same vanilla block type

**Not in build:** mod seeds, `wildplant` blocks, living trees (legacy under `src/`, excluded from csproj).

## Plants in ecosystem

| Group | Examples |
|-------|----------|
| Flowers | 20 × `game:flower-*` + lupine (knife/scythe → `drygrass`) |
| Meadow grass | `game:tallgrass-*` (matrix under flowers) |
| Reeds | coopersreed (рогоз), papyrus (камыш) — on `muddygravel`, shallow water rules |
| Surface | waterlily |
| Underwater | water crowfoot (водяной лютик) — multi-block column |

Details and playtest checklist: [`docs/PROGRESS.md`](docs/PROGRESS.md).

## Config

`%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json`

Set **`BalancePreset`** to `natural` (default), `lush`, or `sparse` — spread tuning is applied on server start. Use `custom` to keep manual numbers only.

| Preset | Attempts/year | Chance | MinFitness |
|--------|---------------|--------|------------|
| natural | 42 | 0.35 | 0.5 |
| lush | 60 | 0.45 | 0.4 |
| sparse | 24 | 0.2 | 0.65 |

## Legacy code

Older systems remain in the repo but are **excluded from the build**. See `docs/PROJECT_VISION.md`.

## Original mod

Based on JakeCool19's Wild Farming (v1.2.0). Do not run alongside [Wild Farming Revival](https://mods.vintagestory.at/wildfarmingrevival).
