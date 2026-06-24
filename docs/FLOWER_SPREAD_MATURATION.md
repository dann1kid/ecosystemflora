# Flower spread maturation

Meadow flowers spread as a **small juvenile block**, then mature into the vanilla parent after a calendar delay. Post-spread cooldown pauses the parent after each spread attempt (including failed chance rolls).

## Behaviour

| Stage | Block | Reproduce registry |
|-------|-------|-------------------|
| Spread offspring | `ecosystemflora:juvenile-flower-{species}-free` | Not registered |
| After maturation | Phenology **vegetative** phase block or vanilla bloom (see [`FLOWER_PHENOLOGY.md`](FLOWER_PHENOLOGY.md)) | Registered |
| Worldgen / chunk scan | Mature vanilla block | Registered immediately (unchanged) |
| Player-placed | Mature vanilla block | Registered immediately (unchanged) |

## Species coverage

All **23** ecology flower species from `EcologyFlowerSpecies.All`, plus grass colonizer **`redtopgrass`** (`EcologyGrassColonizerSpecies` — uses `flower-*` paths but competes with tallgrass).

Per-species maturation/cooldown hours: `WildFlowerMaturation.cs` (`BySpecies` overrides + tier defaults for unlisted flowers).

**Variant-aware maturation:** lupine, croton, and rafflesia inherit the **parent block code** at maturation (color/size variant preserved). Fallback codes when parent is unknown:

| Species | Default mature code |
|---------|---------------------|
| `rafflesiared` | `game:flower-rafflesia-red-free` |
| `rafflesiabrown` | `game:flower-rafflesia-brown-free` |
| `croton` | `game:flower-croton-small-crimson-green-free` |

Juvenile assets for croton/rafflesia use vanilla **croton/rafflesia shapes** (not meadow `petal/*` textures).

## Timing

- **Maturation:** `speciesBaseMaturationHours / GrowthHoursMultiplier` (min 6 game hours; season can shorten).
- **Post-spread cooldown:** after a spread attempt that ran placement logic (sync, two-phase commit, or background no-winners). Applied on **commit**, not when a background job is queued.
- **Failed chance roll cooldown:** ~3 game hours (capped at 4 h) when the spread **chance roll fails** before placement — soft anti-spam for event wake.
- **Event wake:** may pull `NextAttemptHours` forward to `now + EventWakeRetryHours` when spawn cooldown has elapsed; wake never bypasses `NextSpawnAllowedAtHours`.

## Config

| Key | Default | Purpose |
|-----|---------|---------|
| `EnableFlowerSpreadMaturation` | `true` | Juvenile spread + pending maturation queue |
| `EnableFlowerSpreadAttemptCooldown` | `true` | Parent pause after spread attempts (independent of juvenile) |
| `FlowerSpreadCooldownHoursMultiplier` | `1` | Higher = shorter post-spread and failed-roll pauses |
| `GrowthHoursMultiplier` | `1` | Higher = faster juvenile → mature only |
| `MaxPendingFlowerMaturationChecksPerTick` | `32` | Budget for maturation commits per reproduce tick |

## Inspect (I)

| Target | Lines |
|--------|-------|
| Juvenile seedling | Not registered; establishing; mature in ~X days (from maturation queue) |
| Registered parent | Registered; next spread; optional **spread retry cooldown** when `NextSpawnAllowedAtHours` is active |

Lang keys: `inspect-line-flower-establishing`, `inspect-line-flower-maturing`, `inspect-line-spawn-cooldown`.

## Assets

Juvenile blocktypes under `assets/ecosystemflora/blocktypes/plant/`. Meadow flowers reuse vanilla flower shapes/textures from `game:blocktypes/plant/flower.json`. Regenerate batch assets: `tools/GenerateJuvenileFlowerBlocks.ps1`.

Missing juvenile blocktype: one **Notification** per species per session (extra **Warning** when `VerboseLogging`).

## Code

| Component | File |
|-----------|------|
| Species hours + cooldown table | `WildFlowerMaturation.cs` |
| Grass colonizer list | `EcologyGrassColonizerSpecies.cs` |
| Juvenile block codes | `FlowerJuvenileBlocks.cs` |
| Maturation queue | `PendingFlowerMaturation.cs` |
| Spread block resolution | `FlowerSpreadMaturation.cs` |
| Cooldown deferral (background) | `FlowerSpreadCooldownTiming.cs` |
| Parent cooldown field | `ReproducerEntry.NextSpawnAllowedAtHours` |

See also: [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md), [`GAPS.md`](GAPS.md) §1, [`CONFIGURATION.md`](CONFIGURATION.md).
