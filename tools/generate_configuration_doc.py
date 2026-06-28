#!/usr/bin/env python3
"""Regenerate docs/CONFIGURATION.md key reference from EcosystemConfig + ConfigFieldDescriptions."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONFIG_CS = ROOT / "src" / "Ecosystem" / "EcosystemConfig.cs"
DESC_CS = ROOT / "src" / "Ecosystem" / "Config" / "ConfigFieldDescriptions.cs"
SCHEMA_CS = ROOT / "src" / "Ecosystem" / "Config" / "EcosystemConfigSchema.cs"
OUT = ROOT / "docs" / "CONFIGURATION.md"

SKIP = {
    "MaxCanopyUpdateOpsPerTick",
    "CanopyBudgetMs",
}

CATEGORY_TITLES = {
    "master": "Master & climate",
    "spread": "Spread — core & maturation",
    "aquatic": "Spread — aquatic mats",
    "competition": "Spacing, flora context & displacement",
    "stress": "Stress, symbiosis & seasons",
    "soil": "Soil succession & farmland",
    "scope": "Land claims & player scope",
    "trees": "Wild trees, ferntree & vines",
    "mycelium": "Mycelium ecology",
    "canopy": "Seasonal canopy (foliage & client ambience)",
    "harvest": "Meadow harvest",
    "perf": "Registration & performance",
    "advanced": "Diagnostics, logging & ecology inspect (client)",
}

CATEGORY_ORDER = [
    "master",
    "spread",
    "aquatic",
    "competition",
    "stress",
    "soil",
    "scope",
    "trees",
    "mycelium",
    "canopy",
    "harvest",
    "perf",
    "advanced",
]

SCOPE_HINT = {
    "EnableCanopyAmbience": "client",
    "CanopyAmbienceMinHeightBlocks": "client",
    "CanopyAmbienceMoteRate": "client",
    "CanopyAmbienceLeafDriftRate": "client",
    "CanopyAmbienceSampleIntervalSeconds": "client",
    "CanopyAmbienceSuppressInRain": "client",
    "EnableEcologyInspect": "client",
    "EcologyInspectCooldownSeconds": "client",
    "EcologyInspectScanRadius": "client",
    "EnableEcologyAreaScan": "client",
}


def parse_defaults() -> dict[str, tuple[str, str]]:
    text = CONFIG_CS.read_text(encoding="utf-8")
    out: dict[str, tuple[str, str]] = {}
    for m in re.finditer(
        r"public (\w+(?:\[\])?) (\w+) \{ get; set; \} = ([^;]+);",
        text,
    ):
        name, default = m.group(2), m.group(3).strip()
        if name in SKIP:
            continue
        out[name] = (m.group(1), default)
    return out


def parse_descriptions() -> dict[str, str]:
    text = DESC_CS.read_text(encoding="utf-8")
    out: dict[str, str] = {}
    for m in re.finditer(
        r'D\(nameof\(EcosystemConfig\.(\w+)\),\s*\n\s*"([^"]+)"',
        text,
        re.MULTILINE,
    ):
        out[m.group(1)] = m.group(2)
    return out


def parse_prefix_categories() -> list[tuple[str, str]]:
    text = SCHEMA_CS.read_text(encoding="utf-8")
    block = re.search(r"PrefixCategories\s*=\s*\{(.*?)\};", text, re.DOTALL)
    if not block:
        return []
    pairs = []
    for m in re.finditer(r'\("([^"]+)",\s*"([^"]+)"\)', block.group(1)):
        pairs.append((m.group(1), m.group(2)))
    return pairs


def infer_category(name: str, prefixes: list[tuple[str, str]]) -> str:
    overrides = {
        "ReproduceDebug": "advanced",
        "VerboseLogging": "advanced",
        "ReproduceTickProfilingIntervalMs": "perf",
        "ReproduceTickProfilingMinRegistry": "perf",
        "EnableReproduceTickProfiling": "perf",
        "StaggerReproduceAttempts": "spread",
        "CloneBerryTraits": "trees",
        "BerryTraitMutationChance": "trees",
        "EnableFlowerDrygrass": "harvest",
        "EnableThirdPartyParticipants": "master",
        "EnableEcologyHistoryHint": "master",
        "EnableCanopyAmbience": "canopy",
        "EnableEcologyInspect": "advanced",
        "EnableEcologyAreaScan": "advanced",
        "EcologyInspectCooldownSeconds": "advanced",
        "EcologyInspectScanRadius": "advanced",
        "EnableTrampling": "stress",
        "TramplingRadius": "stress",
        "TramplingStressThreshold": "stress",
        "TramplingSoilDegradation": "stress",
    }
    if name in overrides:
        return overrides[name]
    if name.startswith("CanopyAmbience"):
        return "canopy"
    for prefix, category in prefixes:
        if name.startswith(prefix):
            return category
    return "advanced"


def fmt_default(default: str) -> str:
    default = default.replace("\r", "").replace("\n", " ")
    if default.endswith("f"):
        default = default[:-1]
    if default == "true":
        return "**true**"
    if default == "false":
        return "false"
    if default.startswith('"') and default.endswith('"'):
        inner = default[1:-1]
        return f"`{inner}`"
    if "EcosystemBalancePresets.Natural" in default:
        return "`natural`"
    if default == "1f / 3f" or default == "1f / 3":
        return "`0.333` (~⅓)"
    return f"`{default}`"


def fmt_type(clr_type: str) -> str:
    return {
        "bool": "bool",
        "int": "int",
        "float": "float",
        "double": "double",
        "string": "string",
    }.get(clr_type, clr_type)


def build_key_tables(defaults, descriptions, prefixes) -> str:
    by_cat: dict[str, list[str]] = {c: [] for c in CATEGORY_ORDER}
    for name in sorted(defaults):
        cat = infer_category(name, prefixes)
        if cat not in by_cat:
            by_cat[cat] = []
        by_cat[cat].append(name)

    lines: list[str] = []
    lines.append("## Key reference (complete)")
    lines.append("")
    lines.append(
        f"**{len(defaults)} keys** from `EcosystemConfig.cs` (generated by `tools/generate_configuration_doc.py`). "
        "Descriptions from config UI (`ConfigFieldDescriptions.cs`). "
        f"Per-species balance: [`SPECIES_ECOLOGY_CSV.md`](SPECIES_ECOLOGY_CSV.md)."
    )
    lines.append("")
    lines.append("Types: `bool`, `int`, `float`, `double`, `string`. **Scope:** server unless noted *client*.")
    lines.append("")

    for cat in CATEGORY_ORDER:
        names = by_cat.get(cat, [])
        if not names:
            continue
        lines.append(f"### {CATEGORY_TITLES.get(cat, cat.title())}")
        lines.append("")
        lines.append("| Key | Type | Default | Scope | Description |")
        lines.append("|-----|------|---------|-------|-------------|")
        for name in names:
            clr_type, default = defaults[name]
            desc = descriptions.get(name, "")
            if not desc:
                desc = "—"
            scope = SCOPE_HINT.get(name, "server")
            scope_cell = f"*{scope}*" if scope == "client" else "server"
            lines.append(
                f"| `{name}` | {fmt_type(clr_type)} | {fmt_default(default)} | {scope_cell} | {desc} |"
            )
        lines.append("")

    return "\n".join(lines)


def main() -> None:
    defaults = parse_defaults()
    descriptions = parse_descriptions()
    prefixes = parse_prefix_categories()
    existing = OUT.read_text(encoding="utf-8")
    marker = "## Key reference"
    if marker in existing:
        head = existing.split(marker, 1)[0].rstrip()
    else:
        head = existing.rstrip()
    head = re.sub(r"\n---\s*$", "", head)

    # Fix outdated CSV paths in header sections
    head = head.replace(
        "ModConfig/ecosystemflora.species.csv",
        "ModConfig/ecosystemflora/species/ecology.csv",
    )
    head = head.replace(
        "Tune one species in `ModConfig/ecosystemflora.species.csv`",
        "Tune one species in `ModConfig/ecosystemflora/species/ecology.csv`",
    )
    head = head.replace(
        "**Global spread multiplier** (`SpeciesSpreadRateScale`) applies to every species `spread_rate` from CSV. Tune one species in `ModConfig/ecosystemflora/species/ecology.csv` when you need an exception.",
        "**Global spread multiplier** (`SpeciesSpreadRateScale`) applies to every species `spread_rate` from CSV. Per-species exceptions: `ModConfig/ecosystemflora/species/ecology.csv` and `season.csv`. Reload without restart: **`/ecospeciesreload`** (server admin) — see [`SPECIES_ECOLOGY_CSV.md`](SPECIES_ECOLOGY_CSV.md).",
    )

    tail = """

