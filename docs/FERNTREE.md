# Tree fern ecology (`ferntree`)

Vanilla **tree fern** (`game:ferntree-normal-*`) — arborescent fern columns in tropical wet forest. Separate from lumber trees (`log-grown`) and ground ferns (`fern-*`).

Updated: 2026-06-14.

---

## Lifecycle

| Stage | Behaviour |
|-------|-----------|
| **Register** | Chunk scan finds `ferntree-normal-trunk` base → ecology registry at age **0** |
| **Life** | Each game year (`EnableTreeAging`): age +1; crown top young → medium → old; slow height growth |
| **Spread** | Mature trunk places a **young column** nearby (trunk segments + top-young + side foliage) |
| **Senescence** | After **80** calendar years (`EnableTreeSenescence`): 4 years — strip foliage → remove crown top → short snag (`FerntreeSenescenceSnagSegments`) → remove column |
| **Host** | Counts as **tree host** for symbiotic ground ferns and forest context |

No custom blocks. Stumps/logs do not apply — final year clears the column.

---

## vs lumber trees vs ground ferns

| | Lumber tree | Tree fern | Ground fern |
|--|-------------|-----------|-------------|
| Blocks | `log-grown-*` | `ferntree-normal-*` | `fern-*` |
| Spread | `sapling-*-free` | young column structure | clone bush |
| Aging | trunk + crown growth | top maturity + height | none |
| Stress death | no (trunk) | no | yes |

---

## Config

| Key | Default | Description |
|-----|---------|-------------|
| `EnableFerntreeEcology` | `true` | Register, spread, aging |
| `EnableTreeAging` | `true` | Calendar year tick (shared with trees) |
| `EnableTreeSenescence` | `true` | Phased death after lifespan |
| `FerntreeSenescenceSnagSegments` | `2` | Trunk blocks during snag year |

Inspect (**I**) on any ferntree block resolves to trunk base — age, segment count, crown maturity, senescence phase.
