# Third-party blocks as ecosystem participants (v3.1)

**Sample content mod:** [`examples/ecologysample-mynewplant/`](../examples/ecologysample-mynewplant/README.md) in this tree (also publishable as a **standalone public repo** — see `PUBLISHING.md` there). Three blocktypes, vanilla texture placeholders, no C#. **Already have a plant mod?** See [`EXISTING_MOD.md`](../examples/ecologysample-mynewplant/EXISTING_MOD.md) in the sample (four attributes or a patch file).

When **`EnableThirdPartyParticipants`** is `true` in `ecosystemflora.json` (default), any block type from any mod can register for wild spread if it declares JSON **attributes** on the block type.

Vanilla `game:` plants keep using path-based rules; third-party mode is **additive**.

## Required attributes

| Attribute | Type | Purpose |
|-----------|------|---------|
| `ecologyParticipant` | bool | Must be `true`. |
| `ecologySpecies` | string | Stable id for spacing, displacement, registry (e.g. `bluegrass`). Must be unique among your mod’s ecology plants. |
| `ecologySpreadBlock` | string | Block code placed on spread: full `domain:path` or path relative to this block’s domain. |
| `ecologyHabitat` | string | One of: `Terrestrial`, `TerrestrialTree`, `ReedNearWater`, `WaterSurface`, `UnderwaterColumn` (case-insensitive). |

## Optional attributes

| Attribute | Notes |
|-----------|--------|
| `ecologyMatureBlock` | Mature parent identity in the spread registry. **Recommended on sprout/juvenile** to point to the mature code. On the mature block, if you omit it, the default is **the block itself**. |
| `ecologyReproduce` | Default `true`; set `false` to opt a block type out while keeping other attrs. |
| `minTemp`, `maxTemp`, `minRain`, `maxRain`, `minForest`, `maxForest` | Climate (same as vanilla block attrs). |
| `ecologySpreadRate`, `ecologySpreadRadius` | Per-type tuning. |
| `ecologySpreadMode` | `rhizome`, `surfacemat`, or `independent` (reed / lily mat vs legacy radius search). |
| `ecologySeedDispersalChance`, `ecologySeedDispersalRadius` | Rare seed/fragment jump for mat habitats (optional). |
| `ecologyMinSunlight` | Sunlight gate (terrestrial / tree). |
| `ecologySameSpeciesSpacing`, `ecologyOtherSpeciesSpacing` | Spacing (blocks). |
| `ecologyMinGroundFertility`, `ecologyMaxGroundFertility` | Soil fertility window. |
| `ecologyMeadowHarvest` | Meadow break harvest: `whole` (default) — knife/scythe → drygrass, anything else → plant block in world; `delegate` — only registered `MeadowHarvestRegistry` handlers; `none` — mod skips break harvest entirely. Partial harvest (inflorescences only) should use **right-click / block behavior**, not break. |
| `ecologyMaxWaterDepth`, `ecologyMinWaterDepth`, `ecologyVerticalBlocks`, `ecologyExactWaterDepth` | Aquatic / reed column tuning (see habitat defaults below). |

## Habitat defaults (third-party only)

If you omit climate/spacing, the mod applies **template defaults** per `ecologyHabitat` (then your explicit attrs override). Terrestrial defaults match the generic flower fallback (meadow soil via `WildPlantSoil`).

- **ReedNearWater** — shallow reed–like template; with default config uses **rhizome mat** edge spread (`UseRhizomeSpreadForReeds`). Set `ecologySpreadMode: independent` to opt out. Non–`coopersreed`/`tule`/`papyrus` species do **not** use vanilla land/water variant resolution: your `ecologySpreadBlock` should already be the correct block for the target cell.
- **WaterSurface** — lily-like defaults; with default config uses **floating pad mat** (`UseSurfaceMatSpreadForLilies`). `ecologySpreadMode: surfacemat` or `independent`.
- **UnderwaterColumn** — crowfoot-like numeric defaults; adjust with `ecology*` depth fields.
- **TerrestrialTree** — `ecologyMinSunlight` default 11 if unset.