### Trampling

See **Stress** section in key reference (`EnableTrampling`, `TramplingRadius`, …).

### Legacy JSON aliases

| Alias | Maps to |
|-------|---------|
| `MaxCanopyUpdateOpsPerTick` | `MaxFoliageCellsTickedPerTick` |
| `CanopyBudgetMs` | `FoliageBudgetMs` |

Prefer the primary names in new configs.

### Block JSON (third-party, not in `ecosystemflora.json`)

| Attribute | Values |
|-----------|--------|
| `ecologyParticipant` | `true` — join ecology when `EnableThirdPartyParticipants` is on |
| `ecologySpreadMode` | `rhizome`, `surfacemat`, `independent` |

See [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md).

---

## Changelog of config keys (by version)

For release history see [`CHANGELOG.md`](CHANGELOG.md). This file lists the **complete current JSON key set**; version history is in the changelog.

**Maintainer:** after adding keys to `EcosystemConfig.cs` and `ConfigFieldDescriptions.cs`, run:

```powershell
python tools/generate_configuration_doc.py
```
"""

    body = build_key_tables(defaults, descriptions, prefixes)
    OUT.write_text(head + "\n\n" + body + tail + "\n", encoding="utf-8")
    print(f"Wrote {OUT} ({len(defaults)} keys, {len(descriptions)} descriptions).")


if __name__ == "__main__":
    main()
