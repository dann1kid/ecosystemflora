# Wild tree maturation (v3.6)

Registered wild trees (`log-grown` trunk base in the ecology registry) **grow once per game year** near active players: taller trunk (`log-grown`) and wider crown (`leavesbranchy` / `leaves-grown`).

Updated: 2026-06-14.

---

## Two axes (size vs age)

| Axis | Meaning | Used for |
|------|---------|----------|
| **Structure** | Trunk blocks + crown radius (live measure) | Growth pacing, inspect size index |
| **Calendar age** | Years since ecology registration (`TreeAgeYears`) | Future senescence / death — **not** inferred from size |

Worldgen trees register at **age 0** even when already tall. They will **not** senesce just because they look mature. Future death uses calendar age (+ vitality), not structure index.

---

## Behaviour

| When | What |
|------|------|
| **Registration** | `TreeAgeYears = 0`; structure measured live |
| **Each game year** | `TreeAgeYears++`, then 0–2 block placements (no hard size cap) |
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

`SenescenceAgeYears` (120 oak) is the **future** calendar horizon for old-age decline, not current size.

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
TreeGrowthScheduler — once/year, round-robin near players
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

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableTreeAging` | `true` | Master toggle |
| `MaxTreeGrowthAttemptsPerTick` | `6` | Trees advanced per reproduce tick (2 s) |
| `TreeGrowthActivityScale` | `1` | Growth pace (>1 = faster relative to reference) |

---

## Limits (v1)

- No tree death yet — senescence age is metadata for inspect / future vitality.
- Calendar age is in-memory; re-registers after restart reset to 0 (same as spread registry).
- Crown radius for inspect: branchy skeleton only (not `leaves-grown` fluff); measure cap 9 blocks; BFS uses `HashSet<BlockPos>` (fixed coord packing).
