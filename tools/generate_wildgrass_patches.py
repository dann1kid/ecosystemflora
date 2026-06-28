#!/usr/bin/env python3
"""Regenerate wildgrass-ecology.json and wildgrass-handbook.json."""
import json
from pathlib import Path

PATCH_DIR = Path(__file__).resolve().parent.parent / "assets" / "ecosystemflora" / "patches"
MODS = ("wildgrass", "wildgrasscontinued")
PLANTS = (
    "bluegrass",
    "switchgrass",
    "ryegrass",
    "bushgrass",
    "fescue",
    "buttongrass",
    "bermudagrass",
    "buffalograss",
    "common-rush",
)

ECOLOGY_COMMENT = (
    "Wildgrass / Wildgrass Fork (wildgrass:*). Mature growth stages only; "
    "harvest left to wildgrass cutIntoByType."
)
HANDBOOK_COMMENT = (
    "Wildgrass blocks have no root behaviors array; addmerge creates it. "
    "Requires Wildgrass or Wildgrass Fork."
)


def write_json(path: Path, data: list) -> None:
    text = json.dumps(data, indent="\t", ensure_ascii=False)
    path.write_text(text + "\n", encoding="utf-8")


def main() -> None:
    ecology_path = PATCH_DIR / "wildgrass-ecology.json"
    templates = {p["file"]: p for p in json.loads(ecology_path.read_text(encoding="utf-8"))}

    ecology_out: list[dict] = []
    first = True
    for patch in templates.values():
        for mod in MODS:
            entry = {
                "file": patch["file"],
                "side": "server",
                "op": "addmerge",
                "path": patch["path"],
                "value": patch["value"],
                "dependsOn": [{"modid": mod}],
            }
            if first:
                entry = {"comment": ECOLOGY_COMMENT, **entry}
                first = False
            ecology_out.append(entry)

    handbook_out: list[dict] = []
    first = True
    for plant in PLANTS:
        for mod in MODS:
            entry = {
                "file": f"wildgrass:blocktypes/plant/{plant}.json",
                "op": "addmerge",
                "path": "/behaviors",
                "value": [{"name": "ecosystemHandbook"}],
                "side": "server",
                "dependsOn": [{"modid": mod}],
            }
            if first:
                entry = {"comment": HANDBOOK_COMMENT, **entry}
                first = False
            handbook_out.append(entry)

    write_json(ecology_path, ecology_out)
    write_json(PATCH_DIR / "wildgrass-handbook.json", handbook_out)
    print(f"Wrote {len(ecology_out)} ecology + {len(handbook_out)} handbook patches.")


if __name__ == "__main__":
    main()
