#!/usr/bin/env python3
"""Regenerate config docs, example JSON, and sync public config mirrors."""
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CONFIG_CS = ROOT / "src" / "Ecosystem" / "EcosystemConfig.cs"
DESC_CS = ROOT / "src" / "Ecosystem" / "Config" / "ConfigFieldDescriptions.cs"
SCHEMA_CS = ROOT / "src" / "Ecosystem" / "Config" / "EcosystemConfigSchema.cs"
OUT = ROOT / "docs" / "CONFIGURATION.md"
SPECIES_DOC = ROOT / "docs" / "SPECIES_ECOLOGY_CSV.md"
EXAMPLE_JSON_OUT = ROOT / "assets" / "ecosystemflora" / "ecosystemflora.example.json"
MIRROR_DIRS = (
    ROOT / "examples" / "ecosystemflora-config",
    ROOT / "public-config",
)
SECTION_BREAK_BEFORE = {
    "EnableTreeAging",
    "UseFloraContext",
    "EnableEcologyInspect",
    "CloneBerryTraits",
    "EnableThirdPartyParticipants",
    "EnableMyceliumNiche",
    "EnableSeasonalFoliage",
    "EnableCanopyAmbience",
}

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


def parse_field_order() -> list[str]:
    text = CONFIG_CS.read_text(encoding="utf-8")
    order: list[str] = []
    for m in re.finditer(
        r"public (\w+(?:\[\])?) (\w+) \{ get; set; \} = ([^;]+);",
        text,
    ):
        name = m.group(2)
        if name in SKIP:
            continue
        order.append(name)
    return order


def default_to_json(clr_type: str, default: str):
    default = default.strip()
    if default == "true":
        return True
    if default == "false":
        return False
    if "EcosystemBalancePresets.Natural" in default:
        return "natural"
    if default.startswith('"') and default.endswith('"'):
        return default[1:-1]
    if clr_type == "int":
        token = default.split()[0]
        if token.endswith("f") or token.endswith("F"):
            token = token[:-1]
        return int(float(token))
    if clr_type in ("float", "double"):
        if "/" in default:
            left, right = (part.strip() for part in default.split("/", 1))

            def strip_f(token: str) -> float:
                if token.endswith("f") or token.endswith("F"):
                    token = token[:-1]
                return float(token)

            return strip_f(left) / strip_f(right)
        token = default
        if token.endswith("f") or token.endswith("F"):
            token = token[:-1]
        return float(token)
    raise ValueError(f"Unsupported default {default!r} ({clr_type})")


def generate_example_json(defaults: dict[str, tuple[str, str]], field_order: list[str]) -> str:
    pairs: list[tuple[str, object]] = []
    for name in field_order:
        if name not in defaults:
            continue
        clr_type, default = defaults[name]
        pairs.append((name, default_to_json(clr_type, default)))

    chunks: list[str] = []
    for i, (name, value) in enumerate(pairs):
        line = f'  "{name}": {json.dumps(value, ensure_ascii=False)}'
        if i == 0:
            chunks.append(line)
            continue
        sep = ",\n\n" if name in SECTION_BREAK_BEFORE else ",\n"
        chunks.append(sep + line)
    return "{\n" + "".join(chunks) + "\n}\n"


def adapt_configuration_for_mirror(text: str) -> str:
    text = text.replace(
        "**Template:** `assets/ecosystemflora/ecosystemflora.example.json` in the mod package.",
        "**Template:** [`ecosystemflora.example.json`](ecosystemflora.example.json) in this folder "
        "(also shipped as `assets/ecosystemflora/ecosystemflora.example.json` in the mod).",
    )
    text = text.replace(
        "See [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md).",
        "See [THIRD_PARTY_ECOLOGY.md](https://github.com/dann1kid/vs-wildfarming/blob/main/docs/THIRD_PARTY_ECOLOGY.md) in the mod repo.",
    )
    text = text.replace(
        "For release history see [`CHANGELOG.md`](CHANGELOG.md).",
        "For release history see [CHANGELOG.md](https://github.com/dann1kid/vs-wildfarming/blob/main/docs/CHANGELOG.md).",
    )
    return text


def sync_config_mirrors(configuration_md: str, example_json: str) -> None:
    mirror_configuration = adapt_configuration_for_mirror(configuration_md)
    for dest_dir in MIRROR_DIRS:
        dest_dir.mkdir(parents=True, exist_ok=True)
        (dest_dir / "CONFIGURATION.md").write_text(mirror_configuration, encoding="utf-8")
        (dest_dir / "ecosystemflora.example.json").write_text(example_json, encoding="utf-8")
        shutil.copy2(SPECIES_DOC, dest_dir / "SPECIES_ECOLOGY_CSV.md")
        species_dir = dest_dir / "species"
        species_dir.mkdir(exist_ok=True)
        override = species_dir / "ecology.override.example.csv"
        if not override.exists():
            override.write_text(
                "species,spread_rate\n"
                "horsetail,1.5\n"
                "brownsedge,1.05\n",
                encoding="utf-8",
            )
        season_override = species_dir / "season.override.example.csv"
        if not season_override.exists():
            season_override.write_text(
                "species,spread_jun,spread_jul,stress_jan\n"
                "bluebell,1.2,1.0,0.15\n",
                encoding="utf-8",
            )
        print(f"Synced {dest_dir.relative_to(ROOT)}")


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

This also refreshes `assets/ecosystemflora/ecosystemflora.example.json`, `examples/ecosystemflora-config/`, and `public-config/`.
"""

    body = build_key_tables(defaults, descriptions, prefixes)
    configuration_md = head + "\n\n" + body + tail + "\n"
    OUT.write_text(configuration_md, encoding="utf-8")
    print(f"Wrote {OUT} ({len(defaults)} keys, {len(descriptions)} descriptions).")

    field_order = parse_field_order()
    example_json = generate_example_json(defaults, field_order)
    EXAMPLE_JSON_OUT.write_text(example_json, encoding="utf-8")
    print(f"Wrote {EXAMPLE_JSON_OUT.relative_to(ROOT)} ({len(field_order)} keys).")

    sync_config_mirrors(configuration_md, example_json)


if __name__ == "__main__":
    main()
