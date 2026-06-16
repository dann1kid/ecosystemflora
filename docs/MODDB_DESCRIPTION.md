# Ecosystem - Flora — ModDB description

> Текст ниже — черновик описания для страницы на ModDB / mods.vintagestory.at.

---

## 3.7.0 — ModDB update (short paste)

```
3.7.0 — since 3.6.0

• Tree fern — vanilla ferntree-normal columns join the ecosystem: yearly aging, spread of young columns, phased senescence (~80 y). Counts as a tree host for symbiotic ferns. Toggle: EnableFerntreeEcology.

• Canopy — autumn partially strips branchy crown leaves; loose sticks may fall to the ground. In spring, older wild trees bud more branchy foliage from calendar age at the trunk base.

• Wild vines — wildvine tips grow downward and spread onto nearby vertical faces (trunks, walls). Toggle: EnableWildVineEcology.

Press I on ferntrees and trunk logs. Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
```

---

## 3.7.1 — ModDB update (short paste)

```
3.7.1 — flora coverage + meadow balance

• Red top grass — grass colonizer (competes with tallgrass), not a meadow flower.
• Brown sedge, croton, rafflesias, barrel/silver-torch cactus, frosted tallgrass — full ecology profiles.
• Turf colonizers skip empty-cell spread bonus so they invade existing grass instead of “garden fill”.

Default preset is natural; use BalancePreset sparse for patchier wilderness.
```

---

## 3.8 — ModDB update (short paste, Phase 6)

```
3.8 — simulation engine (spread scheduling)

• Chunk-fair spread — round-robin across all loaded ecology chunks (not just near players). EnableChunkFairSpread (default on).
• Event wake — nearby plants react to breaks, placement, displacement, and soil changes; calendar spread remains as fallback. EnableEventDrivenSpread (default on).
• Optional reproduce-tick profiling for large worlds (EnableReproduceTickProfiling).

Full scope in loaded chunks; tune MaxSpreadAttemptsPerChunkPerTick / MaxSpreadChunksVisitedPerTick on powerful hardware.
```

---

## Screenshots (gallery note)

Dense meadow images are from **early builds** or **lush / aggressive tuning** on purpose — captions on the gallery say so. Default gameplay uses **`BalancePreset: natural`** (~72 spread attempts/year). For a wilder, patchier look (less “curated European garden”), use **`sparse`** or lower `ReproduceAttemptsPerYear` / raise `MinFitness` in `ModConfig/ecosystemflora.json`.

---

## 3.7.0 — release notes (paste for update)

**Since 3.6.0**

**Tree fern** — vanilla `ferntree-normal-*`: register, yearly aging, spread young columns, phased senescence. Symbiosis tree host. `EnableFerntreeEcology`.

**Canopy** — partial autumn branchy strip (default 0.35); fallen **loose sticks** when branchy strips; spring **branchy buds** scale with tree calendar age.

**Wild vines** — `wildvine-end-*` tips extend downward and capture adjacent vertical wall faces. `EnableWildVineEcology`.

Full notes: [docs/CHANGELOG.md](CHANGELOG.md).

---

## 3.6.0 — release notes (paste for update)

**Since last public release 3.1.12**

**Wild tree aging (3.6)** — calendar age + slow yearly growth; **phased senescence** over four game years (crown leaves → branchy skeleton → dry snag → **stump + fallen logs**). Age persists in saves. Inspect (I) on trunk logs shows phase.

**Seasonal canopy (3.2)** — deciduous autumn defoliation and spring bud on log-grown trees. `EnableSeasonalFoliage`.

**Canopy ambience (3.5)** — client leaf particles under tall crowns. `EnableCanopyAmbience`.

**Handbook** — nine en/ru pages rewritten.

**Config** — `OnlyActivateNearPlayers` defaults to **false** (ecology in all loaded chunks).

Full notes: `[docs/CHANGELOG.md](CHANGELOG.md)` (EN + RU + long ModDB paste block).

---

## Short description (one-liner)

Living wild flora: flowers, grass, ferns, berries, reeds, trees, and **mycelium niches** spread naturally, compete for space, and follow seasons — all on vanilla blocks.

---

## What changed in 3.1.x (for players)

**3.1.0** — Other mods can register plants via JSON (`ecologyParticipant`). Optional berry trait mutation on spread.

**3.1.2** — Soil succession rebalanced: meadows enrich soil; heather and western gorse slightly dry poor soils. Soil changes skip columns with slabs/builds above (Terrain Slabs friendly).

