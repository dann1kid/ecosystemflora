# Tallgrass spread maturation (v3.9.7+)

Meadow matrix `game:tallgrass-*` spread places **veryshort** offspring. The mod advances height on a calendar timer (`GrowthHoursMultiplier`). Ecology **spread** opens at **half the environment target height**; promotion continues until **full target**.

## Behaviour

| Stage | Block | Reproduce registry |
|-------|-------|-------------------|
| Spread offspring | `game:tallgrass-*-veryshort-*` (cover/snow/free preserved) | Not registered |
| Establishing | below environment **target** height | Promotion queue: one stage per timer tick |
| At **half target** (rounded up) | e.g. target `verytall` (5) → spread from index 3 (`medium`) | Registered; may spread |
| At **full target** | from `TallgrassSpreadHeight.PickTargetStageIndex` | Promotion stops; full meadow height |
| Worldgen / chunk scan | below half target | Promotion queue only |
| Worldgen / scan | at or above half target | Registered via pending queue |
| Player-placed | at or above half target | Registered when ready |
| Player-placed | below half target | Promotion queue |
| Eaten | `tallgrass-eaten-*` | Never ecology (unchanged) |

Target height uses the same scoring as spread height selection (sun, forest, fertility, rain, niche light/moisture, season). Each cell keeps a stable target from world position hash.

**Half-target rule:** `MinSpreadStageIndex(target) = (target + 1) / 2` (integer). Example: target index 5 (`verytall`) → spread from index 3 (`medium`).

## Timing

- **Each stage step:** 36 base game hours / `GrowthHoursMultiplier`, min 6 h; scaled slightly by seasonal spread activity (spring faster).
- Growth continues until current stage index reaches the **full target**, not only until spread opens.
- **Promotion fairness:** `PendingTallgrassPromotion` round-robins due cells; not-due entries do not burn `MaxPendingTallgrassPromotionChecksPerTick`. Timeout (~14 days) applies only after due work keeps failing (claim / SetBlock), not when the queue was simply backed up.
- **Recovery:** cyclic flora discovery re-queues tallgrass below target even if it is already in the spread registry (half-target register). Trample height retreat also re-queues.

## Registration pipeline (3.9+)

See **[`BACKGROUND_REGISTRATION.md`](BACKGROUND_REGISTRATION.md)** for the full snapshot → worker → `PendingRegistrationQueue` flow.

1. **Main thread:** copy block ids into chunk snapshot.
2. **Worker(s):** `RegistrationWorkerCount` threads classify columns (`ChunkEcologyColumnPass`).
3. **Spread (optional):** `EnableBackgroundSpreadSolve` (default **on**) — worker scores terrestrial/mat/crowfoot spread from `SpreadSolveCell` snapshots; `SetBlock` on main via `PendingSpreadQueue`.
4. **Main thread:** `PendingRegistrationQueue.Drain` applies `RegisterReproducer`; establishing tallgrass → `PendingTallgrassPromotion`.

## Config

| Key | Default | Purpose |
|-----|---------|---------|
| `EnableTallgrassSpreadMaturation` | `true` | staged establishment + veryshort spread |
| `GrowthHoursMultiplier` | `1` | stage timer speed |
| `MaxPendingTallgrassPromotionChecksPerTick` | `32` | promotion checks per reproduce tick |
| `RegistrationWorkerCount` | `0` (= half CPU cores) | background classify workers |
| `EnableBackgroundRegistrationScan` | `true` | worker column classify from snapshot |
| `EnableBackgroundSpreadSolve` | `true` | worker spread scoring (terrestrial, mat, crowfoot; two-phase) |
| `EnableCyclicFloraDiscovery` | `true` | live rescan registers missed grass/flowers after load |
| `MaxRegistryAppliesPerChunkPerTick` | `256` | registry inserts per chunk per scan tick |

Turn off for legacy behaviour: spread picks height from local conditions at commit time (`TallgrassSpreadHeight`).

## Code

| Component | File |
|-----------|------|
| Stage hours | `WildTallgrassMaturation.cs` |
| Spread gate + veryshort resolve | `TallgrassSpreadMaturation.cs` |
| Half-target + full-target logic | `TallgrassEstablishment.cs`, `TallgrassSpreadHeight.MinSpreadStageIndex` |
| Promotion queue + SetBlock advance | `PendingTallgrassPromotion.cs` |
| Height parse / stage advance | `TallgrassSpreadHeight.cs` |
| Spread parent gate | `PlantCodeHelper.IsEcologySpreadParent` |

See also: [`FLOWER_SPREAD_MATURATION.md`](FLOWER_SPREAD_MATURATION.md), [`BACKGROUND_REGISTRATION.md`](BACKGROUND_REGISTRATION.md), [`CONFIGURATION.md`](CONFIGURATION.md).
