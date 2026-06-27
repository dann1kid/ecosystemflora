# Forest seral succession (trees)

Wild tree spread uses **local forest cover** at the sapling cell plus per-wood **seral role** so open ground gets pioneers first and mature canopy favors climax species.

## Roles

| Role | Examples | Open cover | Mature cover |
|------|----------|------------|--------------|
| **Pioneer** | birch, acacia | High fitness | Hard `MaxForest` cap + low seral multiplier |
| **Mid** | maple, walnut, baldcypress, purpleheart | Moderate | Edge / young-stand peak |
| **Climax** | oak, pine, larch, redwood, kapok, ebony | Low until ~6% cover | Peak ~55–65% cover |

Climate (temp / rain) still comes from vanilla worldgen — no latitude bands in code.

## Mechanics

1. **`MinForest` / `MaxForest`** — hard gate on `LocalForestCover` (pioneers capped ~0.40–0.42).
2. **`SeralSpreadMultiplier`** — soft bell curve on cover (see `WildTreeEcology.cs`).
3. **`SaplingMinSunlight`** — pioneers need light 11; climax can use 9–10 in gaps.
4. **`FloraContext` affinity** — pioneers Open, climax Forest (when `UseFloraContext`).
5. **Senescence horizon** — shorter for pioneers (birch 75 y, acacia 68 y) so gaps refill toward climax.

Parent spread still places vanilla `sapling-*`; growth is vanilla treegen.

## Config

| Key | Default |
|-----|---------|
| `EnableTreeSeralSuccession` | `true` |

When off, only `MinForest`/`MaxForest` apply (legacy behavior).

## Inspect (I)

On registered trunks: **Forest role: pioneer / mid-seral / climax**.

See also: [`TREE_AGING.md`](TREE_AGING.md), [`PROJECT_VISION.md`](PROJECT_VISION.md) §10–11.