**3.1.3–3.1.5** — **Reeds, tule, and papyrus** spread from the **edge** of a stand (rhizome mat), not random radius jumps. Rare seed/fragment jumps colonize distant banks. **Water lily** spreads as a **floating pad mat** on open water with similar rare seed jumps. **Water crowfoot** unchanged (legacy radius spread).

**3.1.6** — Handbook and **inspect (I)** show spread mode, mat edge yes/no, and seed chance. Tune mat spread in config: `UseRhizomeSpreadForReeds`, `UseSurfaceMatSpreadForLilies`, `RhizomeSeedDispersal*`.

**3.1.7** — **Meadow harvest:** empty hotbar slot → flower or tallgrass **block**; **knife** or **scythe** → **drygrass** only. Scythe can mow all meadow flowers (vanilla: horsetail only). Toggle: `EnableFlowerDrygrass`.

**3.1.8** — **Legacy saves:** old `EcoSystemLife` / `EcosystemPlant` block entities are stripped when chunks load (mod can be removed after a save). **Inspect (I)** dialog no longer crashes. Handbook tree page: fixed `{{wood}}` placeholder.

**3.1.9** — Wild spread no longer overwrites **torches**, **loose stones**, and similar replaceable debris (spread targets **air only**). **Terrain Slabs:** soil succession no longer turns smoothed slab ground into full blocks when fertility drops to barren.

**3.1.10** — **Meadow harvest:** flowers broken without knife/scythe drop as **blocks in the world** (not into inventory); **tallgrass** breaks with **no drop** (use knife/scythe for drygrass). **`MeadowHarvestRegistry`** + `ecologyMeadowHarvest` for herbalism mods. **Inspect (I):** fixed crash when opening the dialog (including on tallgrass). Client reads `ecosystemflora.json` for inspect toggles.

**3.1.11** — **Trees:** saplings no longer spread onto **lake ice, glacier ice, or snow**; tree spread uses the same physical gates as flowers (soil, fluid, mycelium). **Winter tree spread disabled** (Nov–Feb multiplier zero). Mature trunks are still not removed by mod stress death — growth remains vanilla treegen.

**3.1.12** — **Mycelium ecology** around vanilla mushroom anchors (`BlockEntityMycelium`): meadow spread penalty near **forest** mycelium, forest understory bonus, meadow mushrooms **coexist** with flowers/grass (spread onto meadow mycelium; network under existing flowers). Anchor **stress/death** in wrong niche; slow **network spread** from mat edge; tree-cut cascade for forest types. **Inspect (I)** on **mushroom caps** and **soil** (`forestfloor`, `soil-*`, peat, logs). **Config:** missing keys auto-added to `ModConfig/ecosystemflora.json` on startup.

**3.2.0** — **Seasonal canopy phenology:** deciduous trees partially drop branchy leaves in autumn and bud again in spring (in-memory cellular automaton on log-grown skeleton; no custom blocks). Toggle: `EnableSeasonalFoliage`.

**3.5.0** — **Canopy ambience:** subtle leaf particles and flutter near tree crowns (client-side; respects view distance and particle settings). Autumn crown sync fix for mixed foliage states.

**3.6.0** — **Wild tree maturation:** calendar age + slow structure growth once per game year (`EnableTreeAging`, loaded chunks). **Senescence:** phased death at species horizon — four yearly stages: strip crown leaves (spread stops), strip branchy skeleton, short standing trunk (`TreeSenescenceSnagBlocks`, default 3), then **stump + fallen logs** (`EnableTreeSenescenceRemains`, `TreeSenescenceFallenLogCount`). Age **persists in saves** including senescence phase. **Inspect (I)** on trunk logs. **Handbook (en/ru):** nine pages — overview, species groups, trees, canopy, inspect, config. **`OnlyActivateNearPlayers`** default **false** (ecology in all loaded chunks).

**3.7.0** — **Tree fern:** `ferntree-normal-*` register, spread, aging, senescence (`EnableFerntreeEcology`). **Canopy:** partial branchy autumn strip, fallen sticks under crown, spring branchy buds × tree age. **Wild vines:** tip spread down + wall capture (`EnableWildVineEcology`). See [FERNTREE.md](FERNTREE.md), [WILD_VINE.md](WILD_VINE.md), [CANOPY_PHENOLOGY.md](CANOPY_PHENOLOGY.md).

