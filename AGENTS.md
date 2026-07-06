# Guidance for AI agents

**Quick start:** current stage → **`docs/PROGRESS.md`**. Prompt → **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from the original Wild Farming idea vs what to ignore;
- current repo stage: **Ecosystem v4.5.0** — **species CSV registry** — `docs/SPECIES_ECOLOGY_CSV.md` (`ecology.csv` + `season.csv`, user overrides in `ModConfig/`); spread scale (4.4.1); berry colony spread (4.3); registration — `docs/BACKGROUND_REGISTRATION.md`; spread maturation — `docs/FLOWER_SPREAD_MATURATION.md`, `docs/TALLGRASS_SPREAD_MATURATION.md`; **flower phenology** — `docs/FLOWER_PHENOLOGY.md`; canopy — `docs/CANOPY_PHENOLOGY.md`, ambience — `docs/CANOPY_AMBIENCE.md`, tree lifecycle — `docs/TREE_AGING.md`, tree fern — `docs/FERNTREE.md`, wild vines — `docs/WILD_VINE.md`; chunk load registers **vines** (column pass) and **mycelium anchors** (BE scan) into the same reproduce registry; player changelog — `docs/CHANGELOG.md`; config reference — `docs/CONFIGURATION.md`;
- agent rules and constraints.

Quick constraints:

- **Player planting** — no climate gate; only physics + claims.
- **Survival** — `MeetsSurvivalRequirements` on `SuitabilityEvaluator`; stress death after failed checks, not blocked planting.
- **Wild reproduce** — `CanReproduce` + registry; `MinFitness` only here.
- **Seasonality** — `WildSpeciesSeason` profiles; `SeasonEcology` multipliers; winter/fall stress.
- Do **not** expand living trees or termites unless the user asks. **Mycelium (v3.1.12):** soft niche around vanilla `BlockEntityMycelium` only — no custom mushroom blocks. **Ferntree / wild vines (v3.7):** vanilla blocks only; playtest before tuning.
- Legacy BE: `src/Ecosystem/LegacyBlockEntityMigration.cs` (EcoSystemLife / EcosystemPlant strip on load); new logic under `src/Ecosystem/`.
- Target: VS **1.22+**, **.NET 10**, `wildfarming.sln`.
- Tests: `tests/WildFarming.Tests.csproj` (xUnit, 684 tests).
- **Config:** `TryLoadFromDisk` rewrites `ModConfig/ecosystemflora.json` after load so new keys appear automatically (server always; client when file exists). Per-species tuning: `ModConfig/ecosystemflora/species/ecology.csv` and `season.csv` (auto-created on server start) — see `docs/SPECIES_ECOLOGY_CSV.md`.
- Do **not** run alongside **wildfarmingrevival**.

Entry: `src/WF.cs`, `src/Ecosystem/EcosystemSystem.cs`. Docs: `docs/PROJECT_VISION.md`, `docs/PROMPT.md`, `docs/THIRD_PARTY_ECOLOGY.md`, `docs/GAPS.md`, `docs/CHANGELOG.md`, `docs/VISUAL_STUDIO.md`, `docs/SUBMODULES.md`. Community compat patches: submodule `community/` (`ecosystemfloracompat`).
