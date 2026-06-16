# Wild vine ecology

Vanilla `wildvine-end-*` and `wildvine-tropical-end-*` tips register in the ecology reproduce loop.

**Version:** 3.7.0 · Updated: 2026-06-14.

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