Press **I** on any wild plant, mushroom cap, tree trunk, or mycelium soil to debug spread timing, stress, and mat status. Enable **`VerboseLogging`** + **`ReproduceDebug`** in config for server log detail.

---

## Full description

### Your world comes alive

Ecosystem - Flora turns static worldgen flora into a living ecosystem. Flowers spread across meadows, ferns creep under the forest canopy, reeds colonize shorelines, and mature trees seed new saplings — all without replacing any vanilla blocks.

Install the mod, load your world, and watch it change over the seasons.

### What spreads

- **20 flower species** — daisies, cornflowers, poppies, catmint, heather, and more
- **Tallgrass** — fills in as a grass matrix under flowers
- **5 fern species** — forest understory that needs shade and moisture
- **10 wild berry bushes** — blueberry, cranberry, strawberry… (in **1.22+**, wild berry spread can **clone parent traits** when `CloneBerryTraits` is on — see config)
- **14 tree species** — mature trunks spread free saplings; **v3.6** calendar aging, phased senescence, stump + fallen logs on death, inspect age/size/phase — full cycle in [docs/TREE_AGING.md](TREE_AGING.md)

### Wild tree lifecycle (v3.6)

1. **Spread** — mature `log-grown` seeds `sapling-*-free` (winter off; no ice/snow).
2. **Maturation** — vanilla treegen; ecology registers trunk base at **calendar age 0**.
3. **Life** — each game year: age +1, slow structure growth, sapling spread (no stress death on trunks).
4. **Senescence** — after species lifespan, **four game years**: crown leaves → branchy skeleton → dry snag → **stump + fallen debarked logs**.
5. **Aftermath** — remains are vanilla choppable blocks (not `log-grown`); neighbours refill gaps via normal spread.

Press **I** on any trunk log for age, size index, and senescence phase. Config: `EnableTreeAging`, `EnableTreeSenescence`, `EnableTreeSenescenceRemains`, `TreeSenescenceFallenLogCount`.

- **Reeds, tule, and papyrus** — shore and shallow water over gravel beds
- **Water lily** — spreads across open water surfaces
- **Water crowfoot** — underwater column plant, 2–8 blocks deep (legacy radius spread; mat logic not applied yet)
- **Vanilla mushrooms** — no new blocks; **mycelium ecology** (v3.1.12) adds niche competition, anchor stress, slow network spread, and **inspect (I)** on caps and soil
- **Seasonal tree canopy** (v3.2+) — deciduous partial autumn defoliation and spring bud on existing log-grown trees; optional ambience particles (v3.5)

### Not just spreading — competing

Plants don't blindly fill every empty cell. They **compete**:

- A stronger species can **displace** a weaker one from an occupied cell
- Plants stuck in the wrong niche (too dry, wrong soil, not enough light) accumulate **stress and die**
- Forest flowers and ferns depend on nearby trees — cut the tree, and its understory gradually **withers**
- Fast colonizers grab cleared ground first, but get **replaced** by slower perennials over time

### Seasons matter

Each species follows its own **12-month seasonal curve** — spread peaks in species-appropriate months and stress accumulates in harsh ones:

- **Spring** — meadows bloom; colonizers rush cleared ground
- **Summer** — steady peak for most species
- **Fall** — annuals die off; perennials slow down
- **Winter** — dormancy; annuals die, hardy perennials survive

### Paths form where you walk

Players trample nearby plants over time. Walk the same route often enough and flora dies off, soil degrades, and a natural path appears. Leave the area alone and the ecosystem will reclaim it.

*Trampling is experimental and off by default — enable it in the config.*

### Ecology inspect (hotkey **I**)

Press **I** on any wild plant, **tree trunk**, **mushroom cap**, or **mycelium soil block** for spread timing, stress, seasons, tree age/size, mat status, and mycelium niche. See in-game handbook pages *Ecology Inspect*, *Trees*, and *Seasonal Canopy*.

*Tunable in `ModConfig/ecosystemflora.json`: `EnableEcologyInspect`, `EcologyInspectCooldownSeconds`, `EcologyInspectScanRadius`, `EnableEcologyAreaScan`.

### Harvest balance (flowers)

Break wildflowers without knife/scythe to collect the **flower block on the ground**. **Tallgrass** breaks with **no drop** unless you use **knife** or **scythe** (drygrass). Turn off with `EnableFlowerDrygrass: false`. Herbalism mods: see `docs/THIRD_PARTY_ECOLOGY.md` (`MeadowHarvestRegistry`).

### Your world stays safe

