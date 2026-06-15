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

Particles spawn **inside** random `leavesbranchy` / `leaves-grown` voxels within the client **view distance** (`Settings → viewDistance`). Density follows the vanilla **Particles** slider (`particleLevel`); off when `AmbientParticles` is disabled.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableCanopyAmbience` | `true` | Master toggle (client) |
| `CanopyAmbienceMinHeightBlocks` | `2` | Min foliage height above feet |
| `CanopyAmbienceMoteRate` | `1` | Green mote rate multiplier |
| `CanopyAmbienceLeafDriftRate` | `1` | Autumn leaf drift multiplier |
| `CanopyAmbienceSampleIntervalSeconds` | `2` | Canopy re-sample interval |
| `CanopyAmbienceSuppressInRain` | `true` | Off during live precipitation |

---

## Performance

- One sampler pass per player every 2 s (not per frame).
- Spawn intervals scale inversely with season rate (4–8 s motes, 1.5–3 s drift at peak).
- Respects client `particleLevel` and `AmbientParticles`; no mod-side burst cap.
- `Async = true` on particles; `WindAffectednes` for natural drift.
- Leaf drift: **`CanopyLeafVoxelParticleProps`** — textured **Cube** from vanilla `leaves-grown-{wood}`; **`CanopyAmbienceWind`** reads `GlobalConstants.CurrentWindSpeedClient`.
- **Calm** (`Strength &lt; 0.12`): gravity ~0.66, SINUS flutter on X/Z (distinct frequencies), CLAMPEDPOSITIVESINUS on Y for glide stalls, no wind carry.
- **Windy**: lower gravity, gust along wind vector, same flutter axes (no COSIN pair), `ParentVelocityWeight` for drift.
- No per-tree emitters — single player-local system.

---

## Out of scope (v1)

- Reactive bursts on block strip/bud (v2).
- Conifers / non-deciduous wood.
- Server-side particle sync.
- Sound effects.
