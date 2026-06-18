# Canopy foliage — сезонная листва (v3.4)

Per-cell seasonal foliage on **deciduous** `log-grown` / `leavesbranchy` / `leaves-grown` blocks. No trunk anchor BFS, no `GrowTree`, no disk persistence.

Updated: 2026-06-18.

---

## Architecture

**Chunk mode (default)** — two paths when `EnableBackgroundRegistrationScan` is on (v3.8):

```
Chunk load / month change / block place-break
        ↓
FoliageCellScheduler.ScheduleChunkSync → chunk state pending
        ↓
On chunk-scan tick (main thread):
  FoliageCellScheduler.ProcessChunkSyncBatch → FoliageChunkSyncPass
        ↓
CanopySeasonSync.TrySyncCell — strip/bud per foliage block
```

Ecology registration (flowers, trees, vines) uses a **separate** pipeline: snapshot on main → classify on worker → paced `RegisterReproducer` on main. Foliage **never** runs on the worker thread.

**Legacy / background scan off** — registration column pass may still run `SyncFoliage` inline in `ChunkEcologyColumnPass` during `TryRunRegistrationPass`.

```
Chunk load / month change / block place-break
        ↓
PendingChunkScan queue → ChunkEcologyColumnPass (unified column descent, SyncFoliage when inline)
        ↓
CanopySeasonSync.TrySyncCell
        ↓
WildCanopySeason + CanopyEcology (phase, activity, gates)
```

| Component | File |
|-----------|------|
| Per-cell rules | `CanopyFoliageRules.cs` |
| Chunk season sync | `CanopySeasonSync.cs` |
| Unified column pass | `ChunkEcologyColumnPass.cs`, `ChunkColumnWalker.cs` |
| Foliage-only chunk pass | `FoliageChunkSyncPass.cs` |
| Scheduler / chunk state | `FoliageCellScheduler.cs`, `FoliageChunkState.cs` |
| Season key | `FoliageSeasonKey.cs` |
| Season curves | `WildCanopySeason.cs`, `CanopyEcology.cs` |
| Block codes | `CanopyBlockHelper.cs` |

**Sync modes** (`FoliageSyncMode`):

| Mode | Behaviour |
|------|-----------|
| `chunk` (default) | One column pass per chunk per game month; no random tick |
| `hybrid` | Chunk sync + random tick (`MaxFoliageCellsTickedPerTick` > 0) |
| `random` | Legacy v3.3 random cell tick only |

---

## Local rules (per cell)

| Block | Autumn | Spring |
|-------|--------|--------|
| `leaves-grown` | strip → `air` | — |
| `leavesbranchy` | stays (optional peak strip) | bud → adjacent air → `leaves-grown` |
| `log-grown` | stays | bud → adjacent air → `leavesbranchy` |

- Only **orthogonal** neighbors; same `wood`, vacant air, land claims respected.
- Activity + deterministic noise (patchy crown).

**Conifers** — no behaviour. **Not** `log-placed` / `leaves-placed`.

---

## Strip policy (`CanopySeasonSync`)

| Period | `leaves-grown` strip |
|--------|----------------------|
| Oct–Nov (active autumn) | Patchy (~30–55% per pass via deterministic gate) |
| Dec–Feb + winter idle | **Force strip all** regular leaves |
| Mar–Sep | No autumn strip |

Ensures bare skeleton by winter even when chunk sync marks the month complete after a partial autumn pass.

---

## Index lifecycle

1. **Chunk column pass** — scans foliage during rain-heightmap descent (chunk/hybrid modes).
2. **Place/break** — invalidates chunk sync state; re-queues scan.
3. **Month change** — `FoliageSeasonKey` invalidates all tracked chunks.
4. **Chunk unload** — chunk state cleared.

Random/hybrid modes also maintain `FoliageCellIndex` for per-tick picks.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableSeasonalFoliage` | `true` | master toggle |
| `FoliageSyncMode` | `chunk` | `chunk` / `hybrid` / `random` |
| `FoliageChunkSyncBudgetMs` | `12` | wall-time per column pass slice |
| `FoliageChunkWorkPerTick` | `4` | chunk columns resumed per tick |
| `MaxFoliageCellsTickedPerTick` | `0` | random cells/tick (hybrid/random; 0 = off) |
| `FoliageBudgetMs` | `10` | random-tick wall cap |
| `CanopyActivityScale` | `1` | monthly curve multiplier |
| `CanopyBudMinTemperature` | `5` | °C at cell for spring bud |
| `FoliagePeakAutumnBranchyStripActivity` | `0.35` | peak autumn activity before partial branchy strip (0 = keep all branchy) |
| `EnableCanopyFallenSticks` | `true` | drop `loosestick-free` when branchy foliage strips |
| `CanopyFallenStickChance` | `0.42` | stick drop chance scale at peak autumn |
| `EnableSpringBranchyAgeBoost` | `true` | older trees bud more branchy leaves in spring |
| `SpringBranchyAgeBoostYearsToMax` | `60` | calendar years to max spring branch boost |
| `SpringBranchyAgeBoostMax` | `1.5` | max spring branchy bud multiplier from age |
| `FoliageRestoreBareSkeleton` | `true` | Winter crown repair only (not lower trunk; not autumn) |

Legacy keys `MaxCanopyUpdateOpsPerTick` / `CanopyBudgetMs` still map to foliage fields.

---

## Client ambience (v3.5)

Optional **client-only** particles under tall canopy — no server cost. See [`CANOPY_AMBIENCE.md`](CANOPY_AMBIENCE.md).

Toggle: `EnableCanopyAmbience` (requires `EnableSeasonalFoliage`).

---

## History

| Version | Change |
|---------|--------|
| v3.2 | Per-cell random tick phenology |
| v3.3 | `FoliageCellIndex`, removed trunk-anchor BFS |
| v3.4 | Chunk-sync + unified `ChunkEcologyColumnPass` |
| v3.4.1 | Winter force-strip all `leaves-grown` (Dec–Feb) |
| v3.5 | Client canopy ambience particles |
| v3.8 | Background registration decoupled; `ProcessChunkSyncBatch` + `FoliageChunkSyncPass` on main when worker scan enabled |
