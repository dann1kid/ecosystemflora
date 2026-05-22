# Wild Farming (ecosystem fork)

Vintage Story mod — cultivate wild plants with an **ecosystem layer**: player planting, climate survival, optional wild reproduction.

**Docs:** [`docs/PROGRESS.md`](docs/PROGRESS.md) · [`docs/PROJECT_VISION.md`](docs/PROJECT_VISION.md) · [`docs/PROMPT.md`](docs/PROMPT.md) · [`docs/VISUAL_STUDIO.md`](docs/VISUAL_STUDIO.md)

**Стадия:** `MVP-beta` — catmint playtest OK (посадка, рост, reproduce).

**Build:** open [`wildfarming.sln`](wildfarming.sln) in Visual Studio 2022, or `dotnet build wildfarming.sln -c Debug`. Output: `bin/Debug/Mods/wildfarming/` (auto-deployed to game `Mods` when the client is closed).

| Requirement | Version |
|-------------|---------|
| Game | 1.21+ (`modinfo.json`) |
| .NET SDK | 10.x (match installed Vintage Story) |

## How it works (three layers)

1. **Planting (player)** — seeds can be placed on any valid spot (solid ground, empty space above, land claims). No climate check at click time.
2. **Survival (individual)** — `wildplant` grows toward a vanilla block; if climate/soil is unsuitable for too long, the plant **dies** (`MaxFailedSurvivalChecks` in config).
3. **Wild reproduction (ecosystem)** — mature plants registered by the mod (e.g. catmint) may spawn new `wildplant` nearby only where `MinFitness` suitability passes.

Config file (created on first run): `%AppData%/VintagestoryData/ModConfig/wildfarming-ecosystem.json`

## Adding a plant

Pair item and block codes by suffix:

- Item: `wildseeds-{suffix}` (e.g. `wildseeds-flower-catmint-free`)
- Block: `wildplant-{suffix}` (e.g. `wildplant-flower-catmint-free`)

Matures to `game:{suffix without wildplant- prefix}` → `game:flower-catmint-free`.

Optional JSON on `wildplant`:

| Attribute | Purpose |
|-----------|---------|
| `hours` / `hoursByType` | Average grow time (hours) |
| `minTemp` / `maxTemp` / `*ByType` | Survival temperature range |
| `ecologyReproduceByType` | Enable wild reproduction (e.g. `"*-catmint-*": true`) |

## Legacy code

Older systems (living trees, mushrooms, vines, Harmony patches) remain under `src/` but are **excluded from the build**. See `docs/PROJECT_VISION.md` for scope.

## Original mod

Based on JakeCool19's Wild Farming (v1.2.0). Do not run alongside [Wild Farming Revival](https://mods.vintagestory.at/wildfarmingrevival) — same niche, different `modid`.
