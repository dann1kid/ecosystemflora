# Wild tree maturation (v3.6)

Registered wild trees (`log-grown` trunk base in the ecology registry) **grow once per game year** in the main reproduce tick (round-robin over the registry, same scope as spread/stress): taller trunk (`log-grown`) and wider crown (`leavesbranchy` / `leaves-grown`).

Updated: 2026-06-14 (lifecycle + senescence remains).

In-game handbook (en/ru): **Overview**, **Trees**, **Seasonal Canopy**, **Ecology Inspect (I)**, **Configuration Guide**.

---

## Lifecycle (end-to-end)

Full path for a wild tree under default config (`EnableTreeAging`, `EnableTreeSenescence`, `EnableTreeSenescenceRemains`):

```
sapling (spread) → vanilla treegen → log-grown registered (age 0)
  → each game year: age++, growth, sapling spread
  → calendar age ≥ species lifespan: senescence (4 years)
  → stump + fallen logs (not log-grown) → gap refilled by nearby living trees
```

| Stage | What happens |
|-------|----------------|
| **1. Seed** | Mature `log-grown` trunk places `sapling-{wood}-free` on open soil (climate, light, spacing). Nov–Feb spread off; no ice/snow placement. |
| **2. Grow up** | **Vanilla treegen** from sapling to adult — mod only planted the sapling. |
| **3. Register** | Lowest `log-grown` in the column enters the ecology registry at **`TreeAgeYears = 0`** (even worldgen giants). |
| **4. Active life** | Each **game year** in loaded chunks: age +1; may add trunk/crown blocks; continues **sapling spread** from trunk base. Trees do **not** die from niche stress. |
| **5. Seasonal dress** | *(Parallel, optional)* Deciduous crowns follow autumn/winter/spring (`EnableSeasonalFoliage`) — separate from aging/senescence. |
| **6. Senescence** | When age ≥ species lifespan (oak ~120 y, birch ~90, redwood ~140 — see `WildTreeGrowthProfiles.cs`), **one stage per game year**: crown leaves → branchy skeleton → dry snag (`TreeSenescenceSnagBlocks`, default 3) → collapse. Spread and growth stop from year 1; spring bud blocked while senescing. |
| **7. Remains** | Final year: snag removed; **stump** `log-{wood}-ud` at base + up to **`TreeSenescenceFallenLogCount`** horizontal `debarkedlog-*` nearby. Vanilla choppable blocks — **not** re-registered; ecology record cleared. Mycelium/soil cascade as on player tree cut. |
| **8. Succession** | No sapling burst on death — **neighbouring mature trees** fill the gap through normal spread. |

