# Canopy foliage — сезонная листва (v3.3)

Per-cell seasonal foliage on **deciduous** `log-grown` / `leavesbranchy` / `leaves-grown` blocks. No trunk anchor BFS, no `GrowTree`, no disk persistence.

Updated: 2026-06-14.

---

## Architecture

```
FoliageCellIndex — per-chunk list of cell positions (chunk scan + place/break)
        ↓
FoliageCellScheduler — random N cells/tick near players
        ↓
CanopyFoliageRules.TickCell — local 6-neighbor strip/bud
        ↓
WildCanopySeason + CanopyEcology (phase, activity, rolls)
```

| Component | File |
|-----------|------|
| Per-cell rules | `CanopyFoliageRules.cs` |
| Random-tick scheduler | `FoliageCellScheduler.cs` |
| Chunk index | `FoliageCellIndex.cs` |
| Chunk scan on load | `FoliageColumnScanner.cs` |
| Season curves | `WildCanopySeason.cs`, `CanopyEcology.cs` |
| Block codes | `CanopyBlockHelper.cs` |

---

## Local rules (per tick, per cell)

| Block | Autumn | Spring |
|-------|--------|--------|
| `leaves-grown` | roll → `air` | — |
| `leavesbranchy` | stays | roll → adjacent air → `leaves-grown` |
| `log-grown` | stays | roll → adjacent air → `leavesbranchy` |

- Only **orthogonal** neighbors of the ticking cell are candidates.
- Same `wood`, vacant air, land claims respected.
- Activity + deterministic noise + random roll (patchy crown).

**Conifers** — no behavior attached. **Not** `log-placed` / `leaves-placed`.

---

## Index lifecycle

1. **Chunk load** — column scan adds all foliage cells (`FoliageColumnScanner`).
2. **Place/break** — `EcosystemSystem` updates the index.
3. **Chunk unload** — index for chunk cleared.

No spread-registry requirement.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableSeasonalFoliage` | `true` | master toggle |
| `MaxFoliageCellsTickedPerTick` | `64` | random cells per reproduce tick |
| `FoliageBudgetMs` | `10` | wall-time cap (0 = off) |
| `CanopyActivityScale` | `1` | monthly curve multiplier |
| `CanopyBudMinTemperature` | `5` | °C at cell for spring bud |

Legacy JSON keys `MaxCanopyUpdateOpsPerTick` / `CanopyBudgetMs` still map to the new fields.

---

## v3.2 → v3.3

Removed: `CanopyPhenology`, `CanopySkeletonScanner`, trunk-anchor queues, spread-registry coupling.
