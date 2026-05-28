# Third-party blocks as ecosystem participants (v3.1)

**Sample content mod:** [`examples/ecologysample-mynewplant/`](../examples/ecologysample-mynewplant/README.md) in this tree (also publishable as a **standalone public repo** — see `PUBLISHING.md` there). Three blocktypes, vanilla texture placeholders, no C#.

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

## Disable

Set **`EnableThirdPartyParticipants`: `false`** to ignore `ecologyParticipant` on all blocks (only vanilla path rules apply).
