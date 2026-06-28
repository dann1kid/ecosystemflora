# Species ecology CSV tuning

Per-species balance for **65 contract species** (flowers, ferns, berries, trees, tallgrass, aquatic, etc.) lives in CSV tables. Runtime reads the merged registry — not the old C# `Wild*Ecology` tables (those are **export-only** seeds).

JSON config (`ecosystemflora.json`) still controls **global** knobs: spread attempts/year, `SpeciesSpreadRateScale`, maturation toggles, phenology, and so on.

---

## Merge layers (priority low → high)

| Layer | Path | Role |
|-------|------|------|
| 1. C# defaults | `SpeciesEcologyExporter` / `SpeciesSeasonExporter` | Fallback when no CSV; seeds export |
| 2. Mod assets | `assets/ecosystemflora/species/ecology.csv` | Shipped balance with the mod |
| 2. Mod assets | `assets/ecosystemflora/species/season.csv` | Monthly spread/stress curves |
| 3. User override | `ModConfig/ecosystemflora/species/ecology.csv` | Your ecology edits (partial rows OK) |
| 3. User override | `ModConfig/ecosystemflora/species/season.csv` | Your season edits |

**Server first start:** creates `ModConfig/ecosystemflora/species/` and writes full `ecology.csv` + `season.csv` from shipped defaults when missing. On every server start, **missing contract species rows** are appended after mod updates. Edit those files and **restart the world** (or rejoin) to reload — or on server use **`/ecospeciesreload`** (admin) without a full restart.

Legacy flat files (`ModConfig/ecosystemflora.species.csv`, `.species.season.csv`) are **moved** into the species folder on first load if the new paths do not exist yet.

**Partial override:** empty CSV cell = keep value from lower layers when the row is merged at runtime.

### Client vs server (inspect / handbook)

| Side | Ecology source | Notes |
|------|----------------|-------|
| **Dedicated server** | `ModConfig/ecosystemflora/species/*.csv` on the **server** | Spread, stress, maturation use this table. |
| **Client (MP)** | Shipped mod assets only (unless you copy the same ModConfig CSVs locally) | Handbook (**I**) and inspect text may show **defaults**, while simulation follows the server. |
| **Singleplayer** | Same process — server ModConfig + assets | `/ecospeciesreload` updates the shared in-memory registry immediately. |

After editing CSV on a dedicated server, run **`/ecospeciesreload`** (requires `controlserver` privilege) instead of restarting the world.

### CSV validation (server log)

On load and reload, the mod warns about:

- **Duplicate `species` rows** in one file (last row wins).
- **Unknown species** in **user** CSV (typo rows are skipped, not merged).

Shipped asset CSV drift is caught in CI: `ShippedSpeciesCsvParityTests` compares `assets/ecosystemflora/species/*.csv` to `SpeciesEcologyExporter` / `SpeciesSeasonExporter`.

---

## Export scripts (maintainers)

Regenerate shipped CSV from current C# defaults:

```powershell
tools/Export-SpeciesEcologyCsv.ps1
tools/Export-SpeciesSeasonCsv.ps1
```

Tests gate the write (`ECOSYSTEMFLORA_EXPORT_SPECIES_CSV=1` / `ECOSYSTEMFLORA_EXPORT_SPECIES_SEASON_CSV=1`).

---

## `ecology.csv` columns

Header (42 columns):

