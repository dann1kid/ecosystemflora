# Guidance for AI agents

**Quick start:** current stage → **`docs/PROGRESS.md`**. Prompt → **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from the original Wild Farming idea vs what to ignore;
- current repo stage: **Ecosystem v3.2.0** — see `docs/PROGRESS.md`, gaps — `docs/GAPS.md`; canopy — `docs/CANOPY_PHENOLOGY.md`;
- agent rules and constraints.

Quick constraints:

- **Player planting** — no climate gate; only physics + claims.
- **Survival** — `MeetsSurvivalRequirements` on `SuitabilityEvaluator`; stress death after failed checks, not blocked planting.
- **Wild reproduce** — `CanReproduce` + registry; `MinFitness` only here.
- **Seasonality** — `WildSpeciesSeason` profiles; `SeasonEcology` multipliers; winter/fall stress.
- Do **not** expand living trees, vines, or termites unless the user asks. **Mycelium (v3.1.12):** soft niche around vanilla `BlockEntityMycelium` only — no custom mushroom blocks.
- Legacy BE: `src/Ecosystem/LegacyBlockEntityMigration.cs` (EcoSystemLife / EcosystemPlant strip on load); new logic under `src/Ecosystem/`.
- Target: VS **1.22+**, **.NET 10**, `wildfarming.sln`.
- Tests: `tests/WildFarming.Tests.csproj` (xUnit, 215 tests).
- **Config:** `TryLoadFromDisk` rewrites `ModConfig/ecosystemflora.json` after load so new keys appear automatically (server always; client when file exists).
- Do **not** run alongside **wildfarmingrevival**.

Entry: `src/WF.cs`, `src/Ecosystem/EcosystemSystem.cs`. Docs: `docs/PROJECT_VISION.md`, `docs/PROMPT.md`, `docs/THIRD_PARTY_ECOLOGY.md`, `docs/GAPS.md`, `docs/VISUAL_STUDIO.md`.
