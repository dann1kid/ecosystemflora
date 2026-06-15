# Wild tree aging (v3.6)

Registered wild trees (`log-grown` trunk base in the ecology registry) **mature once per game year** near active players: taller trunk (`log-grown`) and wider crown (`leavesbranchy` / `leaves-grown`).

Updated: 2026-06-14.

---

## Behaviour

| When | What |
|------|------|
| **Registration** | Age estimated from trunk + **connected** crown (not nearby trees); worldgen-sized trees read as ~25–45 y, not max age |
| **Each game year** | `TreeAgeYears++`, then 0–2 block placements toward species target |
| **Young trees** | Mostly upward `log-grown` extension |
| **Mid age** | Mix of height + outward `leavesbranchy` |
| **Old age** | Mostly crown spread + occasional `leaves-grown`; very old trees grow rarely |

No custom blocks, no save BE — only vanilla **grown** codes. Sapling spread and vanilla treegen on plant are unchanged.

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
| Species max size / age | `WildTreeGrowthProfiles.cs` |
| Target height & radius vs age | `TreeGrowthTargets.cs` |
| Measure trunk / crown | `TreeStructureProbe.cs` |
| Block placement | `TreeGrowthApplier.cs` |
| Tick scheduling | `TreeGrowthScheduler.cs` |

---

## Species targets (defaults)

| Wood | Max age | Max trunk (blocks) | Max crown radius |
|------|---------|-------------------|------------------|
| Oak | 120 | 34 | 8 |
| Birch | 90 | 26 | 6 |
| Maple | 100 | 28 | 7 |
| Redwood | 140 | 48 | 6 |
| Kapok | 110 | 40 | 9 |
| Others | 80–110 | 22–38 | 4–7 |

A **100-year oak** in a long-lived world approaches full profile height and a wide branchy crown instead of staying worldgen-small.

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableTreeAging` | `true` | Master toggle |
| `MaxTreeGrowthAttemptsPerTick` | `6` | Trees advanced per reproduce tick (2 s) |
| `TreeGrowthActivityScale` | `1` | Scales target height/radius |

Requires `EcosystemEnabled`, `OnlyActivateNearPlayers` radius (same as spread), and a registered trunk.

**Inspect (I)** on a registered trunk shows `Tree age: X / Y years` and current vs target trunk/crown size.

---

## Limits (v1)

- Single-trunk column height from registry origin; wide multi-trunk oaks grow from crown anchors in a 14-block scan.
- No shrink on damage — age only adds blocks while below target.
- Age is re-estimated on re-registration after restart (from structure, stable if growth persisted in world).
- Conifers and deciduous both age; bambo / aged logs excluded from registry.
