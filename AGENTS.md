# Guidance for AI agents

**Quick start:** current stage → **`docs/PROGRESS.md`**. Prompt → **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from the original Wild Farming idea vs what to ignore;
- current repo stage: **Ecosystem v4.7.0** — **third-party wild ecology bootstraps** (Wildcraft Fruit/Trees, Floral Zones worldgen table, fruitvine climate-only); **B+ species auto-curves** (`DiscoveredSpeciesStore`, `DynamicSpeciesAutoCurves`); **species CSV registry** — `docs/SPECIES_ECOLOGY_CSV.md` (`ecology.csv` + `season.csv`, user overrides in `ModConfig/`); spread scale (4.4.1); berry colony spread (4.3); optional compat submodule `community/ecosystemfloracompat` (Biodiversity patches); registration — `docs/BACKGROUND_REGISTRATION.md`; spread maturation — `docs/FLOWER_SPREAD_MATURATION.md`, `docs/TALLGRASS_SPREAD_MATURATION.md`; **flower phenology** — `docs/FLOWER_PHENOLOGY.md`; canopy — `docs/CANOPY_PHENOLOGY.md` (wildfire guard, orphan prune), ambience — `docs/CANOPY_AMBIENCE.md`, tree lifecycle — `docs/TREE_AGING.md`, tree fern — `docs/FERNTREE.md`, wild vines — `docs/WILD_VINE.md`; chunk load registers **vines** (column pass) and **mycelium anchors** (BE scan) into the same reproduce registry; player changelog — `docs/CHANGELOG.md`; config reference — `docs/CONFIGURATION.md`; **next (unreleased):** Floral Zones Cape + Cosmopolitan (211 entries); **planned v5.0:** Phase 7 external ecology sim for **unloaded** chunks — `docs/PHASE7_EXTERNAL_SIMULATION.md` (DB + optional Go worker, spawn/kill from mod, catch-up on load);
- agent rules and constraints.

Quick constraints:

- **Player planting** — no climate gate; only physics + claims.
- **Survival** — `MeetsSurvivalRequirements` on `SuitabilityEvaluator`; stress death after failed checks, not blocked planting.
- **Wild reproduce** — `CanReproduce` + registry; `MinFitness` only here.
- **Seasonality** — `WildSpeciesSeason` profiles; `SeasonEcology` multipliers; winter/fall stress.
- Do **not** expand living trees or termites unless the user asks. **Mycelium (v3.1.12):** soft niche around vanilla `BlockEntityMycelium` only — no custom mushroom blocks. **Ferntree / wild vines (v3.7):** vanilla blocks only; playtest before tuning. **Third-party bootstraps (v4.7.0):** C# runtime injection for Wildcraft Fruit/Trees and Floral Zones — do not patch parent mod JSON unless user asks; **fruitvine** is climate-only (`ecologySpreadRate: 0`).
- Legacy BE: `src/Ecosystem/LegacyBlockEntityMigration.cs` (EcoSystemLife / EcosystemPlant strip on load); new logic under `src/Ecosystem/`.
- Target: VS **1.22+**, **.NET 10**, `wildfarming.sln`.
- Tests: `tests/WildFarming.Tests.csproj` (xUnit, 882 tests).
- **Config:** `TryLoadFromDisk` rewrites `ModConfig/ecosystemflora.json` after load so new keys appear automatically (server always; client when file exists). Per-species tuning: `ModConfig/ecosystemflora/species/ecology.csv` and `season.csv` (auto-created on server start) — see `docs/SPECIES_ECOLOGY_CSV.md`.
- Do **not** run alongside **wildfarmingrevival**.

Entry: `src/WF.cs`, `src/Ecosystem/EcosystemSystem.cs`. Docs: `docs/PROJECT_VISION.md`, `docs/PROMPT.md`, `docs/THIRD_PARTY_ECOLOGY.md`, `docs/GAPS.md`, `docs/CHANGELOG.md`, `docs/PHASE7_EXTERNAL_SIMULATION.md`, `docs/VISUAL_STUDIO.md`, `docs/SUBMODULES.md`. Community compat patches: submodule `community/` (`ecosystemfloracompat`).
