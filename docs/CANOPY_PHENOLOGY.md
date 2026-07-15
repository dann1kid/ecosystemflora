# Canopy foliage — сезонная листва (v3.4)

Per-cell seasonal foliage on **deciduous** `log-grown` / `leavesbranchy` / `leaves-grown` blocks. No trunk-anchor persistence, no `GrowTree`, no disk persistence.

Updated: 2026-06-30 (wildfire guard + orphan prune).

---

## Wildfire interaction (v4.5.4+)

Two lightweight server systems share the existing foliage chunk column pass — no extra world scanner.

### Bud suppression (`CanopyBurnGuard`)

Before ecology places foliage (seasonal bud, tree growth, spread seedling crown):

| Check | Radius | When |
|-------|--------|------|
| Bud source | 3 blocks | Once per bud attempt |
| Target air cell | 2 blocks | Before `SetBlock` |

Active fire = `BlockMaterial.Fire` or block code `fire` / `fire-*`. When fire is detected, the chunk is marked **fire-touched** for orphan prune priority.

### Orphan prune (`CanopyOrphanPrune`)

During the same `ChunkEcologyColumnPass` that runs `CanopySeasonSync`:

1. For each wild `leaves-grown` / `leavesbranchy` cell (not `leaves-placed`), optional bounded **BFS** (default depth **14**) through same-wood foliage asks: is there a `log-grown` trunk?
2. If not → strip to air (land claims respected).
3. Budget: default **64** BFS checks per chunk pass (`OrphanFoliageMaxChecksPerChunkPass`; `0` = unlimited).

**Fire-touched chunks** (`FoliageChunkState.FireTouchedAtHours`):

- Set when fire is seen in the column pass or via burn guard.
- `PendingOrphanPrune` schedules an extra pass (season sync skipped; prune only) after the chunk’s normal monthly sync completes.
- Chunk sync queue sorts fire-touched / pending-prune chunks first for `OrphanFoliageFireChunkHours` (default **48** game hours).

Random/hybrid foliage tick also runs orphan prune on picked cells.

| Component | File |
|-----------|------|
| Fire proximity guard | `CanopyBurnGuard.cs` |
| Orphan BFS + strip | `CanopyOrphanPrune.cs` |
| Per-pass counters | `FoliageChunkPassState.cs` |

**Limits:** does not replace vanilla leaf-pruning for non-ecology blocks; does not strip foliage still connected to a surviving trunk. Pyrogenesis / other fire mods — compatible via vanilla fire blocks.

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
        ↓
CanopyOrphanPrune — optional orphan strip (budgeted BFS)
```

Ecology registration (flowers, trees, **vines**) uses the worker column pipeline when background scan is on. **Mycelium anchors** register on chunk load via `MyceliumChunkRegistrar` (BE scan on main) and then share the same reproduce registry as vines. **Seasonal foliage** uses a separate main-thread pass. Foliage **never** runs on the worker thread.

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
| Fire guard | `CanopyBurnGuard.cs` |
| Orphan prune | `CanopyOrphanPrune.cs` |
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
| `leavesbranchy` | stays (optional peak strip) | bud → adjacent air → `leaves-grown` (until near-crown density cap) |
| `log-grown` | stays | bud → adjacent air → `leavesbranchy` in crown zone (scaffold; denser = fuller leaf dress) |

- Only **orthogonal** neighbors; same `wood`, vacant air, land claims respected.
- Activity + deterministic noise (patchy crown).
- **Density caps** (`MaxBranchyNearLog` / `MaxRegularNearBranchy` per wood profile) stop catch-up when the local crown is already bushy enough.

**Conifers** — no behaviour. **Not** `log-placed` / `leaves-placed`.

---

## Strip policy (`CanopySeasonSync`)

| Period | `leaves-grown` strip |
|--------|----------------------|
| Active autumn (≈ Aug–Nov, when defol activity ≥ ~0.28) | Patchy (~30–55% per pass via deterministic gate) |
| Dec–Feb + winter idle | **Force strip all** regular leaves |
| Mar–Jul (spring / warm Idle) | **No strip** |

Ensures bare skeleton by winter even when chunk sync marks the month complete after a partial autumn pass.

**Anti-wave rules (v4.9+):** temperate bud curves have **no February** and **no July residual bud** (those used to leave→strip→leave). Autumn wins phase ties (`defol >= bud`). Yearly tree aging (`TreeGrowthApplier`) does not place deciduous foliage during autumn or the bare winter window — only trunk height may advance then.

**Fuller spring crowns (v4.8.2+):** chunk sync again grows **`log → leavesbranchy`** in the crown zone during Spring (was missing in Option B “branchy-only dress”, which left thin skeletons). Leaf/branchy catch-up scales and near-crown density caps were raised so trees fill more before summer idle.

**Warm-season leaf keep (v4.8.2+):** summer Idle no longer force-strips (that looked like leaves vanishing in warm weather). Early/mid defol starts later (≈ Aug/Sep), and weak autumn ramps below `MinPatchyAutumnStripActivity` do not drip-strip.

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
| `EnableOrphanFoliagePrune` | `true` | strip wild foliage with no path to log-grown |
| `OrphanFoliageMaxBfsDepth` | `14` | BFS depth when testing foliage support |
| `OrphanFoliageMaxChecksPerChunkPass` | `64` | orphan BFS checks per chunk pass (`0` = unlimited) |
| `OrphanFoliageFireChunkHours` | `48` | prioritize fire-touched chunks for orphan prune (hours; `0` = off) |

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
| v3.8 | Fallen sticks use `SurfacePlacement.TryFindSurfaceCellBelow` (ground/tallgrass surface, not floating) |
| v4.5.4 | `CanopyBurnGuard` — no budding near fire; `CanopyOrphanPrune` in chunk pass; fire-touched chunk priority |
