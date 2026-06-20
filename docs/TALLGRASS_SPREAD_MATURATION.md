# Tallgrass spread maturation (v3.9.7+)

Meadow matrix `game:tallgrass-*` spread places **veryshort** offspring. The mod advances **veryshort → short** on a calendar timer (`GrowthHoursMultiplier`), then registers the cell for ecology spread. Vanilla does not grow this chain on its own.

## Behaviour

| Stage | Block | Reproduce registry |
|-------|-------|-------------------|
| Spread offspring | `game:tallgrass-*-veryshort-*` (cover/snow/free preserved) | Not registered |
| After stage timer | `short` … `verytall` | Registered like worldgen turf |
| Worldgen / chunk scan | `short+` | Registered immediately |
| Worldgen / scan | `veryshort` | Queued until mod raises height |
| Player-placed | `short+` | Registered immediately |
| Player-placed | `veryshort` | Queued until mod raises height |
| Eaten | `tallgrass-eaten-*` | Never ecology (unchanged) |

## Timing

- **veryshort → short:** 36 base game hours / `GrowthHoursMultiplier`, min 6 h; scaled slightly by seasonal spread activity (spring faster).
- Further height stages are **not** advanced by the mod (only the spread gate needs `short+`).

## Config

| Key | Default | Purpose |
|-----|---------|---------|
| `EnableTallgrassSpreadMaturation` | `true` | veryshort spread + promotion queue |
| `GrowthHoursMultiplier` | `1` | Also speeds tallgrass veryshort → short |
| `MaxPendingTallgrassPromotionChecksPerTick` | `32` | Budget for promotion checks per reproduce tick |

Turn off for legacy behaviour: spread picks height from local conditions at commit time (`TallgrassSpreadHeight`).

## Code

| Component | File |
|-----------|------|
| Stage hours | `WildTallgrassMaturation.cs` |
| Spread gate + veryshort resolve | `TallgrassSpreadMaturation.cs` |
| Promotion queue + SetBlock advance | `PendingTallgrassPromotion.cs` |
| Height parse / stage advance | `TallgrassSpreadHeight.cs` |
| Spread parent gate | `PlantCodeHelper.IsEcologySpreadParent` |

See also: [`FLOWER_SPREAD_MATURATION.md`](FLOWER_SPREAD_MATURATION.md), [`CONFIGURATION.md`](CONFIGURATION.md).