The mod does not replace vanilla worldgen blocks. Vanilla parents stay vanilla when you remove the mod. **Optional integration:** other mods can register their own plant blocktypes via JSON (`ecologyParticipant`, etc.); see the repo [docs/THIRD_PARTY_ECOLOGY.md](THIRD_PARTY_ECOLOGY.md) or turn it off with `EnableThirdPartyParticipants: false` if you want only built-in vanilla paths.

### Respects your builds

Wild spread, displacement, and stress death are blocked inside **land claims**. Your gardens and farms are safe.

Plants never spread onto cells with an **active mycelium block entity** — mushrooms continue to regrow naturally. **3.1.12:** forest mycelium softens meadow spread nearby; meadow mushrooms do not repel flowers. Unclaimed empty **farmland** can be colonized by wild plants over time, and those plants slowly restore soil nutrients (fallow restoration).

### Soil succession (optional)

With **`UseSoilSuccession`** on (default), meadow spread and natural plant death **enrich** soil tiers over time. **Heather** and **western gorse** are exceptions — they slightly dry poor soils while growing. Prefer spread and competition only, with no soil block swaps? Set **`UseSoilSuccession: false`**. **`SoilSuccessionSkipWhenBuiltAbove`** (default on) skips soil changes under slabs and builds — compatible with **Terrain Slabs** and similar mods.

### Aquatic spread (reeds & lily)

**Cattail, bulrush, and papyrus** use **rhizome mat** spread: only plants on the **edge** of a stand step one block along the shore. Rare **seed/fragment** jumps (~6–10% of attempts) can colonize distant bank cells. **Water lily** uses a **floating pad mat** (eight-connected edge on open water) with ~5% seed jumps. Press **I** on any plant for mat edge and seed chance in the inspect report.

Turn off mat logic and restore legacy radius-4 spread for reeds: **`UseRhizomeSpreadForReeds: false`**. Lily mat: **`UseSurfaceMatSpreadForLilies`**.

### Mycelium ecology  

Uses vanilla **`BlockEntityMycelium`** only — mushroom caps and regrowth stay vanilla.

- **Forest mycelium** — soft penalty on **meadow** plant spread within ~7 blocks; bonus for **forest** understory (ferns, etc.)
- **Meadow mycelium** — **coexists** with flowers and tallgrass; does not block meadow spread onto its ground cell
- **Wrong niche** — meadow anchors stressed in dense forest; forest anchors need a nearby tree host; failed checks remove the BE (not the whole mod)
- **Network spread** — slow orthogonal steps from the **edge** of a mat; different mushroom species can displace each other
- **Tree cut** — forest mycelium anchors near a felled trunk accumulate stress (meadow / trunk polypore exempt)
- **Inspect (I)** — aim at a **mushroom cap** or **forest soil** block for niche, stress, registry, network edge, next spread timing

Toggle in config: **`EnableMyceliumNiche`**, **`EnableMyceliumEcology`**, **`EnableMyceliumNetworkSpread`** (all default **true**).

### Greenhouse support

Flowers planted inside a fully enclosed glass-roofed room (a greenhouse) are protected from temperature stress — no land claim required. Grow tropical flowers in cold biomes inside your greenhouse.

### Easy to tune

Edit `ModConfig/ecosystemflora.json` (created on first server launch). Full example: `assets/ecosystemflora/ecosystemflora.example.json` in the mod package.

**Upgrading:** after an update, launch once — the mod **rewrites** your config file with any **new keys** at default values. Your existing settings are kept. Server always; client when a config file already exists.

#### Config keys added or changed since 3.1.2


