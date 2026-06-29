# Ecosystem - Flora — configuration reference

Public mirror of mod configuration docs and templates. Canonical copies are regenerated from `EcosystemConfig.cs` via:

```powershell
python tools/generate_configuration_doc.py
```

## Files

| File | Purpose |
|------|---------|
| [`CONFIGURATION.md`](CONFIGURATION.md) | Full `ecosystemflora.json` key reference (201 keys) |
| [`ecosystemflora.example.json`](ecosystemflora.example.json) | Default template — copy to `%AppData%/Vintagestory/ModConfig/ecosystemflora.json` |
| [`SPECIES_ECOLOGY_CSV.md`](SPECIES_ECOLOGY_CSV.md) | Per-species `ecology.csv` + `season.csv` tuning |
| [`species/ecology.override.example.csv`](species/ecology.override.example.csv) | Partial ecology override samples |
| [`species/season.override.example.csv`](species/season.override.example.csv) | Partial season override sample |

**In-game path:** `ModConfig/ecosystemflora.json` and `ModConfig/ecosystemflora/species/*.csv` on the **server**.

**Reload species CSV without restart:** `/ecospeciesreload` (server admin, `controlserver` privilege).

Published standalone repo: [vs-ecosystemflora-config](https://github.com/dann1kid/vs-ecosystemflora-config).