## Berry traits (v3.0)

`CloneBerryTraits` only applies to vanilla **`game:fruitingbush-wild-*`**. Custom fruiting bushes from other mods are not detected by `IsWildBerryBushBlock`; trait cloning would need a separate hook if you rely on the same BE behavior.

## Example (block type JSON fragment)

```json
{
  "attributes": {
    "ecologyParticipant": true,
    "ecologySpecies": "bluegrass",
    "ecologyHabitat": "Terrestrial",
    "ecologySpreadBlock": "mygrassmod:wildgrass-bluegrass-free",
    "minTemp": 0,
    "maxTemp": 30,
    "minRain": 0.3,
    "maxRain": 0.8,
    "ecologySpreadRate": 0.6
  }
}
```

## Built-in mod compatibility (optional)

When **[Wildgrass](https://mods.vintagestory.at/wildgrass)** or **[Wildgrass Fork](https://mods.vintagestory.at/wildgrassfork)** (`wildgrass:*` blocks) is installed alongside Ecosystem - Flora, JSON patches under `assets/ecosystemflora/patches/wildgrass-*.json`:

| Patch | Purpose |
|-------|---------|
| `wildgrass-ecology.json` | `ecologyParticipant` on **mature growth stages** for all nine species (stages 3–4 where present; bermudagrass stage 3; buffalograss stage 2). Climate/spread from Wildgrass worldgen; `ecologyMeadowHarvest: none` leaves cut/harvest to Wildgrass. |
| `wildgrass-handbook.json` | `ecosystemHandbook` behavior on grass blocktypes (handbook ecology text). |

Patches use **`"side": "server"`** (blocktypes load server-side in VS 1.22+) and **`dependsOn`** for modids `wildgrass` / `wildgrasscontinued` so nothing runs when the grass mod is absent.

No hard dependency on either grass mod. Requires **`EnableThirdPartyParticipants`: `true`** (default).

### Handbook patches (vanilla + third-party)

`handbook-behaviors.json` adds `ecosystemHandbook` via **`addmerge`** on `/behaviors` (or `/behaviorsByType` for tallgrass). On VS **1.22+** many plant blocktypes no longer have a root `behaviors` array — do **not** use `add` on `/behaviors/-`. Reeds and crowfoot paths: `reedpapyrus.json`, `aquatic/watercrowfoot.json` (not legacy `tallplant-*` / flat `aquatic-watercrowfoot` filenames).

## Disable

Set **`EnableThirdPartyParticipants`: `false`** to ignore `ecologyParticipant` on all blocks (only vanilla path rules apply).

## Meadow harvest hooks (C#)

When **`EnableFlowerDrygrass`** is on: **knife/scythe** → drygrass; **flowers** broken with anything else drop the flower **block in the world**; **tallgrass** with anything else is **removed with no drop** (use knife/scythe for drygrass).

Other mods can intercept break harvest:

```csharp
WildFarming.Ecosystem.MeadowHarvestRegistry.Register(args =>
{
    if (!args.BrokenBlock.Code.Path.StartsWith("flower-cornflower")) return MeadowHarvestHandleResult.Pass;
    args.Api.World.SpawnItemEntity(new ItemStack(...), args.Pos);
    args.Api.World.BlockAccessor.SetBlock(strippedBlockId, args.Pos);
    return MeadowHarvestHandleResult.Handled;
});
```

Set **`ecologyMeadowHarvest`: `"delegate"`** on the block type to disable the default whole-plant drop and rely on handlers only. Use **`"none"`** if your mod fully owns drops and block replacement.

**Partial harvest without breaking** (typical herbalism UX): implement interact / block behavior on **use**, transform the block in place, and set `"none"` or `"delegate"` so break does not fight your logic.
