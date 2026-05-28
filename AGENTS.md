# Guidance for AI agents

**Quick start:** current stage → **`docs/PROGRESS.md`**. Prompt → **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from the original Wild Farming idea vs what to ignore;
- current repo stage: **Ecosystem v3.1.7** — see `docs/PROGRESS.md`, gaps — `docs/GAPS.md`;
- agent rules and constraints.

Quick constraints:

- **Player planting** — no climate gate; only physics + claims.
- **Survival** — `MeetsSurvivalRequirements` on `SuitabilityEvaluator`; stress death after failed checks, not blocked planting.
- **Wild reproduce** — `CanReproduce` + registry; `MinFitness` only here.
- **Seasonality** — `WildSpeciesSeason` profiles; `SeasonEcology` multipliers; winter/fall stress.
- Do **not** expand living trees, vines, mushrooms, or termites unless the user asks.
- BE: `src/BlockEntity/EcoSystemLife.cs`; new logic under `src/Ecosystem/`.
- Target: VS **1.22+**, **.NET 10**, `wildfarming.sln`.
- Tests: `tests/WildFarming.Tests.csproj` (xUnit, 106 tests).
- Do **not** run alongside **wildfarmingrevival**.

Entry: `src/WF.cs`, `src/Ecosystem/EcosystemSystem.cs`. Docs: `docs/PROJECT_VISION.md`, `docs/PROMPT.md`, `docs/THIRD_PARTY_ECOLOGY.md`, `docs/GAPS.md`, `docs/VISUAL_STUDIO.md`.
