# Examples

## `ecosystemflora-config/`

Public **configuration reference** for [Ecosystem - Flora](https://mods.vintagestory.at/ecosystemflora): `CONFIGURATION.md`, `ecosystemflora.example.json`, species CSV docs, and override samples.

- Synced from `docs/` + `assets/ecosystemflora/` via `python tools/generate_configuration_doc.py`.
- Published as [vs-ecosystemflora-config](https://github.com/dann1kid/vs-ecosystemflora-config) (`public-config/` in this repo).

## `ecologysample-mynewplant/`

Reference **content mod** for third-party [Ecosystem - Flora](https://mods.vintagestory.at/ecosystemflora) integration (`ecologyParticipant` JSON attributes).

- Lives in this monorepo under `examples/ecologysample-mynewplant/`.
- **Existing plant mod?** → `ecologysample-mynewplant/EXISTING_MOD.md` (four attrs or patch).
- Intended to be published as a **separate public GitHub repository** (see `ecologysample-mynewplant/PUBLISHING.md`).
- Sync tool: `tools/Export-EcologySampleRepo.ps1`.