| Key                                     | Default | Since                 | What it does                                                                                                    |
| --------------------------------------- | ------- | --------------------- | --------------------------------------------------------------------------------------------------------------- |
| `SoilSuccessionSkipWhenBuiltAbove`      | true    | **3.1.2** / **3.1.9** | Skip soil tier swaps under slabs/builds; **3.1.9** also protects Terrain Slabs ground blocks (`terrainslabs:`*) |
| `UseRhizomeSpreadForReeds`              | true    | **3.1.3**             | Cattail/tule/papyrus: rhizome **mat edge** spread. **false** = legacy radius-4                                  |
| `RhizomeSeedDispersalEnabled`           | true    | **3.1.4**             | Rare virtual seed/fragment jumps for reed and lily mats (no item seeds)                                         |
| `RhizomeSeedDispersalChanceScale`       | 1.0     | **3.1.4**             | Multiplier on per-species seed jump chance                                                                      |
| `RhizomeSeedDispersalFitnessScale`      | 0.25    | **3.1.4**             | Harder establishment for distant seed landing sites                                                             |
| `UseSurfaceMatSpreadForLilies`          | true    | **3.1.5**             | Water lily: floating **pad mat** on open water. **false** = legacy radius spread                                |
| `EnableFlowerDrygrass`                  | true    | **3.1.10**            | Meadow harvest: **flowers** → block in world; **tallgrass** → no drop (knife/scythe → drygrass)                 |
| `EnableMyceliumNiche`                   | true    | **3.1.12**            | Meadow spread penalty / forest bonus near vanilla mycelium anchors                                              |
| `EnableMyceliumEcology`                 | true    | **3.1.12**            | Register mycelium BE: stress/death, inspect (I) on caps and soil                                                |
| `EnableMyceliumNetworkSpread`           | true    | **3.1.12**            | Slow orthogonal network spread from mat edge                                                                    |
| `MyceliumZoneRadius`                    | 7       | **3.1.12**            | Niche zone radius (matches vanilla growRange)                                                                   |
| `MyceliumMeadowSpreadPenalty`           | 0.35    | **3.1.12**            | Meadow spread fitness at anchor (tapers to 1.0 at zone edge)                                                    |
| `MyceliumForestSpreadBonus`             | 1.22    | **3.1.12**            | Forest understory spread bonus at anchor                                                                        |
| `MyceliumSkipSoilSuccession`            | true    | **3.1.12**            | Skip soil succession / fallow drip on mycelium anchor cells                                                     |
| `MyceliumTreeHostRadius`                | 4       | **3.1.12**            | Tree-host search for forest mycelium survival                                                                   |
| `MyceliumForestMinForestCover`          | 0.12    | **3.1.12**            | Below this, forest mycelium stressed in open context                                                            |
| `MyceliumMeadowMaxForestCover`          | 0.45    | **3.1.12**            | Above this, meadow mycelium stressed                                                                            |
| `MyceliumSpreadRate`                    | 0.12    | **3.1.12**            | Scales mycelium network spread interval                                                                         |
| `MyceliumSpreadAttemptsPerYear`         | 4       | **3.1.12**            | Network spread attempts per game year at rate 1.0                                                               |
| `MyceliumSpreadMinFitness`              | 0.35    | **3.1.12**            | Min fitness to colonize / displace neighbor anchor                                                              |
| `EnableTreeAging`                       | true    | **3.6.0**             | Yearly calendar age + structure growth for registered log-grown trunks                                          |
| `EnableTreeSenescence`                  | true    | **3.6.0**             | Phased natural death after species lifespan                                                                     |
| `TreeSenescenceSnagBlocks`              | 3       | **3.6.0**             | Trunk blocks left during snag year                                                                              |
| `EnableTreeSenescenceRemains`           | true    | **3.6.0**             | Final year: vanilla stump + fallen logs (not log-grown)                                                         |
| `TreeSenescenceFallenLogCount`          | 3       | **3.6.0**             | Horizontal logs scattered near stump (0 = stump only)                                                           |
| `MaxTreeGrowthAttemptsPerTick`          | 6       | **3.6.0**             | Cap structure growth work per server tick                                                                       |
| `TreeGrowthActivityScale`               | 1.0     | **3.6.0**             | Scale tree growth pacing                                                                                        |
| `EnableFerntreeEcology`                 | true    | **3.7.0**             | Tree fern register, spread, aging                                                                               |
| `FerntreeSenescenceSnagSegments`        | 2       | **3.7.0**             | Snag trunk height (ferntree)                                                                                    |
| `FoliagePeakAutumnBranchyStripActivity` | 0.35    | **3.7.0**             | Partial branchy strip threshold (0 = keep all)                                                                  |
| `EnableCanopyFallenSticks`              | true    | **3.7.0**             | Drop loose sticks when branchy strips                                                                           |
| `CanopyFallenStickChance`               | 0.42    | **3.7.0**             | Stick drop chance scale at peak autumn                                                                          |
| `EnableSpringBranchyAgeBoost`           | true    | **3.7.0**             | Spring branchy buds scale with tree age                                                                         |
| `SpringBranchyAgeBoostYearsToMax`       | 60      | **3.7.0**             | Years to max spring branch boost                                                                                |
| `SpringBranchyAgeBoostMax`              | 1.5     | **3.7.0**             | Max spring branchy bud multiplier                                                                               |
| `EnableWildVineEcology`                 | true    | **3.7.0**             | Wild vine tip spread                                                                                            |
| `WildVineWallCaptureRadius`             | 4       | **3.7.0**             | Horizontal wall-face scan                                                                                       |
| `WildVineWallCaptureHeight`             | 6       | **3.7.0**             | Vertical wall-face scan                                                                                         |
| `EnableSeasonalFoliage`                 | true    | **3.2.0**             | Deciduous autumn defol / spring bud on log-grown crowns                                                         |
| `EnableCanopyAmbience`                  | true    | **3.5.0**             | Client leaf particles near tall crowns                                                                          |


