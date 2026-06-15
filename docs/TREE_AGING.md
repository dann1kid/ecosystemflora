# Wild tree maturation (v3.6)

Registered wild trees (`log-grown` trunk base in the ecology registry) **grow once per game year** near active players: taller trunk (`log-grown`) and wider crown (`leavesbranchy` / `leaves-grown`).

Updated: 2026-06-14.

---

## Behaviour

| When | What |
|------|------|
| **Registration** | No stored age — structure measured live from blocks |
| **Each game year** | 0–2 block placements toward species max trunk / crown |
| **Young trees** | Mostly upward `log-grown` extension |
| **Mid maturation** | Mix of height + outward `leavesbranchy` |
| **Near max size** | Mostly crown spread + occasional `leaves-grown`; growth slows |

No custom blocks, no save BE — only vanilla **grown** codes. Sapling spread and vanilla treegen on plant are unchanged.

---

## Maturation index

Progress is **block-based**, not calendar years:

```
maturity = 55% × (trunk blocks / species max trunk)
         + 45% × (crown radius / species max crown)
```

Inspect shows: `Maturation: 41% (trunk 14/34 blocks, crown radius 5/8)`.

Growth runs while structure is below profile max (× `TreeGrowthActivityScale`). Chopping the crown lowers the index; the tree can grow again on later ticks.

---

## Architecture

```
ReproducerEntry (TerrestrialTree)
        ↓
TreeGrowthScheduler — once/year, round-robin near players
        ↓
TreeGrowthApplier — log up / branchy out / leaf fill
        ↓
CanopyBlockHelper block resolve + land claims
```

| Component | File |
|-----------|------|
| Species max size | `WildTreeGrowthProfiles.cs` |
| Maturity fraction + targets | `TreeGrowthTargets.cs` |
| Measure trunk / crown | `TreeStructureProbe.cs` |
| Block placement | `TreeGrowthApplier.cs` |
| Tick scheduling | `TreeGrowthScheduler.cs` |

---

## Species targets (defaults)

| Wood | Max trunk (blocks) | Max crown radius |
|------|-------------------|------------------|
| Oak | 34 | 8 |
| Birch | 26 | 6 |
| Maple | 28 | 7 |
| Redwood | 48 | 6 |
| Kapok | 40 | 9 |
| Others | 22–38 | 4–9 |

Ancient oaks in long-lived worlds approach full profile height and a wide branchy crown instead of staying worldgen-small.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableTreeAging` | `true` | Master toggle |
| `MaxTreeGrowthAttemptsPerTick` | `6` | Trees advanced per reproduce tick (2 s) |
| `TreeGrowthActivityScale` | `1` | Scales max trunk height / crown radius |

Requires `EcosystemEnabled`, `OnlyActivateNearPlayers` radius (same as spread), and a registered trunk.

**Inspect (I)** on a registered trunk shows maturation % and trunk/crown vs species max.

---

## Limits (v1)

- Single-trunk column height from registry origin; wide multi-trunk oaks grow from crown anchors in a 14-block scan.
- Crown radius uses flood-fill from trunk — not neighbouring trees of same wood.
- No tree death / senescence yet — 100% maturation = max profile size, not end of life.
- Conifers and deciduous both mature; bambo / aged logs excluded from registry.
