# Flower spread maturation (v3.9.6)

Colonizer meadow flowers spread as a **small juvenile block**, then mature into the vanilla parent after a calendar delay. This keeps cell competition and succession but avoids “instant adult meadow” after harvest or spring spread bursts.

## Behaviour

| Stage | Block | Reproduce registry |
|-------|-------|-------------------|
| Spread offspring | `ecosystemflora:juvenile-flower-{species}-free` | Not registered |
| After maturation | `game:flower-{species}-free` | Registered like worldgen flora |
| Worldgen / chunk scan | Mature vanilla block | Registered immediately (unchanged) |
| Player-placed | Mature vanilla block | Registered immediately (unchanged) |

## Species (initial rollout)

Fast colonizers + woad: `cowparsley`, `horsetail`, `mugwort`, `lupine`, `woad`, `redtopgrass`, `heather`, `westerngorse`.

## Timing

- **Maturation:** `speciesBaseMaturationHours / GrowthHoursMultiplier`, scaled slightly by seasonal spread activity (spring faster).
- **Post-spawn cooldown:** parent cannot spawn again until `postSpawnCooldownHours` after a **successful** offspring commit (in addition to normal spread interval).
- **Event wake:** may pull `NextAttemptHours` forward to `now + 6` game hours when spawn cooldown has elapsed; wake never bypasses `NextSpawnAllowedAtHours`.

## Config

| Key | Default | Purpose |
|-----|---------|---------|
| `EnableFlowerSpreadMaturation` | `true` | Juvenile spread + pending maturation queue |
| `GrowthHoursMultiplier` | `1` | Higher = faster juvenile → mature |
| `MaxPendingFlowerMaturationChecksPerTick` | `32` | Budget for maturation commits per reproduce tick |

## Assets

Juvenile blocktypes under `assets/ecosystemflora/blocktypes/plant/` reuse **vanilla flower shapes** (`1patch-3faces-24x24`, `1patch-cross-24x24`, `3patches-3faces-24x24`, `lupine/one-plant`) and **petal/stem texture paths** from `game:blocktypes/plant/flower.json` — not per-species shape folders.

## Code

| Component | File |
|-----------|------|
| Species hours + cooldown table | `WildFlowerMaturation.cs` |
| Juvenile block codes | `FlowerJuvenileBlocks.cs` |
| Maturation queue | `PendingFlowerMaturation.cs` |
| Spread block resolution | `FlowerSpreadMaturation.cs` |
| Parent cooldown field | `ReproducerEntry.NextSpawnAllowedAtHours` |

See also: [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md) (meadow matrix), [`GAPS.md`](GAPS.md) §1, [`CONFIGURATION.md`](CONFIGURATION.md).