Related (unchanged keys, but behavior context from **3.1.2**):


| Key                            | Default | Note                                                                                                    |
| ------------------------------ | ------- | ------------------------------------------------------------------------------------------------------- |
| `UseSoilSuccession`            | true    | **3.1.2 balance:** meadows enrich soil on spread/death; heather & western gorse slightly dry poor soils |
| `SoilSuccessionStrength`       | 1.0     | Scale of all soil succession changes                                                                    |
| `EnableEcologyInspect`         | true    | **3.1.6:** inspect (**I**) shows mat edge / seed % on reeds & lily                                      |
| `BerryTraitMutationChance`     | 0.0     | Optional trait loss on berry spread (**3.1.1**; 0 = off)                                                |
| `EnableThirdPartyParticipants` | true    | JSON `ecologyParticipant` on blocktypes (**3.1.0**)                                                     |


Third-party block JSON (not in `ecosystemflora.json`): `ecologySpreadMode` — `"rhizome"`, `"surfacemat"`, or `"independent"` (**3.1.3+**).

#### Quick start — balance presets

Set `"BalancePreset"` to one of:


| Preset                  | Style           | Attempts/yr | Chance | Fitness | Spacing  |
| ----------------------- | --------------- | ----------- | ------ | ------- | -------- |
| `"natural"` *(default)* | Realistic       | 72          | 0.50   | 0.45    | 1 block  |
| `"lush"`                | Greener, denser | 120         | 0.65   | 0.35    | 1 block  |
| `"sparse"`              | Minimal, subtle | 36          | 0.30   | 0.60    | 2 blocks |
| `"custom"`              | Manual          | —           | —      | —       | —        |


Presets overwrite **5 fields** on startup: `ReproduceAttemptsPerYear`, `ReproduceChance`, `MinFitness`, `DefaultSameSpeciesSpacing`, `DefaultOtherSpeciesSpacing`. Set `"custom"` to use your own values.

#### Feature toggles (true/false)


