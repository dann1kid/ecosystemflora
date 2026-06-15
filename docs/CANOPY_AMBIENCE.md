# Canopy ambience — client particles (v3.5)

Optional **client-only** atmosphere under tall deciduous canopy. No server packets, no block changes, no save data.

Updated: 2026-06-14.

---

## Purpose

Block simulation (`CanopySeasonSync`) updates crowns slowly and invisibly. Ambience particles bridge the gap between simulation and **felt** seasonality:

| Season | Effect |
|--------|--------|
| Dec–Feb | off |
| Mar–Apr | rare green motes (bud / spring dust) |
| May–Aug | green motes under canopy |
| Sep | motes + first leaf drift |
| Oct–Nov | falling leaf drift (species-tinted) |
| Live rain | suppressed when `CanopyAmbienceSuppressInRain` |

---

## Architecture

```
CanopyAmbienceClientSystem (game tick, client)
        ↓
CanopyAmbienceSampler — 5-column scan, ≥2 m foliage above feet
        ↓
CanopyAmbienceSeasonCurves — month rates + colours
        ↓
SimpleParticleProperties → IWorldAccessor.SpawnParticles
```

| Component | File |
|-----------|------|
| Client tick + spawn | `src/Client/CanopyAmbienceClientSystem.cs` |
| Canopy detection | `src/Ecosystem/CanopyAmbienceSampler.cs` |
| Season rates / colours | `src/Ecosystem/CanopyAmbienceSeasonCurves.cs` |

Server code is unchanged. Requires `EnableSeasonalFoliage` and `EnableCanopyAmbience`.

---

## Detection

Every `CanopyAmbienceSampleIntervalSeconds` (default 2 s):

1. Scan 5 columns (player + N/E/S/W) upward from `feetY + CanopyAmbienceMinHeightBlocks`.
2. First seasonal foliage block per column = canopy contact.
3. `Density` = fraction of columns with foliage within 2 blocks of lowest canopy layer.
4. Ambience runs only when `Density ≥ 0.2`.

Particles spawn **under** the canopy (`CanopyY − 0.2…1.4`) in a 2–4 block XZ disc around the player.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableCanopyAmbience` | `true` | Master toggle (client) |
| `CanopyAmbienceMinHeightBlocks` | `2` | Min foliage height above feet |
| `CanopyAmbienceMoteRate` | `1` | Green mote rate multiplier |
| `CanopyAmbienceLeafDriftRate` | `1` | Autumn leaf drift multiplier |
| `CanopyAmbienceMaxParticles` | `48` | Soft burst cap per 2 s window |
| `CanopyAmbienceSampleIntervalSeconds` | `2` | Canopy re-sample interval |
| `CanopyAmbienceSuppressInRain` | `true` | Off during live precipitation |

---

## Performance

- One sampler pass per player every 2 s (not per frame).
- Spawn intervals scale inversely with season rate (4–8 s motes, 1.5–3 s drift at peak).
- `Async = true` on particles; `WindAffectednes` for natural drift.
- No per-tree emitters — single player-local system.

---

## Out of scope (v1)

- Reactive bursts on block strip/bud (v2).
- Conifers / non-deciduous wood.
- Server-side particle sync.
- Sound effects.
