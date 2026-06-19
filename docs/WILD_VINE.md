# Wild vine ecology

Vanilla `wildvine-end-*` and `wildvine-tropical-end-*` tips register in the ecology reproduce loop.

**Version:** 3.7.0 · Updated: 2026-06-14.

## Registration

Wild vine tips are discovered in **`ChunkEcologyColumnPass`** when a chunk is scanned (same queue as meadow flora; background worker when `EnableBackgroundRegistrationScan` is on). Tips enter `PendingRegistrationQueue` and then the ecology registry.

**Mycelium** uses the same registry and reproduce tick, but discovery is **`MyceliumChunkRegistrar`** — enumerates vanilla `BlockEntityMycelium` in the loaded chunk column (main thread, short delay after load). Caps still regrow via vanilla rules; ecology handles anchor stress and network spread.

Both habitats run in **chunk-fair spread** when `EnableChunkFairSpread` is on. Spread commits **directly** in the reproduce callback (not via `PendingSpreadQueue` / two-phase placement).

## Spread behaviour

Each reproduce tick (when chance passes):

1. **Extend down** — air below the lowest tip becomes a new end; the former tip becomes `wildvine-section-*`.
2. **Wall capture** — if downward growth fails, scan adjacent vertical faces of the host column (building sides, tree trunks) and place a new end on a vacant cell.

Temperate and tropical vines keep their variant; spread uses vanilla block codes only.

## Config (`ecosystemflora.json`)

| Key | Default | Description |
|-----|---------|-------------|
| `EnableWildVineEcology` | `true` | Register tips and run spread |
| `WildVineWallCaptureRadius` | `4` | Horizontal scan along a wall face |
| `WildVineWallCaptureHeight` | `6` | Vertical scan span |

## Playtest notes

- Watch vine columns on cliff faces and log trunks over several in-game days.
- Break the host block — vanilla removes the vine; registry entry expires on the next tick.
- Tropical vines need warm, wet climate per `WildVineEcology` profile.