| Setting                            | Default | What it does                                                                                                                              |
| ---------------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `EcosystemEnabled`                 | true    | Master switch — disable all spread/competition                                                                                            |
| `UseSeasonalEcology`               | true    | Spread rates follow spring/summer/fall/winter                                                                                             |
| `SeasonalStressEnabled`            | true    | Winter and fall die-off                                                                                                                   |
| `UseCellDisplacement`              | true    | Stronger species displace weaker ones                                                                                                     |
| `EnableStressDeath`                | true    | Plants in wrong niche die over time                                                                                                       |
| `EnableSymbiosis`                  | true    | Some species boost each other                                                                                                             |
| `UseFloraContext`                  | true    | Forest interior/edge affects spread                                                                                                       |
| `UseNicheContext`                  | true    | Local soil + climate niche scoring                                                                                                        |
| `UseSoilSuccession`                | true    | Wild plants gradually enrich soil on spread/death (**3.1.2:** meadows +; heather/gorse dry). **false** = spread only, no soil block swaps |
| `SoilSuccessionSkipWhenBuiltAbove` | true    | **3.1.2** — skip soil swaps under slabs/builds (Terrain Slabs)                                                                            |
| `UseFarmlandNutrientBridge`        | true    | Wild plants enrich tilled farmland N/P/K                                                                                                  |
| `EnableFallowRestoration`          | true    | Healthy plants on farmland slowly restore nutrients                                                                                       |
| `RespectLandClaims`                | true    | No spread/death inside player claims                                                                                                      |
| `PlantSpacingEnabled`              | true    | Minimum distance between plants                                                                                                           |
| `HarshWildPlants`                  | true    | Wild plants enforce survival checks                                                                                                       |
| `ApplyWorldgenRainForest`          | true    | Respect worldgen rain/forest values                                                                                                       |
| `UseCalendarScaledSpread`          | true    | Scale intervals to DaysPerYear                                                                                                            |
| `UseSpeciesSpreadRates`            | true    | Per-species spread rates from ecology table                                                                                               |
| `EnableEcologyInspect`             | true    | Hotkey **I**: ecology report (spread mode, mat edge, seed % — **3.1.6**; mycelium niche — **3.1.12**)                                     |
| `EnableEcologyAreaScan`            | true    | Include nearby-species mix in the inspect dialog                                                                                          |
| `EnableTrampling`                  | false   | Plants near player paths accumulate stress and die                                                                                        |
| `TramplingSoilDegradation`         | false   | Trampled paths lose soil fertility                                                                                                        |
| `EnableFlowerDrygrass`             | true    | **3.1.7** — empty hand → plant block; knife/scythe → drygrass                                                                             |
| `CloneBerryTraits`                 | true    | Wild berry spread copies parent traits (**1.22+**, **3.0**)                                                                               |
| `EnableThirdPartyParticipants`     | true    | Other mods: JSON `ecologyParticipant` on blocktypes (**3.1.0**)                                                                           |
| `UseRhizomeSpreadForReeds`         | true    | **3.1.3** — cattail/tule/papyrus mat edge spread                                                                                          |
| `UseSurfaceMatSpreadForLilies`     | true    | **3.1.5** — water lily floating pad mat                                                                                                   |
| `RhizomeSeedDispersalEnabled`      | true    | **3.1.4** — rare seed jumps for reed & lily mats                                                                                          |
| `RhizomeSeedDispersalChanceScale`  | 1.0     | **3.1.4** — multiplier on seed jump chance                                                                                                |
| `RhizomeSeedDispersalFitnessScale` | 0.25    | **3.1.4** — fitness penalty for distant seed sites                                                                                        |
| `EnableMyceliumNiche`              | true    | **3.1.12** — forest mycelium meadow spread penalty / forest bonus zone                                                                    |
| `EnableMyceliumEcology`            | true    | **3.1.12** — mycelium anchor stress, death, inspect (I)                                                                                   |
| `EnableMyceliumNetworkSpread`      | true    | **3.1.12** — slow mycelium network spread from mat edge                                                                                   |
| `EnableTreeAging`                  | true    | **3.6.0** — wild trunk calendar age + structure growth                                                                                    |
| `EnableTreeSenescence`             | true    | **3.6.0** — phased senescence (leaves → skeleton → snag → stump/logs)                                                                     |
| `TreeSenescenceSnagBlocks`         | 3       | **3.6.0** — standing trunk height during snag year                                                                                        |
| `EnableTreeSenescenceRemains`      | true    | **3.6.0** — leave choppable stump + fallen logs on final year                                                                             |
| `TreeSenescenceFallenLogCount`     | 3       | **3.6.0** — ground logs near stump (0 = stump only)                                                                                       |
| `EnableFerntreeEcology`            | true    | **3.7.0** — tree fern ecology                                                                                                             |
| `EnableCanopyFallenSticks`         | true    | **3.7.0** — loose sticks when branchy strips                                                                                              |
| `EnableSpringBranchyAgeBoost`      | true    | **3.7.0** — spring branchy buds × tree age                                                                                                |
| `EnableWildVineEcology`            | true    | **3.7.0** — wild vine tip spread                                                                                                          |
| `EnableSeasonalFoliage`            | true    | **3.2.0** — deciduous seasonal crown phenology                                                                                            |
| `EnableCanopyAmbience`             | true    | **3.5.0** — client crown leaf ambience                                                                                                    |
| `MyceliumSkipSoilSuccession`       | true    | **3.1.12** — no soil succession on mycelium anchor cells                                                                                  |


#### Mycelium tuning (numbers, **3.1.12**)


| Setting                         | Default | What it does                                        |
| ------------------------------- | ------- | --------------------------------------------------- |
| `MyceliumZoneRadius`            | 7       | Chebyshev radius for niche zone (vanilla growRange) |
| `MyceliumMeadowSpreadPenalty`   | 0.35    | Meadow spread fitness at forest mycelium anchor     |
| `MyceliumForestSpreadBonus`     | 1.22    | Forest understory spread bonus at anchor            |
| `MyceliumTreeHostRadius`        | 4       | Horizontal tree search for forest anchor survival   |
| `MyceliumForestMinForestCover`  | 0.12    | Forest anchor stress below this cover in open       |
| `MyceliumMeadowMaxForestCover`  | 0.45    | Meadow anchor stress above this cover               |
| `MyceliumSpreadRate`            | 0.12    | Network spread interval scale (lower = slower)      |
| `MyceliumSpreadAttemptsPerYear` | 4       | Network attempts per year at spread rate 1.0        |
| `MyceliumSpreadMinFitness`      | 0.35    | Min fitness to spread to / displace neighbor cell   |


