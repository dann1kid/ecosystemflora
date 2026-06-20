# Tallgrass spread maturation (v3.9.7)

Meadow matrix `game:tallgrass-*` spread places **veryshort** offspring; vanilla transient growth raises height. Ecology spread registration waits until height is **short or taller** (not veryshort or eaten).

## Behaviour

| Stage | Block | Reproduce registry |
|-------|-------|-------------------|
| Spread offspring | `game:tallgrass-*-veryshort-*` (cover/snow/free preserved) | Not registered |
| After vanilla growth | `short` … `verytall` | Registered like worldgen turf |
| Worldgen / chunk scan | `short+` | Registered immediately |
| Worldgen / scan | `veryshort` | Queued until growth |
| Player-placed | `short+` | Registered immediately |
| Player-placed | `veryshort` | Queued until growth |
| Eaten | `tallgrass-eaten-*` | Never ecology (unchanged) |

Height growth uses vanilla **BlockEntityTransient** (or random-tick grass mods); this mod does not advance stages on a timer.

## Config

| Key | Default | Purpose |
|-----|---------|---------|
| `EnableTallgrassSpreadMaturation` | `true` | veryshort spread + promotion queue |
| `MaxPendingTallgrassPromotionChecksPerTick` | `32` | Budget for promotion checks per reproduce tick |

Turn off for legacy behaviour: spread picks height from local conditions at commit time (`TallgrassSpreadHeight`).

## Code

| Component | File |
|-----------|------|
| Spread gate + veryshort resolve | `TallgrassSpreadMaturation.cs` |
| Promotion queue | `PendingTallgrassPromotion.cs` |
| Height parse / veryshort resolve | `TallgrassSpreadHeight.cs` |
| Spread parent gate | `PlantCodeHelper.IsEcologySpreadParent` |

See also: [`FLOWER_SPREAD_MATURATION.md`](FLOWER_SPREAD_MATURATION.md), [`CONFIGURATION.md`](CONFIGURATION.md).
