# Guidance for AI agents

**Quick start:** current stage → **`docs/PROGRESS.md`**. Prompt → **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from legacy Wild Farming vs what to ignore;
- current repo stage: **MVP-beta** (catmint playtest OK) — see `docs/PROGRESS.md`;
- MVP scope and agent rules.

Quick constraints:

- **Player planting** — no climate gate; only physics + claims.
- **Survival** — `CanSurviveAt` on `WildPlant`; death after failed checks, not blocked planting.
- **Wild reproduce** — `CanReproduceAt` + registry; `MinFitness` only here.
- Do **not** expand living trees, vines, mushrooms, or termites unless the user asks.
- BE: `src/BlockEntity/EcoSystemLife.cs`; new logic under `src/Ecosystem/`.
- Target: VS **1.21+**, **.NET 10**, `wildfarming.sln`.
- Do **not** run alongside **wildfarmingrevival**.

Entry: `src/WF.cs`, `src/Ecosystem/EcosystemSystem.cs`. Docs: `docs/PROJECT_VISION.md`, `docs/PROMPT.md`, `docs/VISUAL_STUDIO.md`.
