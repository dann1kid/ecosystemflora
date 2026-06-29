# Forest seral succession (trees)

Wild tree spread uses **local forest cover** at the sapling cell, per-wood **seral role**, and optional **species seral peaks** so each wood has a distinct niche (not one curve per role).

## Roles

| Role | Examples | Niche |
|------|----------|-------|
| **Pioneer** | birch, acacia | Bare/open ground; low `MaxForest`; peak cover ~6–8% |
| **Mid** | oak, maple, walnut, baldcypress, purpleheart | Edges, semi-open woodland, riparian; peaks ~22–35% |
| **Climax** | pine, larch, redwood, kapok, ebony | Mature forest; peaks ~38–62%; pine tolerates dense stands |

Climate (temp / rain) is tuned per wood from vanilla worldgen bands. Spacing follows vanilla seed hints (pine ~1 block, birch ~2–3, oak ~5, redwood ~7) with a small wild-spread buffer.

## Mechanics

1. **`MinForest` / `MaxForest`** — hard gate on `LocalForestCover`.
2. **`SeralPeakForest` / `SeralHalfWidth` / `SeralFloor`** — per-species bell on cover (`WildTreeEcology.cs`); falls back to role curve when unset.
3. **`SaplingMinSunlight`** — pioneers/sun-loving mids 11; shade-tolerant climax (pine, ebony) 9–10.
4. **`FloraContext` affinity** — pioneers Open; mids Edge; climax Forest; baldcypress wetland edge.
5. **Soil** — niche footing per wood (`WildPlantSoil.cs`); vanilla any-soil baseline, specials for sand/peat/gravel.

Parent spread still places vanilla `sapling-*`; growth is vanilla treegen.

## Config

| Key | Default |
|-----|---------|
| `EnableTreeSeralSuccession` | `true` |

When off, only `MinForest`/`MaxForest` apply (legacy behavior).

## Inspect (I)

On registered trunks: **Forest role: pioneer / mid-seral / climax**, plus climate/soil/sun from the species row.

Per-species overrides: `ModConfig/ecosystemflora/species/ecology.csv` (see `docs/SPECIES_ECOLOGY_CSV.md`).

See also: [`TREE_AGING.md`](TREE_AGING.md), [`PROJECT_VISION.md`](PROJECT_VISION.md) §10–11.
