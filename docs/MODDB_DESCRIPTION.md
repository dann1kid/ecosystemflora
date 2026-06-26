# Ecosystem - Flora — ModDB description

> Текст ниже — черновик описания для страницы на ModDB / mods.vintagestory.at.

## Admin FAQ (HTML — paste at top of ModDB Description)

**Файл:** [`MODDB_ADMIN_FAQ.html`](MODDB_ADMIN_FAQ.html) — готовый блок с inline-стилями (пергамент + зелёный заголовок, три карточки рецептов, таблица FAQ).

**Как вставить:** ModDB → Edit mod → Description → вставить содержимое файла **самым первым** (до changelog и «Your world comes alive»). Поле принимает HTML; если редактор режет теги — переключиться в исходник / убрать `linear-gradient` на заголовке.

**Зачем:** ответы на типичные вопросы админов (sparse, treehouses, redwood, lifespan, global spread) видны до длинного feature list.

---

## 4.3.0 — ModDB update (short paste)

```
4.3.0 — berry colony ecology (since 4.2.0)

• Wild berries spread as colonies — mat edge (rhizome, suckers, stolons) plus optional seed jumps (RhizomeSeedDispersal settings).

• Per-species models: currants (edge clumps + seed), blueberry (forest rhizome), raspberry/blackberry (edge thickets), strawberry (runners + seed), cranberry/cloudberry (peat/bog mats), beautyberry (seed shrub, radius only).

• Context and niche tuning; blueberry/cranberry no longer require tree symbiosis. EnableBerryColonySpread.

Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
```

Full notes: [docs/CHANGELOG.md](CHANGELOG.md).

---

## 4.2.0 — ModDB update (short paste)

```
4.2.0 — simulation visibility (since 4.1.5)

• Fern phenology — dormant / sporulating / dieback phase blocks; orphan symbionts fade via dieback; spread off-season and in dieback. EnableFernPhenology.

• Tallgrass phenology — winter dormant and stress dieback visuals; spread off in dormant/dieback. EnableTallgrassPhenology.

• Berry spread maturation — spread offspring mature through cutting state; tuned density for blackberry, raspberry, currants.

• Stump decay — senescent snag stumps remove after configurable game years (saved with the world). EnableStumpDecay, StumpDecayYears.

• Inspect (I) — recent ecology events (dieback, stress death, spread) at the bottom of the report when EnableEcologyHistoryHint is on.

• Handbook — fixed cross-page links (en/ru). New preset vanilla-minimal; optional JSON presets in ModConfig/ecosystemflora.presets/.

• Default ApplyCrossHabitatSpacing true. Mycelium tree-cut is notify-only (gradual stress, not instant removal).

Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
```

Full notes: [docs/CHANGELOG.md](CHANGELOG.md).

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
3.8.0 — simulation engine (Phase 6 complete)

• Chunk-fair spread — round-robin across all loaded ecology chunks (EnableChunkFairSpread, default on).
• Event wake — plants retry spread after breaks, placement, displacement, soil changes (EnableEventDrivenSpread).
• Two-phase placement — evaluate then commit with revalidation (EnableTwoPhaseSpreadPlacement).
• Season coarse wake — seasonal species wake each in-game month (EnableSeasonCoarseWake).
• Fast registration — player-vicinity chunks first; burst on load; background column scan; paced registry apply (EnableBackgroundRegistrationScan, MaxRegistryAppliesPerTick).
• Vines (column pass) and mycelium anchors (chunk BE scan) register on load; same reproduce loop and chunk-fair spread.
• Seasonal canopy — separate main-thread foliage sync when background scan is on (FoliageSyncMode chunk).
• Empty-first spread — scans empty cells first; displacement still runs when no vacancy (EnableEmptyFirstSpreadCollect). Column occupancy hint skips known plant columns (EnableSpreadColumnOccupancyHint).
• Desynced server ticks (2 s spread / 2.3 s registration / 5.5 s stress). Fallen sticks land on surface below crown. Less ecology wake when breaking blocks without ecology or forest context (e.g. loose sticks; leaves/logs still wake).

Full ecology in loaded chunks — tune MaxSpreadAttemptsPerChunkPerTick / MaxSpreadChunksVisitedPerTick on powerful hardware. Handbook updated (en/ru). Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
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

**HTML для ModDB (основной текст «Your world comes alive» → «Easy to tune»):** [`MODDB_DESCRIPTION.html`](MODDB_DESCRIPTION.html) — вставить в поле Description на mods.vintagestory.at (принимает HTML).

**Короткий admin-FAQ сверху (опционально):** [`MODDB_ADMIN_FAQ.html`](MODDB_ADMIN_FAQ.html).

Ниже — markdown-черновик того же содержания (для редактирования в репо).

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

**Full reference:** [`docs/CONFIGURATION.md`](CONFIGURATION.md) — every key, default, and description (synced with `EcosystemConfig.cs`). In-game handbook: *Configuration Guide* (presets + Phase 6 summary).

Third-party block JSON (not in `ecosystemflora.json`): `ecologySpreadMode` — `"rhizome"`, `"surfacemat"`, or `"independent"` (**3.1.3+**). See [`THIRD_PARTY_ECOLOGY.md`](THIRD_PARTY_ECOLOGY.md).

#### Quick start — balance presets

Set `"BalancePreset"` to one of:


| Preset                  | Style           | Attempts/yr | Chance | Fitness | Spacing  |
| ----------------------- | --------------- | ----------- | ------ | ------- | -------- |
| `"natural"` *(default)* | Realistic       | 72          | 0.50   | 0.45    | 1 block  |
| `"lush"`                | Greener, denser | 120         | 0.65   | 0.35    | 1 block  |
| `"sparse"`              | Minimal, subtle | 36          | 0.30   | 0.60    | 2 blocks |
| `"custom"`              | Manual          | —           | —      | —       | —        |


Presets overwrite **5 fields** on startup: `ReproduceAttemptsPerYear`, `ReproduceChance`, `MinFitness`, `DefaultSameSpeciesSpacing`, `DefaultOtherSpeciesSpacing`. Set `"custom"` to use your own values.

#### Common admin tweaks

| Goal | Keys |
|------|------|
| Slower spread | `BalancePreset: "sparse"` or lower `ReproduceAttemptsPerYear` / `ReproduceChance` |
| No wild tree death | `EnableTreeSenescence: false` (or `EnableTreeAging: false` for no growth) |
| Spread only near players | `LimitSpreadNearPlayers: true` (registration still global) |
| Full ecology only near players | `OnlyActivateNearPlayers: true` (also limits chunk scans — playtest) |
| Phase 6 fairness (defaults on) | `EnableChunkFairSpread`, `EnableEventDrivenSpread`, `EnableTwoPhaseSpreadPlacement` |
| Server spread logs | `VerboseLogging: true` + `ReproduceDebug: true` |

All other keys — spread, trees, canopy, mycelium, ferntree, vines, performance budgets — are in [`CONFIGURATION.md`](CONFIGURATION.md).

### Requirements

- Vintage Story **1.22+**
- Do **not** run alongside Wild Farming Revival

### Credits

Originally inspired by JakeCool19's Wild Farming (v1.2.0). Fully rewritten.