**Other exits:** player fells trunk (registry + saved age removed); senescence/growth/spread blocked inside land claims (phase retries next year). Calendar age persists in savegame moddata, not on blocks — see [Persistence](#persistence--server-restart).

Toggle **`EnableTreeSenescenceRemains`** off to skip stump/logs (bare air on final year). Details per phase: [Senescence](#senescence-phased-death).

---

## Two axes (size vs age)

| Axis | Meaning | Used for |
|------|---------|----------|
| **Structure** | Trunk blocks + crown radius (live measure) | Growth pacing, inspect size index |
| **Calendar age** | Years since ecology registration (`TreeAgeYears`) | Senescence death — **not** inferred from size |

Worldgen trees register at **age 0** even when already tall. They will **not** senesce just because they look mature. Future death uses calendar age (+ vitality), not structure index.

---

## Behaviour

| When | What |
|------|------|
| **Registration** | `TreeAgeYears = 0`; structure measured live |
| **Each game year** | `TreeAgeYears++`, then growth **or** next senescence stage if age ≥ horizon |
| **Young / below reference** | Faster growth, mostly upward |
| **Above typical mature** | Growth slows but continues (rare ops above ~125% size index) |
| **Physical limits** | Map height, crown scan radius 14, vacancy / claims |

No custom blocks, no save BE — only vanilla **grown** codes.

---

## Size index (inspect only)

Compared to **typical worldgen mature** per species (reference, not a cap):

```
size index = 55% × (trunk / ref trunk) + 45% × (crown / ref crown)
```

Example oak reference ~14 trunk / 5 crown — a worldgen oak near that reads **~100%**; an ancient taller tree can read **150%+**.

Inspect:

- `Tree age: 3 / 120 years (since ecology registration)`
- `Structure: trunk 14 blocks, crown radius 5 (100% of typical mature ~14/5)`

### Senescence (phased death)

After calendar age reaches `SenescenceAgeYears`, one stage advances each game year (while the trunk remains registered):

| Year after lifespan | Phase | World effect |
|---------------------|-------|--------------|
| 1 | Declining | All `leaves-grown` removed; sapling spread and structure growth stop; seasonal bud blocked |
| 2 | Dead crown | All `leavesbranchy` removed — bare branchy skeleton gone |
| 3 | Snag | Branches and upper trunk cleared; `TreeSenescenceSnagBlocks` (default 3) of `log-grown` remain |
| 4 | Removed | Snag collapses: **stump** (`log-{wood}`) at base + up to **3 fallen logs** nearby; registry cleared |

Blocked inside land claims (phase retries next year).

---

## Spread (saplings)

| Topic | Behaviour |
|-------|-----------|
| **Registry origin** | Lowest `log-grown` in the column (`GetTreeTrunkBase`) — one entry per tree |
| **Spread source** | Horizontal search from **trunk base** only; upper trunk blocks are not separate parents |
| **Inspect registry** | Any `log-grown` on the tree resolves to the same base entry (`TryGetReproducer`) |

---

## Inspect (I) on trees

| Topic | Behaviour |
|-------|-----------|
| **Target** | Any trunk `log-grown` block shows the same tree profile |
| **Live registry** | Resolved via trunk base (registered, age, spread timer, maturation lines) |
| **Climate / soil / niche** | Sampled at **trunk base** (ground under roots), not the log block directly below the clicked voxel — avoids false “bad soil on log” mid-trunk |
| **Crown radius** | Branchy skeleton only (not `leaves-grown`); BFS from trunk; measure cap 9 blocks horizontal |

---

## Architecture

```
ReproducerEntry (TerrestrialTree)
  TreeAgeYears — calendar, from 0 at register
        ↓
TreeGrowthScheduler — once/year, round-robin over registry (world-wide)
        ↓
TreeGrowthApplier — log up / branchy out / leaf fill (no hard cap)
        ↓
CanopyBlockHelper block resolve + land claims
```

| Component | File |
|-----------|------|
| Reference size + senescence horizon | `WildTreeGrowthProfiles.cs` |
| Size index math | `TreeGrowthTargets.cs` |
| Measure trunk / crown | `TreeStructureProbe.cs` |
| Block placement | `TreeGrowthApplier.cs` |
| Tick scheduling | `TreeGrowthScheduler.cs` |
| Calendar age save/load | `TreeCalendarAgeStore.cs` |
| Senescence death | `TreeSenescence.cs` — phased decline after lifespan (see below) |

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableTreeAging` | `true` | Master toggle |
| `EnableTreeSenescence` | `true` | Phased natural death when calendar age ≥ lifespan |
| `EnableTreeSenescenceRemains` | `true` | Final year leaves vanilla stump + fallen logs instead of bare air |
| `TreeSenescenceSnagBlocks` | `3` | Trunk blocks left during snag phase (year 3) |
| `TreeSenescenceFallenLogCount` | `3` | Horizontal debarked logs on ground near stump (0 = stump only) |
| `MaxTreeGrowthAttemptsPerTick` | `6` | Trees advanced per reproduce tick (2 s) |
| `TreeGrowthActivityScale` | `1` | Growth pace (>1 = faster relative to reference) |

**Scope:** tree aging uses the same loaded-chunk registry as spread. `OnlyActivateNearPlayers` defaults to **false** (all registered plants in loaded chunks). Set **true** only for local playtest / perf (limits spread, stress, trees, and chunk scans to `PlayerActivationRadiusBlocks`).

---

## Persistence & server restart

Calendar age is **not** stored on blocks — only in savegame moddata (`TreeCalendarAgeStore`, key `ecosystemflora-tree-calendar-age-v1`).

| Event | What happens |
|-------|----------------|
| **Each game year tick** | `Capture` writes `TreeAgeYears` + `LastTreeGrowthYear` to in-memory store |
| **World save** | `SyncFromRegistry` then `StoreData` — all live registry trees flushed to disk |
| **Server restart / load save** | `SaveGameLoaded` → store loaded; **registry empty** until chunks scan |
| **Chunk loads, trunk registers** | `TryRestore(trunk base, wood)` → age and last year applied |
| **Wood mismatch at same coords** | Restore skipped → age **0** (new tree) |
| **Tree cut or senescence death** | Record removed from store |
| **Crash without save** | Age reverts to last saved snapshot |

Between restart and chunk re-scan, inspect **(I)** shows no live tree entry — age sits in store until registration. `LastTreeGrowthYear` prevents double-aging in the same game year after reload.

---

## Limits (v1)

- **Senescence death** — when `TreeAgeYears >= SenescenceAgeYears`, one stage per game year: (1) strip `leaves-grown`, stop spread/growth; (2) strip `leavesbranchy`; (3) reduce to snag (`TreeSenescenceSnagBlocks`); (4) collapse snag to **vanilla stump + fallen logs** (`EnableTreeSenescenceRemains`, `TreeSenescenceFallenLogCount`). Stumps are not `log-grown` — ecology does not re-register them. Toggle off remains for instant air removal. Blocked inside land claims. Seasonal spring bud is blocked while senescing.
- Calendar age **persists in savegame moddata** (`TreeCalendarAgeStore`).
- Crown radius for inspect: branchy skeleton only; measure cap 9 blocks.