#### Ecology inspect tuning


| Setting                         | Default | What it does                                               |
| ------------------------------- | ------- | ---------------------------------------------------------- |
| `EcologyInspectCooldownSeconds` | 2.0     | Min seconds between inspect requests per player            |
| `EcologyInspectScanRadius`      | 16      | Radius (blocks) for nearby-species tally in inspect dialog |


#### Spread tuning (numbers)


| Setting                    | Default | What it does                            |
| -------------------------- | ------- | --------------------------------------- |
| `ReproduceAttemptsPerYear` | 72      | Spread attempts per game year           |
| `ReproduceChance`          | 0.50    | Base chance per attempt                 |
| `MinFitness`               | 0.45    | Minimum fitness to reproduce            |
| `ReproduceRadius`          | 4       | Max horizontal spread distance (blocks) |
| `ReproduceVerticalSearch`  | 5       | Y-axis search range                     |
| `MaxFailedSurvivalChecks`  | 5       | Failed checks before plant dies         |
| `GrowthHoursMultiplier`    | 1.0     | Scale sapling → mature growth time      |


#### Competition & niche tuning


| Setting                          | Default | What it does                                                     |
| -------------------------------- | ------- | ---------------------------------------------------------------- |
| `DisplacementHoldMargin`         | 1.18    | Challenger must beat incumbent × this                            |
| `EmptySpreadFitnessMultiplier`   | 2.5     | Empty-cell spread weight when mixed with displacement            |
| `NicheStressThreshold`           | 0.45    | Niche score below this = stress                                  |
| `SoilSuccessionStrength`         | 1.0     | Speed of soil tier changes                                       |
| `BerryTraitMutationChance`       | 0.0     | **3.1.1** — chance berry spread drops one parent trait (0 = off) |
| `FarmlandNutrientBridgeStrength` | 1.0     | Scale of till nutrient bonus                                     |
| `FallowRestorationStrength`      | 1.0     | Scale of fallow restoration bonus                                |
| `FloraOpenInteriorPenalty`       | 0.35    | Penalty for open-field species in forest                         |


#### Trampling tuning


| Setting                    | Default | What it does                              |
| -------------------------- | ------- | ----------------------------------------- |
| `TramplingRadius`          | 1       | Detection range around player (blocks)    |
| `TramplingStressThreshold` | 8       | Stress ticks before a trampled plant dies |


#### Performance


| Setting                         | Default | What it does                                                                                                                      |
| ------------------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `TickBudgetMs`                  | 30      | Hard cap on ms per server tick for spread (0 = unlimited)                                                                         |
| `StressBudgetMs`                | 0       | Hard cap for stress tick (0 = use TickBudgetMs)                                                                                   |
| `StressTickIntervalMs`          | 6000    | Interval between stress ticks (ms)                                                                                                |
| `MaxReproduceAttemptsPerTick`   | 64      | Spread checks per server tick                                                                                                     |
| `MaxStressChecksPerTick`        | 16      | Stress checks per tick                                                                                                            |
| `MaxChunkColumnsScannedPerTick` | 6       | Chunk registration pacing (unfinished chunks stay queued)                                                                         |
| `MaxRegistrationsPerTick`       | 512     | Max new plant registrations per server tick                                                                                       |
| `OnlyActivateNearPlayers`       | false   | **Playtest shortcut** — when true, limit spread/stress/trees/scans to player radius; normal play leaves false (all loaded chunks) |
| `PlayerActivationRadiusBlocks`  | 192     | Radius if above is true                                                                                                           |
| `VerboseLogging`                | false   | Detailed server log output                                                                                                        |
| `ReproduceDebug`                | false   | Log each spread attempt (pair with `VerboseLogging` for balance tuning)                                                           |


All settings work together — presets give a good baseline, toggles let you disable features you don't want, and number fields let you fine-tune the balance.

### Requirements

- Vintage Story **1.22+**
- Do **not** run alongside Wild Farming Revival

### Credits

Originally inspired by JakeCool19's Wild Farming (v1.2.0). Fully rewritten.