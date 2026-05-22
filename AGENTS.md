# Guidance for AI agents

**Quick start:** copy the block from **`docs/PROMPT.md`**.

Read **`docs/PROJECT_VISION.md`** before non-trivial changes. It defines:

- ecosystem-first design (interfaces, not monolithic block entities);
- living = must reproduce (`IReproducible`);
- what to reuse from legacy Wild Farming vs what to ignore;
- current repo stage (original v1.2.0 archive, pre-MVP);
- MVP scope and agent rules.

Quick constraints:

- Do **not** expand living trees, vines, mushrooms, or termites unless the user asks.
- Prefer new code under `src/Ecosystem/` (planned; not created yet).
- Target port: Vintage Story **1.21+**, **.NET 8** — not the legacy .NET 4.6.1 project as-is.
- Do **not** install or merge alongside **wildfarmingrevival**; different modid, same niche.

Legacy entry point: `src/WF.cs`. Theory: `docs/PROJECT_VISION.md`. Prompt: `docs/PROMPT.md`.
