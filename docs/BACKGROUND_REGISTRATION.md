# Background registration (Phase 6.7)

How meadow flora, vines, and trees enter the ecology **reproduce registry** without blocking the main server thread on full chunk column scans.

See also: [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.7, [`CONFIGURATION.md`](CONFIGURATION.md) (registration keys), [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md) (establishing tallgrass from scan hits).

---

## Pipeline overview

```
Chunk load / cyclic rescan / player-priority queue
        │
        ▼
RegistrationScanQueue  (priority + burst near players)
        │
        ▼
Main thread — RegistrationChunkSnapshotBuilder
  • copies block.Id grid into chunk snapshot (budget: MaxRegistrationSnapshotCellsPerTick)
        │
        ▼
BackgroundRegistrationScanner (worker thread pool)
  • ChunkEcologyColumnPass on snapshot (no BlockAccessor)
  • flowers, establishing tallgrass, wild vines, tree saplings
  • foliage index pass optional (sync on main when background scan off)
        │
        ▼
Main thread — ApplyScanResult
  • PendingRegistrationQueue.Enqueue hits
  • PendingTallgrassPromotion for below-target grass
  • FoliageChunkSyncPass when decoupled
        │
        ▼
Main thread — PendingRegistrationQueue.Drain
  • RegisterReproducer (paced: MaxRegistryAppliesPerTick / per-chunk cap)
```

**Hard rule:** `SetBlock` and `RegisterReproducer` always run on the **main thread**. Workers only classify from snapshots.

---

## What gets registered

| Discovery | Worker output | Main-thread follow-up |
|-----------|---------------|------------------------|
| Meadow flower (mature) | `flowerHits` | `PendingRegistrationQueue` → registry |
| Tallgrass below target height | `establishingTallgrassHits` | `PendingTallgrassPromotion` (growth timer) |
| Tallgrass at spread-ready height | `flowerHits` | registry when `IsReadyToRegister` |
| Wild vine tip | `vineHits` | registry at anchor |
| Tree / ferntree sapling | `treeHits` | registry or pending sapling queue |
| Mycelium anchor | — | **`MyceliumChunkRegistrar`** on chunk load (BE scan, main thread) |

**Cyclic flora** (`EnableCyclicFloraDiscovery`): `CyclicFloraScanner` re-queues loaded chunks so grass/flowers missed at first pass can still register.

---

## Priority and burst

| Mechanism | Config | Effect |
|-----------|--------|--------|
| Player-vicinity priority | `EnablePlayerPriorityRegistration`, `PlayerRegistrationPriorityRadiusBlocks` | Chunks near players dequeue first |
| Load burst | `EnableBurstRegistrationNearPlayers`, `BurstRegistrationBudgetMs` | Extra scan budget after chunk load near players |
| Per-tick caps | `MaxRegistryAppliesPerTick`, `MaxRegistryAppliesPerChunkPerTick`, `MaxPriorityRegistryAppliesPerTick` | Smooth registry inserts |

When `EnableBackgroundRegistrationScan` is **off**, the same column pass runs **synchronously** on the main thread during chunk scan (legacy path).

---

## Worker pool

| Key | Default | Notes |
|-----|---------|-------|
| `EnableBackgroundRegistrationScan` | **true** | Snapshot → worker classify |
| `RegistrationWorkerCount` | **0** (= half CPU cores, max 8) | Parallel classify workers |
| `MaxRegistrationSnapshotCellsPerTick` | see CONFIGURATION | Main-thread snapshot copy budget |

Foliage phenology: with background scan on, **`FoliageChunkSyncPass`** runs on main after worker results; worker pass sets `SyncFoliage: false` on the column pass request.

---

## Related spread pipeline (separate)

Registration discovers **who can spread**. Actual spread scoring may use **`BackgroundSpreadPipeline`** when `EnableBackgroundSpreadSolve` is on — see [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.8+.

---

## Code map

| Component | File |
|-----------|------|
| Queue + priority | `RegistrationScanQueue.cs` |
| Snapshot build | `RegistrationChunkSnapshotBuilder.cs` |
| Worker submit/take | `BackgroundRegistrationScanner.cs` |
| Orchestration | `BackgroundRegistrationPipeline.cs` |
| Column classify | `ChunkEcologyColumnPass.cs` |
| Paced registry apply | `PendingRegistrationQueue.cs` |
| Top-down flower/tallgrass find | `RegistrationColumnFlowerScan.cs` |
| Live rescan | `CyclicFloraScanner.cs` |

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| Flowers/grass never register | `EcosystemEnabled`, chunk scan tick, `EnableCyclicFloraDiscovery`, registry caps not exhausted |
| Slow registry near player | Raise `MaxPriorityRegistryAppliesPerTick` or burst budget |
| Worker idle, main spikes | Lower `MaxRegistrationSnapshotCellsPerTick` or worker count |
| Tallgrass registers but does not spread | `EnableTallgrassSpreadMaturation`, height vs `MinSpreadStageIndex` — [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md) |