| Column | Meaning |
|--------|---------|
| `species` | Ecology key (`horsetail`, `birch`, `brownsedge`, …) |
| `taxon` | `flower`, `fern`, `berry`, `tree`, `tallgrass`, `grass_colonizer`, `shore_sedge`, `desert`, `aquatic`, `ferntree`, `vine` |
| `min_temp` … `max_forest` | Climate envelope (°C, rain 0–1, forest cover 0–1) |
| `spread_rate` | Relative vigor (1 = config baseline); scaled by `SpeciesSpreadRateScale` in JSON |
| `spread_mode` | `Independent`, `RhizomeMat`, `SurfaceMat`, `FernRhizomeMat`, `BerryColonyMat`, `ShoreSedgeMat`, … |
| `mat_connectivity` | `Orthogonal4` or `Chebyshev8` (berry mats) |
| `seed_dispersal_chance`, `seed_dispersal_radius` | Mat species seed jumps |
| `mat_spread_radius`, `independent_spread_radius`, `spread_radius` | Horizontal reach (trees/saplings use `spread_radius`) |
| `same_species_spacing`, `other_species_spacing` | Chebyshev spacing (0 = patch-forming) |
| `spacing_from_species` | Per-other-species overrides: `wilddaisy=3\|catmint=3` |
| `min_sunlight` | Spread cell light floor (0 = skip) |
| `habitat` | `Terrestrial`, `TerrestrialTree`, `ReedNearWater`, `WaterSurface`, … |
| `water_*` | Aquatic depth / column fields |
| `soil_kinds` | Pipe flags: `MediumFert\|Peat\|Gravel` |
| `soil_min_fertility`, `soil_max_fertility` | Block fertility floor/ceiling |
| `context_affinity`, `context_bonus`, `forest_interior_penalty`, `hold_strength` | Cell competition / succession hold |
| `moisture`, `light`, `niche_bonus` | Niche profile (`Mesic`, `Shade`, …) |
| `season_explicit` | `true` if species has a dedicated season row in C# (informational) |
| `flower_maturation_h`, `flower_cooldown_h` | Juvenile / post-spread cooldown (flowers, sedge) |
| `fern_maturation_h`, `fern_cooldown_h` | Fern spread maturation |
| `berry_maturation_h` | Berry bush calendar maturation base |
| `tree_seral_role` | `Pioneer`, `Mid`, `Climax` |
| `soil_succession_role` | e.g. `MeadowColonizer`, `WetlandHerb`, `ForestUnderstory` |

Runtime wiring: `PlantRequirements.FromBlock` → `SpeciesEcologyRegistry` → spread policy gates (`BerryColonySpread`, `RhizomeSpread`, …). Handbook and ecology inspect (**I**) use the same merged data.

---

## `season.csv` columns

| Column | Meaning |
|--------|---------|
| `species` | Ecology key |
| `spread_jan` … `spread_dec` | Monthly spread multipliers (0 = dormant, 1 = baseline, &gt;1 = peak) |
| `stress_jan` … `stress_dec` | Per-month stress die-off chance (0–1) |

Used by `WildSpeciesSeason.Resolve` → `SeasonEcology` when `UseSeasonalEcology` is true.

---

## Example user overrides

**Slow horsetail globally (after JSON scale):**

`ModConfig/ecosystemflora/species/ecology.csv`:

```csv
species,spread_rate
horsetail,1.5
```

**Restore brownsedge effective rate at `SpeciesSpreadRateScale` 0.33** (target ≈0.35 effective → set `spread_rate` ≈ 1.05):

```csv
species,spread_rate
brownsedge,1.05
```

**Bluebell spacing:**

```csv
species,spacing_from_species
bluebell,wilddaisy=4|catmint=2
```

**June-only horsetail spread:**

`ModConfig/ecosystemflora/species/season.csv`:

```csv
species,spread_jun
horsetail,0.15
```

**Blackberry mat connectivity:**

```csv
species,mat_connectivity
blackberry,Orthogonal4
```

---

## JSON vs CSV

| Tune | Where |
|------|--------|
| All species spread ×0.33 | `SpeciesSpreadRateScale` in `ecosystemflora.json` or `BalancePreset` |
| One species spread / climate / maturation | `ModConfig/ecosystemflora/species/ecology.csv` |
| Seasonal curve shape | `ModConfig/ecosystemflora/species/season.csv` |
| Disable berry mat spread | `EnableBerryColonySpread` in JSON (policy gate; CSV still defines mode) |

See [`CONFIGURATION.md`](CONFIGURATION.md) for full JSON reference.

---

## Code map

| Area | Path |
|------|------|
| Registry load / merge | `SpeciesEcologyRegistry.cs`, `SpeciesSeasonRegistry.cs` |
| Reload / load entry | `SpeciesEcologyLoadService.cs`; `/ecospeciesreload` → `SpeciesEcologyServerSystem.cs` |
| CSV validation | `SpeciesEcologyCsvReader.cs`, `SpeciesCsvLoadWarnings.cs`, `SpeciesEcologyCatalogIndex.cs` |
| Export | `SpeciesEcologyExporter.cs`, `SpeciesSeasonExporter.cs` |
| FromBlock build | `PlantRequirementsRegistryBuild.cs` |
| Display (handbook / inspect) | `SpeciesEcologyDisplay.cs` |
| Export-only C# tables | `WildFlowerClimate.cs`, `WildBerryEcology.cs`, … (`[EcologyExportTable]`, `[Obsolete]`) |
| Tests | `ShippedSpeciesCsvParityTests`, `SpeciesEcologyExportTests`, `SpeciesSeasonRegistryTests`, `SpacingFromSpeciesCodecTests` |
