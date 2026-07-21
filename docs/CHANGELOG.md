# Changelog — Ecosystem - Flora

Player-facing release notes. Dev history: [`PROGRESS.md`](PROGRESS.md).

**Last public release:** **4.7.0** (ModDB)  
**Next release:** **4.11.23** (unreleased) — immature tree spread gate

Requirements: Vintage Story **1.22+**. Do not run alongside Wild Farming Revival.

---

## Unreleased — 4.11.23

- **Immature tree spread** — ecology seedlings no longer unlock wild spread via soft size bypass after a few years of growth; calendar maturity (or full structure estimate) is required unless the trunk was already worldgen-sized at registration. Inspect shows “too young to spread”; immature trees are polled about once per game year instead of every spread interval.

## Unreleased — 4.11.22

- **Tree niche lifespan stress** — climate/forest mismatch accumulates yearly lifespan debt (default +2 hard / +1 soft seral; −1 recovery), shortening effective senescence horizon (cap 50% of species lifespan; 8-year grace). Toggle `EnableTreeNicheLifespanStress`. Inspect shows effective age / debt.

## Unreleased — 4.11.21

- **Dedicated-server `/ecosetup`** — console opens GUI for online admins (or explains that an in-game admin is required); packaging tests fixed (Newtonsoft + migration/lang hints).

## Unreleased — 4.11.20

- **Potato / Weak wizard** — profile **Potato PC** + Weak apply Super-minimal; `/ecoautotune` Weak tier also applies Super-minimal; Super-minimal disables seasonal foliage.
- **X3D / dual-CCD** — wizard profile + bench button: Strong ecology with **2** registration/spread workers, `LimitSpreadNearPlayers`, no load burst; Strong/Balanced auto-tune also use 2 workers (not half of all cores).
- **Paced fern-phase remap** — legacy column remap resumes across frames (~2 ms slices) instead of one hitch per chunk on mass load.
- **Worker rescale on Apply** — registration/spread worker count can change mid-session without world reload.
- **Potato Weak caps** (from 4.11.15+) — near-player, catch-up/phenology/cyclic caps, chunk-load stagger, prep wall-clock.

## Unreleased — 4.11.15

- **Setup wizard overhaul** — welcome page (this mod / this world), full Performance knob table with `val→ CPU↑/↓` hints, `/ecosetup` + `/ecoautotune`; old worlds prompt until completed.
- **Wizard completion stickiness** — `setup-wizard.done` marker + meta flag; SSP stale sync no longer re-opens the wizard after Apply; at most one auto-open per session.
- **Config load fidelity** — known balance presets no longer wipe hand-edited perf/tick fields on load/save; `WildVineMaxHangDepth` in `trees.json`; enabling ecosystem after disabled Init starts runtime ticks.
- **Server chat commands** — `/ecosetup` and `/ecoconfig` registered on the server (open GUI via packet).
- **Boot-order fidelity** — Harmony mycelium patch always applied (displacement gated by world config); third-party ecology attrs always injected at finalize (participation still runtime-gated); flower drygrass drops re-sync on world load and config Apply (restore originals when disabled).
- **Reproduce prep wall-clock** — `TickBudgetMs` caps maturation/phenology/snow/stump/tree prep before foliage/spread; `SpreadBudgetMs` stays independent so prep cannot starve spread.
- **Chunk-load hitch smoothing** — deferred strip/remap/mycelium/registration delays staggered per chunk; load-time registration burst skipped when background scan is on (priority queue + paced snapshot instead).
- **Potato Weak / Super-minimal** — Weak auto-tune enables `OnlyActivateNearPlayers` and lowers foliage catch-up / phenology / cyclic-tree / tallgrass-promotion caps; Super-minimal also disables vicinity rescan, foliage catch-up, cyclic trees, and load burst; chunk-scan tick caches player-chunk sets once.

## Unreleased — 4.10.32

- **Per-world config files** — U/perf settings live in `ModConfig/ecosystemflora/worlds/<WorldName_Seed>/` as category JSON (`perf.json`, `spread.json`, … + `meta.json`). Template: `template/defaults.json`; presets: `presets/`. Legacy SaveGame blob and flat `ecosystemflora.json` migrate automatically. U dialog is fully world-scoped (Apply/Reload).

## Unreleased — 4.10.31

- **Per-world config** — server settings persist per world; setup wizard + light auto-tune (`/ecoautotune`); Performance scan knobs; lighter main-path defaults.

## Unreleased — 4.10.9

- **Smoother cadence** — rarer, desynced ticks (reproduce **3.5 s**, chunk-scan **2.3 s**, stress **8.5 s**, vicinity **5 s**); spread fairer (**1** attempt × **12** chunks); smaller registration pipeline defaults (pending **6**, drain/active snapshots **3**).
- **~3× lighter main defaults** — another cut for mod-pack headroom: snapshot **340**, registration **9 ms**, burst/priority **8 ms**, tick/spread **5/4 ms** (natural **2/1**), reproduce attempts **14**, registry applies **85**, foliage sync **2 ms** / **1** chunk, capture ≤**12** cells, flora rescan **7** columns/tick.
- **Smarter main paths** — registration snapshots copy only a surface band (`RegistrationSnapshotBandBelowSurface` **24**, not full underground); spread runs PreSpawn + near-zero season skip and chance roll before capture; maturation queues no longer burn GetBlock/check budget on not-yet-due juveniles.
- **Perpetual near-player re-scan** — player-vicinity no longer re-queues **finished** chunks into the full registration snapshot pipeline every 2.5 s (that kept one CPU core pegged). Late flora still comes from the light cyclic column scanner. Default `MaxFloraRescanColumnsPerTick` is **20** (property + **natural** balance preset).
- **Perf F1–F12** — spread/registration/foliage queue fixes (F1–F8); tighter environment-cache invalidation without wiping worldgen climate columns (F9); cyclic flora skips unfinished registration chunks so discovery paths do not overlap (F10); vine network BFS reuses thread-local pools (F11); third-party ecology bootstraps share one block-table pass (F12).
- **Perf F6–F8** — foliage pending sync uses an incremental dirty set (no full-map enumerate+sort each tick); orphan BFS reuses thread-local pools and runs in a deferred pass after season catch-up; pending registration drain uses per-chunk queues (no `RemoveAt(0)` shifts).

## Unreleased — 4.10.8

- **Redwood lifespan** — senescence horizon raised to **1000** years.
- **Sapling age catch-up death** — new trunks no longer inherit a size-estimated calendar age; impossible `LastGrowthYear` lags (e.g. 0 from old saves) are snapped instead of burning lifespan from world year 0; stale age saves on seedling trunks are ignored.

## Unreleased — 4.10.7

- **Per-species crown forms** — yearly aging grows foliage inside a species silhouette (`Spreading` oak/maple/walnut, `Oval` birch, `Umbrella` acacia/kapok, `Column` pine/cypress, `Tiered` redwood) instead of a generic broom ball.

## Unreleased — 4.10.6

- **Bare oak tip fix** — spring density and winter skeleton repair no longer treat foliage *below* a protruding trunk tip as “already dressed”, so stick-tops above a leafy mid canopy get branchy again; yearly aging also repairs undressed tips even when height growth has stopped.
- **Upper canopy bias** — yearly tree growth anchors from the upper half of the trunk and tries top anchors first (plus tip dress on trunk extend), so broadleaf crowns widen the top shelf instead of only fluffing the mid canopy.
- **Broadleaf crown targets** — oak soft crown 5→7; maple / crimson king maple / walnut 5→6.

## Unreleased — 4.10.5

- **Redwood trunk inspect** — living `logsection-grown-redwood-*` 2×2 trunk quarters are recognized as ecology trunks (vanilla treegen), so the lower trunk inspects and registers like branch `log-grown` wood.
- **Oak crown fill** — yearly tree aging prefers branchy/leaf spread when the crown lags the trunk (typical after sapling treegen); older lagging trees get extra crown ops.
- **Wild vines with seasonal leaves** — canopy leaf/branchy strip and tree senescence now revalidate attached vine columns so vines no longer float after the host foliage disappears.
- **Wild vines on chunk load** — pending vine registration checks column support (once per column per drain) and prunes already-floating vines before they enter the ecology registry.

## Unreleased — 4.9.13

- **Main-thread chunk-load hitch** — legacy fern remaps scan only a rain-surface band (±2 / +8), not full `MapSizeY`; empty worker flora no longer triggers a second live column rescan on flat/empty chunks.
- **Playable scan defaults** — `ChunkScanTickIntervalMs` 1000 (was 30), foliage scan height 48, `MaxFoliageCellsTickedPerTick` 48; `natural` preset resets these so timelapse leftovers cannot stick.
- **Trail calendar scrub fix** — decay no longer collapses every column when `TotalHours` jumps (time slider); save only snapshots (no full Age/prune); soil sync is **footstep-only**, not on ecology ticks.
- **Timelapse leftover guard** — `ReproduceTickIntervalMs` / `ChunkScanTickIntervalMs` below 100 under non-`timelapse` presets are clamped (custom JSON after timelapse was freezing the time slider at 25ms ecology ticks).
- **Canopy catch-up hitch** — chunk-load budget lowered (`MaxFoliageCatchUpPerChunk` 256); catch-up scales dialed back from the 4.9.12 spike.
- **Juvenile flower textures** — seedlings used `drawnHeight: 11`, which clipped vanilla petal/stem composites into half-flowers; now uses vanilla heights (seedlings stay small via shape `scale: 0.45`).
- **Tree spacing** — trees keep per-wood Chebyshev distances (CSV / `WildTreeEcology`); spacing `0` no longer means “adjacent trunks” (crown-aware fallback). Timelapse no longer writes global spacing defaults of `0`. Redwood reference crown aligned to 8.
- **Fuller deciduous canopies** — spring chunk sync grows `log → leavesbranchy` again (Option B had left thin skeletons); higher leaf/branchy catch-up scales and local density caps so crowns fill more without restoring leave→strip→leave waves.
- **Warm-season leaf keep** — summer Idle no longer force-strips leaves; early/mid autumn defol starts later (≈ Aug/Sep); weak early autumn activity does not drip-strip.
- **Flower phenology stress + senescence** — dieback is deferred via accumulating stress (frost = winter rate, hysteresis exit); plants die after dieback life-cycles (`flower_phenology_life_cycles` in species CSV, else `MaxFlowerPhenologyLifeCycles`, default 4) so perennial meadows turn over.
- **Ecological trails** — foot traffic syncs wild soil grass coverage (`normal`↔`verysparse`, not bare `none`) to column pressure; abandoned trails restore as pressure fades (footstep + world-save age when soil mark is stale, budget-capped). Tempo via `FootTrafficStepsToFullCoverageWear` (default 20). Never drains fertility or farmland.
- **Tallgrass height fix** — establishing grass keeps growing to full environment target after half-target registry (was stuck around `medium`).
- **Floral Zones ecology** — runtime climate/spread injection for **Cape Region** and **Cosmopolitan Region** (211 worldgen-derived entries across all **seven** regional mods; was 142 in 4.7.0).
- **NZ mushroom** — `mushroom-asteliahastata` included when the worldgen patch omits `maxRain`.

## Planned — v5.0 Phase 7 (not in code)

Design only: [`PHASE7_EXTERNAL_SIMULATION.md`](PHASE7_EXTERNAL_SIMULATION.md).

- Ecology continues in **unloaded** chunk columns (not only loaded).
- Compact snapshot export on chunk unload; per-world ecology database.
- Optional external `ecology-sim` worker (Go); mod spawns/stops process on server lifecycle.
- Game applies accumulated block changes on chunk load (main thread); no spread math for distant zones in-process.

---

## 4.7.0 — Third-party wild ecology and simulation polish

**Since 4.5.4**

### Wildcraft Fruit, Wildcraft Trees, Floral Zones

- **Wildcraft Fruit** — worldgen climate and spread attrs injected at asset finalize (berries, herbs, flowers, fruit trees from worldgen patches).
- **Wildcraft Trees** — climate envelope from tree worldgen patches for modded `log-grown` / `sapling` woods.
- **Floral Zones** — five regional mods at release (Caribbean, Central Australia, East Asia, Mediterranean, Neozeylandic); climate from worldgen patches, no JSON patch to parent mods.
- **Fruiting vines (Herbarium)** — **climate and stress only**; `ecologySpreadRate: 0` keeps growth on vanilla/Herbarium BE; no `WildVineEcology` spread.
- **B+ auto-curves** — discovered third-party species auto-append to user `ecology.csv` / `season.csv` with generated defaults (`DiscoveredSpeciesStore`, `DynamicSpeciesAutoCurves`).

Optional companion: **`ecosystemfloracompat`** submodule (`community/`) — JSON patches for Biodiversity shrubs/herbs; not required for Wildcraft or Floral Zones.

### Meadow, trees, vines, snow

- **Wild tree spread** — mature `log-grown` places **log-grown seedlings** (not saplings) with yearly maturation.
- **Tree spread guard** — meadow spread skips `log-grown` trunks, player saplings, and fruit-tree stems; column cache clears on nearby player placement.
- **Wild vines** — hanging growth, corner wrap, wall latch on spread.
- **Seasonal snow** — ecology meadow plants swap `-free` / `-snow` from climate and snow layer; brown sedge dormant/dieback via tallgrass phenology; underwater plant snow cover; sync limited to ecology blocks with sky exposure.
- **Fern phase saves** — legacy `fernphase-*-dieback` codes restored; chunk migration for mistaken `-free` variants.

### Wildfire canopy

- **No budding near active fire** — ecology stops placing `leaves-grown` / `leavesbranchy` while fire blocks are nearby (seasonal sync, tree growth, spread seedlings). Works with vanilla lightning and mods such as Pyrogenesis.
- **Orphan foliage prune** — during foliage chunk sync, wild leaves with no BFS path to `log-grown` of the same wood are stripped (`EnableOrphanFoliagePrune`). Skips player `leaves-placed`.
- **Fire-touched chunks** — chunks that recently had fire are prioritized for orphan-prune passes for **48** game hours (`OrphanFoliageFireChunkHours`). See [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md).

### Fixes and tuning

- **Berry mat** — third-party berry blocks participate in colony mat-edge spread when attrs are injected.
- **Calendar speed** — spread and maturation delays scale with the world calendar speed multiplier.
- **Crowfoot** — spread footing and meadow trunk protection fixes; brownsedge scythe harvest.
- **Flower phenology** — hand harvest on phase blocks; ghost pipe seasonal textures.

**ModDB short paste**

```
4.7.0 — third-party wild ecology (since 4.5.4)

• Wildcraft Fruit, Wildcraft Trees, Floral Zones (5 regions) — climate/spread injected at load; fruiting vines climate-only (no ecosystem spread). B+ species auto-append to user ecology/season CSV.

• Wild trees spread log-grown seedlings; meadow no longer overwrites trunks. Seasonal snow on meadow plants; wildfire canopy guard + orphan foliage prune. Wild vine hang/corner spread.

Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
```

---

## 4.5.4 — CSV hardening and config reference

- **Shipped CSV parity tests** — CI fails if `assets/ecosystemflora/species/ecology.csv` or `season.csv` drift from exporters (`tools/Export-SpeciesEcologyCsv.ps1`, `Export-SpeciesSeasonCsv.ps1`).
- **Load warnings** — duplicate species rows and unknown species in user ModConfig CSV (server log; unknown rows skipped).
- **`/ecospeciesreload`** — server admin command reloads merged ecology/season tables without world restart.
- Docs: client vs server inspect/handbook in [`SPECIES_ECOLOGY_CSV.md`](SPECIES_ECOLOGY_CSV.md).
- **`CONFIGURATION.md`** — complete reference for all **209** JSON keys (generated from `EcosystemConfig.cs` + config UI descriptions via `tools/generate_configuration_doc.py`).

---

## 4.5.3 — Wildgrass patch fixes

- **Wildgrass ecology patches** — `dependsOn` for `wildgrass` / `wildgrasscontinued`; `addmerge` instead of `add` on `/attributesByType`. Stops log spam when Wildgrass is not installed and avoids patch failures when the target file already has attributes.
- **Wildgrass handbook patches** — explicit per-species file paths instead of `plant/*` glob (nine blocktypes × two mod ids).
- Maintainer: `tools/generate_wildgrass_patches.py` regenerates both patch files.

---

## 4.5.2 — Fern spread and stability

- **Fern rhizome placement** — fix crash on invalid placement cells.
- **Fern phase textures** — restore correct seasonal visuals on phase blocks.
- **Spread null guards** — harden spread pipeline against null/air edge cases.

---

## 4.5.1 — Shore sedge harvest

- **Brownsedge knife harvest** — fix crash when harvesting with a knife.
- **Harvested root break** — correct visuals when breaking harvested sedge roots.

---

## 4.5.0 — Species CSV tuning

**Since 4.4.1**

### Per-species balance in CSV

- Contract species (flowers, ferns, berries, trees, aquatic, …) read **`assets/ecosystemflora/species/ecology.csv`** and **`season.csv`** at load — not hardcoded C# tables at runtime.
- Server admins can override without recompiling:
  - `ModConfig/ecosystemflora/species/ecology.csv` — climate, spread rate, spacing, maturation hours, mat connectivity, …
  - `ModConfig/ecosystemflora/species/season.csv` — monthly spread/stress curves
- **Server:** folder and files are **created automatically** on first start; missing species rows appended on each start. **Restart world** to reload edits.
- **Partial rows OK:** leave cells empty to keep shipped defaults. New species rows auto-append when the mod updates.
- Handbook and **Inspect (I)** show merged registry values.
- Global vigor knob: **`SpeciesSpreadRateScale`** in JSON (default ~⅓; presets set lush/sparse scales). Per-species exceptions in CSV.
- Maintainer export: `tools/Export-SpeciesEcologyCsv.ps1`, `tools/Export-SpeciesSeasonCsv.ps1`. Details: [`SPECIES_ECOLOGY_CSV.md`](SPECIES_ECOLOGY_CSV.md).

**ModDB short paste**

```
4.5.0 — species CSV tuning (since 4.4.1)

• Runtime balance from shipped ecology.csv + season.csv. Server auto-creates ModConfig/ecosystemflora/species/ with editable CSVs; missing rows appended on update. Partial overrides OK — restart world to reload.

• Inspect (I) and handbook use merged tables. Global spread: SpeciesSpreadRateScale (~⅓ default).

Vintage Story 1.22+. Do not run alongside Wild Farming Revival.
```

---

## 4.4.1 — Spread balance and meadow harvest

**Since 4.3.4**

- **`SpeciesSpreadRateScale`** (~⅓ default) — global multiplier on all species spread rates; presets `natural` / `lush` / `sparse` set scale with attempts and fitness.
- Shore sedge (**brownsedge**) mat spread retuned for the slower default pace.
- Meadow **scythe harvest** fix — flowers/tallgrass drop drygrass reliably when using scythe on spread maturation blocks.

---

## 4.3.0 — Berry colony ecology

**Since 4.2.0**

### Species-specific spread

- Wild berries now expand as **colonies**: one mat step from the patch edge (rhizome, root suckers, or stolons). Currants, strawberry, cloudberry, and others also use occasional **seed jumps** (reuses `RhizomeSeedDispersal*` settings).
- **Blueberry** and **cranberry** — forest/bog rhizome mats; no tree-host symbiosis.
- **Raspberry / blackberry** — aggressive edge thickets (blackberry uses eight-connected mat for tip rooting).
- **Strawberry** — forest-clearing runners + seed.
- **Beautyberry** — seed shrub; radius search, no mat.
- Per-species **context** (Forest / Edge / Open) and **moisture/light niche** profiles updated.
- Toggle: `EnableBerryColonySpread` (default on). **Inspect (I)** shows berry colony spread mode.

---

## 4.2.0 — Simulation visibility and ecology parity

**Since 4.1.5**

### Fern phenology

- Five fern species now follow **dormant → sporulating → dieback** phases (like meadow flowers), with new `fernphase-*` blocks for off-season and stressed visuals.
- **Orphan symbionts** (host tree gone) sync to dieback under stress instead of vanishing instantly.
- Spread is **gated** outside the sporulation season and while a fern is dormant or in dieback.
- **Inspect (I)** shows fern phase and sporulation status.
- Toggle: `EnableFernPhenology` (default on). Asset generator: `tools/GenerateFernPhaseBlocks.ps1`.

### Tallgrass phenology

- Tallgrass gets **winter dormant** and **stress dieback** phase blocks (`tallgrassphase-dormant-free`, `tallgrassphase-dieback-free`).
- Spread stops in dormant and dieback, matching the flower/fern pipeline.
- **Inspect (I)** shows tallgrass phase.
- Toggle: `EnableTallgrassPhenology` (default on).

### Berry spread maturation

- Wild berry bushes spawned by spread now **reset through cutting state** and enter the registry only when calendar-mature (same maturation idea as flowers/ferns).
- Density tuned for **blackberry, raspberry, and currants** so patches read more natural, not instant carpets.
- Toggle: `EnableBerrySpreadMaturation` (default on).

### Stump decay

- After tree senescence, standing **snag stumps** can schedule removal after a configurable number of **game years** (persisted in save data).
- Toggle: `EnableStumpDecay` (default on). Years: `StumpDecayYears` (default 10).

### Ecology history in inspect

- Recent ecology events for the aimed block — **orphan dieback**, **stress death**, **spread** — appear at the bottom of the **I** inspect report (up to three lines, ~14-day memory).
- No separate hotkey; history is part of inspect when `EnableEcologyHistoryHint` is on (default).

### Handbook

- Cross-links between ecology handbook pages (Overview, Inspect, Trees, Canopy, Configuration) use explicit `handbook://` URLs so links work in **en** and **ru**.
- Config key names in handbook text are plain bold labels, not broken pseudo-links.

### Presets and defaults

- New balance preset **`vanilla-minimal`** — disables juvenile spread maturation and phenology systems for a lighter “mostly vanilla” feel.
- Optional custom JSON presets in `ModConfig/ecosystemflora.presets/*.json`.
- **`ApplyCrossHabitatSpacing`** now defaults to **true** — meadow flowers/grass and shore/aquatic plants compete in the same spacing index where configured.

### Mycelium (aligned with 4.1.5)

- Felling a host tree **no longer instantly removes** linked mycelium. Tree removal is **notify-only**; anchors build stress and fade on the normal cadence, like ferns and berries.

---

**ModDB short paste**

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

---

## 4.1.5 — Symbiosis orphans fade via stress, not instant cascade

- **Fix (symbiosis):** removing a tree or other symbiosis host no longer mass-kills linked understory plants in one frame. Host removal only invalidates the host cache and wakes nearby ecology; orphaned symbionts (ferns, bluebells, berries, …) accumulate failed stress checks on the normal recheck cadence (~3–4 game days at defaults) and then die with the usual Death soil impulse. Aligns with impulse-only simulation — no ambient soil pass, no instant script cascade.

---

## 4.1.4 — Fast registration for flora placed near players

- **Fix (registration):** flora added to an already-scanned chunk without a normal place event (worldedit / fill / other mods) no longer takes minutes to enter the registry. A chunk's load-time scan marked it complete and nothing re-ran the fast scan afterwards, so discovery fell to the slow cyclic column crawler round-robining every active chunk (≈225 at default radius) at 32 columns/tick. The chunks immediately around each player are now re-enqueued into the fast background scan on a short cadence, so near-player edits register within a couple seconds. Hand-placed blocks were always instant (place event) and are unchanged.
- **Fix (rescan):** the cyclic flora rescanner no longer discards all per-chunk column progress whenever the active-chunk set changes (e.g. the player walks across a chunk border); it keeps cursors for chunks that stayed active, matching the tree rescanner.

---

## 4.1.3 — Fix runaway tallgrass spread

- **Fix (spread):** a single planted clump no longer carpets everything around it within a minute. Spreaders without a maturation cooldown (tallgrass especially, with its zero spacing and high spread rate) were re-triggered every reproduce tick by the event-wake path, which bypasses the calendar interval and was gated only by a spawn cooldown they never set. They now floor that cooldown to their own calendar interval, so event-wake can never spread faster than the scheduled cadence. Flowers and ferns are unchanged (their policies already set the cooldown).

---

## 4.0.2 — Soil succession + flower parity

- **Fix (soil):** meadow and understory spread no longer creates `forestfloor-*` from `soil-*`. Litter layer stays on existing forest floor (worldgen / canopy); plants only shift soil fertility and moisture tiers.
- **Hart's-tongue:** wetland herb role (not forest understory); no tree-symbiosis gate — open wet meadows only, no podzol conversion. Wetland spread raises soil moisture only (no peat creation).
- **Catmint / redtopgrass:** same spread-maturation and phenology pipeline as other meadow flowers; texture paths aligned with vanilla (`petal/catmint`, `redtopgrass1/2/3`).
- **Ferns:** rhizome mat spread (patch edge step), seasonal sporulation gate, juvenile spread maturation + post-attempt cooldown; inspect shows frontier / sporulation / maturing seedling. Species differentiated by temperature/rain envelopes, niche, and season curves (eagle/cinnamon/deer/tall/hartstongue).
- **Tests:** `WildSoilBlockMapperTests`, `FlowerSpreadAssetParityTests`, `FernSpreadTests` guard regressions.

---

## 3.9.24 — Meadow flower phenology (simulation-first)

- **Phases:** dormant → vegetative → bloom → dieback driven by `WildSpeciesSeason`, local temperature, and per-plant bloom energy. Spread and hand harvest only in **bloom**; inspect (I) shows phase, energy, and bloom ETA.
- **Visuals:** 72 full-size `flowerphase-*` blocktypes (24 meadow species × 3 non-bloom phases) plus bloom = vanilla `flower-*`. Spread seedlings stay on small `juvenile-flower-*`.
- **Assets:** `tools/GenerateFlowerPhaseBlocks.ps1` — petal textures with phase tint (fixes transparent dormant/dieback on heather and similar); dedicated rafflesia `inside`/`petals` overrides; block lang `block-flowerphase-*` / `block-juvenile-flower-*` in en/ru/de.
- **Config:** `EnableFlowerPhenology` (default on) and bloom temperature/energy keys in settings UI. Docs: [`FLOWER_PHENOLOGY.md`](FLOWER_PHENOLOGY.md).

---

## 3.9.23 — Juvenile flower texture fix

- **Fix:** spread seedlings for catmint, wild daisy, forget-me-not, edelweiss, heather, and western gorse now use vanilla texture paths (catmint has a single `petal/catmint` file; two-variant species no longer reference missing `*3` assets).
- **Tooling:** `GenerateJuvenileFlowerBlocks.ps1` — `WildcardSingle24` and `TwoVariant16/24` helpers mirror vanilla `texturesByType` groups.

---

- **Fix:** flower spread cooldown now applies when two-phase placement enqueues candidates but every commit fails revalidation.
- **Inspect (I):** last spread channel on registered plants (rhizome mat / seed jump / radius / failure reason).
- **Docs:** `GAPS.md` reframed — shallow-water colonization is natural; aquatic gaps are model/tempo consistency, not “slow down crowfoot”.

---

## 3.9.20 — Flower spread maturation (all meadow flowers)

- **Juvenile spread** for all 23 meadow flower species plus redtopgrass; maturation queue and inspect on establishing seedlings.
- **Spread-attempt cooldown** on placement commit (not background queue); separate `EnableFlowerSpreadAttemptCooldown` and `FlowerSpreadCooldownHoursMultiplier`.
- **Failed chance roll** applies a short parent pause (~3 h) to reduce event-wake spam.
- **Assets:** croton/rafflesia juveniles use vanilla shapes; variant-aware maturation for lupine, croton, and rafflesia.
- **Support:** one-time log when a juvenile blocktype is missing; `EcologyGrassColonizerSpecies` for redtopgrass; integration tests and doc updates.

---

## 3.9.16 — Branchy-first seasonal canopy (Option B)

**Canopy:** Spring leaf dress only from existing branchy skeleton (not from bare log); new branches via tree aging and rare winter skeleton repair. Autumn strip is patchy with a soft periphery-first bias; spring dress favors the inner crown first.

---

## 3.9.15 — Seasonal foliage regrowth fix

**Fix:** Breaking deciduous leaves no longer triggers immediate chunk foliage resync and catch-up budding (aggressive regrowth at tree bases, especially birch/maple). Player-cleared cells suppress seasonal buds for ~10 game days as a safety net for hybrid/random modes and tree aging.

---

## 3.9.14

**Performance:** Background spread scoring for meadow plants, reed/lily mats, and water crowfoot (default **on**). Per-chunk spread scheduling and chunk-based event wake.

**Fixes:** Catmint spreads in open meadows and keeps meadow soil (no forest floor under the plant). Tallgrass spreads only after half its local target height; growth continues to full height.

**Optional:** `EnableReproduceTickProfiling` for spread/worker debug logs.

Short ModDB copy: [`RELEASE_3.9.14.md`](RELEASE_3.9.14.md).

---

## 3.9.13 — Mat spread workers + per-chunk due heap (Phase 6.11 + 6.2)

### Mat / rhizome / lily pad spread on workers (6.11)

- **`EnableBackgroundSpreadSolve`** now covers **rhizome mat** (reeds) and **surface mat** (lilies) when two-phase spread is on — same snapshot → worker → `PendingSpreadQueue` pipeline as terrestrial meadow spread.
- Main thread still runs frontier checks, water placement, vacancy, and spacing; worker scores mat candidates only.
- Not covered at release time: water crowfoot (added in **3.9.14**), vines, mycelium.

### Per-chunk due scheduling (6.2)

- **`ProcessDue`** (when chunk-fair spread is off) now uses the **chunk round-robin executor** with **per-chunk due min-heaps** instead of a global due list scan.
- Wake vs calendar classification unchanged.

See [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.11–§6.2.

---

## 3.9.12 — Reproduce tick profiling (Phase 6.10)

- **`EnableReproduceTickProfiling`** — second log line: chunk-fair spread stats, wake vs calendar attempts, background spread worker queue, pending spread commits, column cache hit rate (interval delta).
- See [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.10.

---

## 3.9.11 — Empty-first spread on worker path (Phase 6.9)

- **Background spread solve** now mirrors sync **empty-first** collect when `EnableEmptyFirstSpreadCollect` is on: worker runs `EmptyOnly` scoring first, then `DisplacementOnly` only if no empty winner qualifies.
- Removes the v3.9.10 limitation that forced sync spread for default meadow displacement settings.
- See [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.9.

---

## 3.9.10 — Background spread solve + cyclic flora discovery

### Background spread scoring (opt-in)

- **`EnableBackgroundSpreadSolve`** (default **off**) — main thread captures compact **`SpreadSolveCell`** snapshots (surface, soil, climate, niche, spacing); **worker threads** score fitness and pick spread targets; **`SetBlock`** still runs on the main thread via the existing **`PendingSpreadQueue`** commit pass.
- Requires **`EnableTwoPhaseSpreadPlacement`** (default on). **Terrestrial** meadow spread only — not rhizome/reed mat, surface-mat lilies, vines, or mycelium network.
- **Empty-first** (`EnableEmptyFirstSpreadCollect`, default on) — supported on worker path since **3.9.11** (Phase 6.9).
- **`SpreadWorkerCount`** — background scorer threads (0 = half CPU cores, max 8). See [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) §6.8.

### Cyclic flora discovery

- **`EnableCyclicFloraDiscovery`** (default **on**) — round-robin **live** column rescan for flowers and tallgrass after chunk load (mirrors cyclic tree trunk discovery). Fixes flora missed by one-shot background registration when heightmap or load order hid plants.
- **`MaxFloraRescanColumnsPerTick`** (default **32**) — columns rescanned per chunk-scan tick.
- Worker registration hits can be **supplemented** with a live rescan when the snapshot pass returns zero flora (`FloraColumnDiscovery`).

### Tests

- **`SpreadSolverTests`** — worker-safe scoring unit tests.

### Кратко (RU)

- **Фоновый spread solve** (opt-in) — main собирает `SpreadSolveCell`, worker считает fitness и выбирает клетку; `SetBlock` на main через `PendingSpreadQueue`. Только terrestrial, нужен two-phase spread.
- **Cyclic flora** — live rescan цветов/травы после load (как cyclic trees); дополняет one-shot background registration.

---

## 3.9.9 — Tallgrass stage advance

- **Fix:** veryshort spread turf did not grow in vanilla — mod now advances **veryshort → short** on a calendar timer (`GrowthHoursMultiplier`), then registers for spread.
- See [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md).

---

## 3.9.8 — Server crash fix + config UI copy

- **Fix:** register `ecosystemHandbook` with `RegisterCollectibleBehaviorClass` (required on VS 1.22+ dedicated servers).
- **Config UI (RU):** shorter maturation field labels and tooltips without anglicisms.

---

## 3.9.7 — Tallgrass spread maturation

- **Veryshort spread** — meadow tallgrass spread places **veryshort** turf (cover/snow/free preserved); vanilla grass growth raises height before the patch spreads again from that cell.
- **Spread gate** — ecology registration and parent spread only from **short** and taller; `veryshort` and eaten grass do not reproduce.
- **`EnableTallgrassSpreadMaturation`** — turn off to restore legacy spread height selection at commit time.
- Design doc: [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md).

---

## 3.9.6 — Flower spread maturation

- **Juvenile spread** — colonizer meadow flowers (cow parsley, horsetail, mugwort, lupine, woad, red top grass, heather, western gorse) spread as a **small establishing plant**, then mature into the vanilla parent after a calendar delay.
- **`GrowthHoursMultiplier`** — now controls juvenile → mature pace (higher = faster).
- **Post-spawn cooldown** — parents wait after a successful offspring before spreading again; event wake no longer bypasses this floor.
- **`EnableFlowerSpreadMaturation`** — turn off to restore instant mature spread for all flowers.
- Design doc: [`FLOWER_SPREAD_MATURATION.md`](FLOWER_SPREAD_MATURATION.md).

---

## 3.9.4 — patch fixes (VS 1.22)

- **Handbook JSON patches** — `addmerge` on `/behaviors` (and `behaviorsByType` for tallgrass) instead of `/behaviors/-`; fixes errors on blocktypes without a root `behaviors` array.
- **Vanilla asset paths** — reeds → `reedpapyrus.json`; water crowfoot → `aquatic/watercrowfoot.json`; berries/saplings → `fruitingbush.json` / `plaintreesapling.json`.
- **Server-side patching** — `"side": "server"` on blocktype/item patches (no client “file not found” spam).
- **Wildgrass handbook** — same `addmerge` fix; `dependsOn` for `wildgrass` / `wildgrasscontinued`.

---

## 3.9.3 — Wildgrass Fork (optional)

- JSON patches register nine **Wildgrass** / **Wildgrass Fork** species (`wildgrass:*`) as third-party ecology participants when that mod is installed (`EnableThirdPartyParticipants`, default on).
- Mature growth stages spread; climate tuned from Wildgrass worldgen; harvest left to Wildgrass (`ecologyMeadowHarvest: none`).

---

## 3.8.0 — short (EN)

**Since 3.7.0**

- **Phase 6 simulation** — ecology runs in all loaded chunks: chunk-fair spread, wake on world changes, column cache, two-phase placement, monthly wake for seasonal species.
- **Faster registration** — player-vicinity priority (16-block radius), burst on chunk load, background column scan on a worker thread, paced registry apply (no more “lost tail” flora).
- **Vines & mushrooms** — wild vine tips and vanilla mycelium anchors register on chunk load and run in the same reproduce loop (chunk-fair spread, stress, inspect **I**) as meadow flora.
- **Seasonal trees** — deciduous canopy still strips/buds on the main thread; autumn branchy strip can drop `loosestick-free` on the ground.
- **Spread perf** — empty cells first; displacement when no vacancy; skip occupied columns on empty-first pass.
- **Tick desync** — reproduce 2 s, chunk scan 2.3 s, stress 5.5 s (fewer aligned server spikes).
- **Canopy sticks** — autumn branchy strip drops `loosestick-free` on the surface below (not floating on tallgrass).
- **Break wake** — breaking blocks with no ecology participant and no forest-context semantics (e.g. `loosestick-free`) no longer wakes neighbors; breaking leaves or tree logs still can.
- **Flora (3.7.1)** — red top grass colonizer, brown sedge, croton, rafflesias, cacti, frosted tallgrass profiles.
- Handbook updated (en/ru). Press **I** for ecology inspect.

---

## Since 3.7.0 — at a glance

| Area | What you get |
|------|----------------|
| **Simulation engine** | Chunk-fair spread across loaded chunks; event wake on break/place/displacement; column cache; two-phase evaluate/commit; monthly wake for seasonal species |
| **Registration** | Priority + burst near players; background column scan; paced registry apply; **vines** (column pass) + **mycelium anchors** (chunk BE scan) on load; seasonal foliage sync on main thread |
| **Spread perf** | Empty cells scanned first with full fitness; displacement still runs when no vacancy; column occupancy hint skips known plant columns |
| **Perf & fixes** | Desynced tick intervals; fallen sticks on ground surface; reduced wake on breaks without ecology/forest context (e.g. loose sticks) |
| **Handbook** | Configuration guide updated (en/ru) for v3.8 keys |
| **Tests** | 332 unit tests |

---

## 3.8.0 — Simulation engine (Phase 6)

Full ecology in **all loaded chunks** without geographic cutoffs. Smarter scheduling instead of throttling scope.

### Spread scheduling

- **Chunk-fair spread** — round-robin across ecology registry chunks (`EnableChunkFairSpread`, default on).
- **Event wake** — neighbors retry spread after breaks, placement, displacement, soil succession (`EnableEventDrivenSpread`).
- **Column cache** — spread preflight reads `SpreadColumnSnapshot` (`EnableEcologyColumnCache`).
- **Two-phase placement** — evaluate candidates without `SetBlock`, then chunk-fair commit with revalidation (`EnableTwoPhaseSpreadPlacement`). Applies to terrestrial/aquatic mat spread via `TrySpawnOffspring`; **mycelium network** and **wild vines** commit directly in the reproduce callback.
- **Season coarse wake** — seasonal species wake each in-game month (`EnableSeasonCoarseWake`).

Break turf or fell a tree — the meadow reacts within a couple of spread ticks.

### Registration (deferred chunk scan)

When you explore, flora registers incrementally. New in 3.8:

- **Priority queue** — chunks within `PlayerRegistrationPriorityRadiusBlocks` (default 16) drain before the background queue (`EnablePlayerPriorityRegistration`).
- **Burst on load** — one nearby chunk can finish registration in a single callback (`EnableBurstRegistrationNearPlayers`, ~80 ms scan budget).
- **Paced registry apply** — column scan collects hits into a pending queue; up to 512–2048 `RegisterReproducer` calls per tick (priority first). Fixes “lost tail” flora when scan budget ran out mid-chunk.
- **Background column scan** — main thread copies block ids into a chunk snapshot; flower / vine / tree classification runs on a dedicated worker thread (`EnableBackgroundRegistrationScan`).
- **Mycelium on chunk load** — vanilla `BlockEntityMycelium` anchors register via `MyceliumChunkRegistrar` on the same chunk-load path as vines (BE scan on main; then network spread + stress like vines in the reproduce tick).
- **Seasonal foliage (chunk mode)** — when background scan is on, canopy strip/bud runs in a separate main-thread pass (`FoliageCellScheduler.ProcessChunkSyncBatch` / `FoliageChunkSyncPass`), not on the worker.

Distant loaded chunks still register in the background — full scope, faster where you stand.

### Perf & correctness

- **Desynced ticks** — `ReproduceTickIntervalMs` 2000, `ChunkScanTickIntervalMs` 2300, `StressTickIntervalMs` 5500 (intervals not multiples of each other → less aligned CPU spikes).
- **Priority radius** — default **16** blocks (was 384); **burst** scan budget **80** ms per nearby chunk load.
- **Fallen sticks** — autumn branchy leaf strip can drop `loosestick-free`; placement uses surface search below the crown (not on tallgrass mid-air).
- **Break wake** — no ecology wake when the broken block is not an ecology plant, not in the registry, not a forest-context block (`log-grown`, `leaves-*`, ferntree trunk, …), and not an event-driven wake target (e.g. picking up `loosestick-free`).

### Spread collect (terrestrial)

- **Empty-first** — scan empty/vacancy cells with full fitness first (`EnableEmptyFirstSpreadCollect`). **Displacement is unchanged** when no empty cell qualifies.
- **Occupancy hint** — spacing index tracks occupied XZ columns per chunk; empty-first pass skips them before expensive placement (`EnableSpreadColumnOccupancyHint`).

Not applied to turf colonizers, mat spread (reeds/lilies), or habitats without displacement.

### Config (new / raised defaults)

| Key | Default | Purpose |
|-----|:-------:|---------|
| `EnableChunkFairSpread` | true | RR spread per loaded chunk |
| `EnableEventDrivenSpread` | true | Wake on world changes |
| `EnableEcologyColumnCache` | true | Spread column snapshot cache |
| `EnableTwoPhaseSpreadPlacement` | true | Evaluate then commit queue |
| `EnableSeasonCoarseWake` | true | Monthly wake for seasonal species |
| `EnablePlayerPriorityRegistration` | true | Player-vicinity registration first |
| `EnableBurstRegistrationNearPlayers` | true | Finish one nearby chunk on load |
| `PlayerRegistrationPriorityRadiusBlocks` | 16 | Priority/burst radius |
| `BurstRegistrationBudgetMs` | 80 | Burst scan time budget per load (ms) |
| `MaxRegistryAppliesPerTick` | 512 | Paced registry applies per chunk-scan tick |
| `MaxPriorityRegistryAppliesPerTick` | 2048 | Extra applies for player-vicinity chunks |
| `MaxPriorityChunkScansPerTick` | 48 | Extra priority queue scan passes per chunk-scan tick |
| `MaxPriorityRegistrationsPerTick` | 8192 | Legacy sync registration cap for priority queue |
| `PriorityRegistrationBudgetMs` | 80 | Per-pass ms budget for priority registration scans |
| `MaxBurstRegistrationsPerChunk` | 4096 | Max applies while finishing one burst chunk on load |
| `RegistrationBudgetMs` | 25 | Chunk-scan tick time budget (0 = `TickBudgetMs`) |
| `EnableBackgroundRegistrationScan` | true | Worker-thread column classification |
| `RegistrationWorkerCount` | 0 | Registration classify workers (0 = half cores, max 8) |
| `EnableBackgroundSpreadSolve` | false | Worker-thread spread fitness scoring (requires two-phase spread) |
| `SpreadWorkerCount` | 0 | Spread scoring workers (0 = half cores, max 8) |
| `EnableCyclicFloraDiscovery` | true | Live round-robin flora rescan after chunk load |
| `MaxFloraRescanColumnsPerTick` | 32 | Columns rescanned per chunk-scan tick (cyclic flora) |
| `MaxRegistrationSnapshotCellsPerTick` | 8192 | Block ids copied on main per tick |
| `MaxChunkColumnsScannedPerTick` | 16 | Background registration throughput |
| `MaxRegistrationsPerTick` | 2048 | Background registration cap |
| `EnableEmptyFirstSpreadCollect` | true | Empty cells before displacement |
| `EnableSpreadColumnOccupancyHint` | true | Skip occupied columns on empty-first pass |
| `ReproduceTickIntervalMs` | 2000 | Spread / reproduce tick interval (ms) |
| `ChunkScanTickIntervalMs` | 2300 | Registration scan tick (desynced from reproduce) |
| `StressTickIntervalMs` | 5500 | Stress tick interval (ms) |

Legacy safety (unchanged): `OnlyActivateNearPlayers`, `LimitSpreadNearPlayers` (spread + stress + tree aging near players; registration scans unchanged), `TickBudgetMs`, `SpreadBudgetMs`.

Full key list: `assets/ecosystemflora/ecosystemflora.example.json`.

See [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md) and handbook *Configuration Guide*.

---

## Кратко — с 3.7.0 до 3.8.0 (RU)

**Базовый релиз:** 3.7.0 (папоротник-дерево, крона, лианы). **Этот релиз:** 3.8.0.

### Контент (3.7.1)

- **Red top grass** — колонизатор луга, конкурирует с tallgrass (не «цветок луга»).
- **Brown sedge**, croton, rafflesias, barrel/silver-torch cactus, frosted tallgrass — полные ecology-профили.
- Turf colonizers без бонуса на пустые клетки — захват существующей травы.

### Симуляция (Phase 6)

- Spread **по чанкам** + **пробуждение** от изменений мира; двухфазный commit; coarse wake сезонных видов.
- **Быстрая регистрация** рядом с игроком (priority + burst + фоновый скан колонок; paced apply).
- **Лианы и грибница** — регистрация при load чанка; spread/stress/inspect (I) в том же reproduce loop, что и луг.
- **Сезонная крона** — отдельный foliage-pass на main при фоновой регистрации.
- **Empty-first spread** — пустые клетки первыми; **displacement** если vacancy нет.
- **Perf** — desynced ticks (2 s / 2.3 s / 5.5 s); палки на поверхности земли; меньше wake при ломании блоков без ecology/forest-context (напр. `loosestick-free`; листва и брёвна — по-прежнему могут будить).

Handbook (en/ru). VS 1.22+. Не совместим с Wild Farming Revival.

---

## ModDB paste — 3.8.0 update text

```
Since 3.7.0 → 3.8.0

FLORA (3.7.1)
• Red top grass — meadow colonizer, competes with tallgrass.
• Brown sedge, croton, rafflesias, cacti, frosted tallgrass — full ecology profiles.
• Turf colonizers skip empty-cell bonus — invade existing grass, not garden fill.

SIMULATION (Phase 6)
• Chunk-fair spread + event wake on break/place/displacement.
• Two-phase placement, column cache, monthly wake for seasonal species.
• Fast registration near you (priority queue + burst + background column scan).
• Vines and mycelium anchors register on chunk load; same reproduce loop as meadow flora.
• Seasonal canopy sync on main thread when background scan is enabled.
• Empty-first spread; displacement when no empty cell. Column occupancy hint.
• Desynced tick intervals (2 s / 2.3 s / 5.5 s). Fallen sticks land on ground surface. Less ecology wake when breaking blocks without ecology or forest context (e.g. loose sticks).

Handbook updated (en/ru). Press I for ecology inspect.
VS 1.22+. Do not run alongside Wild Farming Revival.
```

---

## Since 3.6.0 — at a glance

| Area | What you get |
|------|----------------|
| **Tree fern** | Vanilla `ferntree-normal-*` registers, spreads young columns, ages yearly, phased senescence — [`FERNTREE.md`](FERNTREE.md) |
| **Canopy** | Partial autumn branchy strip; fallen **sticks** under crown; spring **branchy buds** scale with tree calendar age — [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md) |
| **Wild vines** | `wildvine-end-*` tips extend downward and capture adjacent wall faces — [`WILD_VINE.md`](WILD_VINE.md) |
| **Trees (3.6 polish)** | Phased senescence implementation hardened; final year leaves **stump + fallen logs** (`TreeDecayRemains`) |
| **Handbook** | Species group pages refreshed (flowers, ferns, berries, aquatic) en/ru |
| **Tests** | 279 unit tests |

---

## 3.7.0 — Tree fern, canopy sticks, wild vines

### Tree fern (`ferntree-normal-*`)

Tropical arborescent fern columns — **not** lumber trees (`log-grown`) and **not** ground ferns (`fern-*`).

- Chunk scan registers trunk base at calendar age **0** (`EnableFerntreeEcology`).
- Yearly aging: crown top young → medium → old; slow height growth every few years.
- Spread places a **young column** (trunk + top-young + side foliage) near mature trunks.
- Phased senescence after ~80 years: foliage → crown removal → snag (`FerntreeSenescenceSnagSegments`) → column cleared.
- Counts as **tree host** for symbiotic ferns and forest context.
- Inspect (**I**) on any ferntree block → trunk base age, segments, crown maturity, senescence phase.

See [`FERNTREE.md`](FERNTREE.md).

### Canopy phenology enhancements

On top of **3.2** seasonal strip/bud:

- **Partial branchy autumn strip** — `FoliagePeakAutumnBranchyStripActivity` default **0.35** (was 0 = keep all branchy).
- **Fallen sticks** — stripping `leavesbranchy` may drop `loosestick-free` on the ground below (`EnableCanopyFallenSticks`, `CanopyFallenStickChance`).
- **Age-scaled spring branches** — older registered trees bud more `leavesbranchy` in spring from calendar age at trunk base (`EnableSpringBranchyAgeBoost`, `SpringBranchyAgeBoostYearsToMax`, `SpringBranchyAgeBoostMax`).

### Wild vines

Vanilla `wildvine-end-*` and `wildvine-tropical-end-*` tips join the reproduce loop (`EnableWildVineEcology`):

1. **Extend down** — air below tip → new end; former tip → section.
2. **Wall capture** — scan adjacent vertical faces of buildings and trunks (`WildVineWallCaptureRadius`, `WildVineWallCaptureHeight`).

See [`WILD_VINE.md`](WILD_VINE.md).

### Config (new keys)

| Key | Default | Purpose |
|-----|:-------:|---------|
| `EnableFerntreeEcology` | true | Tree fern register, spread, aging |
| `FerntreeSenescenceSnagSegments` | 2 | Snag trunk height (ferntree) |
| `FoliagePeakAutumnBranchyStripActivity` | 0.35 | Partial branchy strip threshold |
| `EnableCanopyFallenSticks` | true | Drop sticks when branchy strips |
| `CanopyFallenStickChance` | 0.42 | Stick drop chance scale |
| `EnableSpringBranchyAgeBoost` | true | Spring branchy buds × tree age |
| `SpringBranchyAgeBoostYearsToMax` | 60 | Years to max branch boost |
| `SpringBranchyAgeBoostMax` | 1.5 | Max spring branchy multiplier |
| `EnableWildVineEcology` | true | Vine tip spread |
| `WildVineWallCaptureRadius` | 4 | Horizontal wall scan |
| `WildVineWallCaptureHeight` | 6 | Vertical wall scan |

---

## Since 3.1.12 — at a glance (3.6 baseline)

| Area | What you get |
|------|----------------|
| **Trees** | Calendar age, slow yearly growth, phased senescence death (4 years), age saved in the world |
| **Canopy** | Deciduous autumn leaf drop and spring bud on existing log-grown trees; optional leaf particles under tall crowns |
| **Handbook** | Nine en/ru guide pages rewritten (overview, species groups, trees, canopy, inspect, config) |
| **Inspect (I)** | Trunk logs show age, structure size, senescence horizon (same key as flowers, reeds, mycelium) |
| **Config** | New toggles for tree aging, senescence, seasonal foliage, canopy ambience; `OnlyActivateNearPlayers` now defaults to **false** |

Press **I** on wild plants, mushroom caps, mycelium soil, or trunk logs. Enable **`VerboseLogging`** + **`ReproduceDebug`** in `ecosystemflora.json` for server log detail.

---

## 3.6.0 — Wild tree maturation

Registered wild trees (`log-grown` trunk base in the ecology registry) now have a **life cycle** beyond sapling spread. See [`TREE_AGING.md`](TREE_AGING.md) for the full end-to-end table.

### Full lifecycle

1. Mature trunk **spreads** a free sapling (winter off; not on ice/snow).
2. **Vanilla treegen** grows it; ecology **registers** the trunk base at calendar age **0**.
3. Each game year: **age +1**, optional structure growth, **sapling spread** (trunks never stress-die).
4. After species lifespan: **four senescence years** → stump + fallen debarked logs (or air if remains off).
5. **Neighbouring trees** refill the gap — no sapling burst on death.

### Calendar age and growth

- Each **game year**, registered trunks gain one calendar year and may add vanilla **log-grown**, **leavesbranchy**, or **leaves-grown** blocks.
- **Structure size** (trunk height, crown radius) and **calendar age** are separate: a worldgen giant can look tall at age 0 and will not die just because it looks mature.
- Growth respects map height, land claims, and physical vacancy — no custom block IDs.

### Senescence (phased death of old age)

- After lifespan, **four game years**: strip crown leaves → strip branchy skeleton → short dry trunk (snag, default 3 blocks) → **stump + fallen logs** (vanilla `log-*`, choppable; not re-registered as wild trees).
- Sapling spread and growth stop once senescence begins; spring canopy bud is blocked.
- Toggle: `EnableTreeSenescence` (default **on**). Snag height: `TreeSenescenceSnagBlocks`. Remains: `EnableTreeSenescenceRemains` (default **on**), `TreeSenescenceFallenLogCount` (default **3**, 0 = stump only). Set remains off for bare air removal. Blocked inside land claims.
- Master toggle: `EnableTreeAging` (default **on**). Turn off both to keep pre-3.6 tree behaviour (sapling spread only).

### Persistence

- Calendar age is **stored in the savegame** and restored when the chunk rescans the trunk after a server restart.
- Open **Inspect (I)** on any trunk log after reload to confirm age and structure.

### Inspect (I) on trunk logs

- Any `log-grown` block on the tree shows the same profile (resolved via trunk base).
- Lines include calendar age, structure vs typical mature for the species, and senescence horizon.
- Climate, soil, and niche are sampled at the **root base**, not mid-trunk.

### Config (new keys)

| Key | Default | Purpose |
|-----|:-------:|---------|
| `EnableTreeAging` | true | Yearly age + structure growth |
| `EnableTreeSenescence` | true | Phased death after lifespan |
| `TreeSenescenceSnagBlocks` | 3 | Trunk blocks during snag year |
| `EnableTreeSenescenceRemains` | true | Stump + fallen logs on final year |
| `TreeSenescenceFallenLogCount` | 3 | Ground logs near stump (0 = stump only) |
| `MaxTreeGrowthAttemptsPerTick` | 6 | Server tick budget for growth |
| `TreeGrowthActivityScale` | 1.0 | Pacing multiplier |

### Handbook (en / ru)

Nine in-game pages rewritten in plain language:

- Overview, Flowers, Ferns, Trees, Berries, Aquatic plants  
- Seasonal Canopy, Ecology Inspect, Configuration Guide  

Per-species numbers remain on vanilla block handbook pages and in the inspect dialog.

### Ecology scope default

- `OnlyActivateNearPlayers` defaults to **false** — ecology runs on all plants in **loaded chunks** (normal multiplayer and exploration).
- Set **true** only for local perf testing (~192 blocks from players). Old configs that still have `true` will behave as before until you edit the file; deleting the key lets the mod rewrite the default on next load.

---

## 3.5.0 — Canopy ambience

Client-side atmosphere under tall deciduous crowns — no server load, no save data.

- Subtle **green motes** under canopy in spring and summer; **falling leaf drift** in autumn (species-tinted colours).
- Respects view distance, particle settings, and optional rain suppression (`CanopyAmbienceSuppressInRain`).
- Toggle: `EnableCanopyAmbience` (default **on**).
- Autumn crown sync fix for mixed foliage states after seasonal strip/bud.

---

## 3.2.0 — Seasonal canopy phenology

Deciduous **log-grown** trees change crown foliage with the calendar — still vanilla blocks, no new IDs, no disk persistence.

### Autumn

- **`leaves-grown`** strips to air (partial defoliation — patchy crowns, not every leaf at once).
- **`leavesbranchy`** may thin at peak autumn depending on species curve.

### Spring

- **`log-grown`** and **`leavesbranchy`** bud into adjacent air → new branchy / leaf blocks.
- Only orthogonal neighbors; same wood type; land claims respected.

### How it works

- Per-cell rules synced on chunk load, month change, and nearby block updates.
- Deciduous species only; conifers unchanged.
- Toggle: `EnableSeasonalFoliage` (default **on**).

Works together with **3.5** ambience particles for a visible seasonal forest.

---

## Unchanged since 3.1.12 (reminder)

Still in the mod from earlier releases — no need to re-read if you already play 3.1.12:

- Mycelium ecology around vanilla mushroom anchors (niche, stress, network spread, inspect on caps and soil)
- Reed / tule / papyrus **mat edge** spread + rare seed jumps; water lily **pad mat**
- Meadow harvest (empty hand → block; knife/scythe → drygrass)
- Soil succession, symbiosis, displacement, seasonal spread for flowers/ferns/berries
- Third-party plants via JSON `ecologyParticipant`
- Config auto-merge — missing keys added to `ModConfig/ecosystemflora.json` on startup

---

## Кратко — с 3.6.0 до 3.7.0 (RU)

**Базовый релиз:** 3.6.0 (деревья, крона, справочник). **Этот релиз:** 3.7.0.

### Древовидный папоротник (`ferntree`)

- Регистрация колонны `ferntree-normal-trunk`, календарный возраст, рост кроны и высоты.
- Spread молодой колонны; phased senescence (~80 лет).
- Хост для симбиоза и лесного контекста. Осмотр **I**. [`FERNTREE.md`](FERNTREE.md).

### Крона (дополнение к 3.2)

- Частичное снятие `leavesbranchy` осенью (порог **0.35**).
- Палки `loosestick-free` под кроной при снятии ветвистой листвы.
- Весной больше почек `leavesbranchy` у **старых** деревьев (по `TreeAgeYears`).

### Дикие лианы

- Кончики `wildvine-end-*` растут **вниз** и захватывают соседние вертикальные грани. [`WILD_VINE.md`](WILD_VINE.md).

### Деревья (уточнение 3.6)

- Финальный год senescence: пень + брёвна (`TreeDecayRemains`) — реализация закреплена в коде.

---

## Кратко — с 3.1.12 до 3.6.0 (RU)

**Последняя публикация на ModDB:** 3.1.12. **Этот релиз:** 3.6.0.

### Деревья (3.6)

- Зарегистрированные дикие стволы получают **календарный возраст** раз в игровой год и могут медленно наращивать ствол и крону (ванильные блоки).
- **Размер и возраст разделены:** высокое дерево из генерации мира может быть «молодым» по календарю.
- В конце жизни вида — **четыре игровых года**: листва кроны → ветвистый остов → короткий сухой ствол (snag) → **пень и брёвна** (ванильные `log-*`, можно рубить; экология их не регистрирует). Spread саженцев останавливается; весенний bud кроны заблокирован. Ключи: `EnableTreeSenescenceRemains`, `TreeSenescenceFallenLogCount`.
- Возраст и **фаза senescence** сохраняются в мире и восстанавливаются после перезапуска сервера.
- **Осмотр (I)** на любом бревне ствола: возраст, размер, текущая фаза упадка.
- Ключи: `EnableTreeAging`, `EnableTreeSenescence`, `TreeSenescenceSnagBlocks`, `MaxTreeGrowthAttemptsPerTick`, `TreeGrowthActivityScale`.

### Сезонная крона (3.2)

- Лиственные породы: частичное опадание **`leaves-grown`** осенью, почки весной на **`log-grown`** и **`leavesbranchy`**.
- Без своих блоков; только правила на существующих `log-grown` / листьях.
- `EnableSeasonalFoliage` — по умолчанию включено.

### Атмосфера под кроной (3.5)

- Клиентские частицы: зелёная пыль весной/летом, опадающие листья осенью.
- Только клиент; `EnableCanopyAmbience`.

### Справочник (3.6)

- Девять страниц переписаны (en/ru): обзор, цветы, папоротники, деревья, ягоды, водные, сезонная крона, осмотр, настройки.
- Цифры по видам — в справочнике блока и в осмотре.

### Настройки

- `OnlyActivateNearPlayers` по умолчанию **false** — экология во **всех загруженных** чанках. **true** — только для локального теста производительности.

### Без изменений (если уже играли на 3.1.12)

- Грибница, mat-распространение тростника и кувшинки, сбор луга, сукцессия почвы, сторонние моды через `ecologyParticipant`, автодополнение конфига.

---

## ModDB paste — 3.7.0 update text

```
Since 3.6.0 → 3.7.0

TREE FERN
Vanilla ferntree-normal columns: register, yearly aging, spread young structures, phased senescence. Symbiosis tree host. EnableFerntreeEcology.

CANOPY (3.2+)
Partial autumn branchy strip (default 0.35). Fallen loose sticks under crown when branchy strips. Spring branchy buds scale with tree calendar age.

WILD VINES
wildvine-end tips extend downward and colonize adjacent wall faces. EnableWildVineEcology.

Press I on ferntree blocks, trunk logs, plants, mushrooms. VerboseLogging + ReproduceDebug for server detail.
```

---

## ModDB paste — full update text (3.1.12 → 3.6.0)

```
Since 3.1.12 → 3.6.0

WILD TREE AGING (3.6)
Registered trunks gain calendar years once per game year and may grow taller/wider (vanilla log-grown / leaves). At species lifespan: phased death over four game years (leaves, skeleton, snag, stump + fallen logs). Age persists in saves. EnableTreeAging / EnableTreeSenescence / EnableTreeSenescenceRemains / TreeSenescenceFallenLogCount.

SEASONAL CANOPY (3.2)
Deciduous trees partially drop leaves in autumn and bud again in spring on existing log-grown skeletons — no custom blocks. EnableSeasonalFoliage (default on).

CANOPY AMBIENCE (3.5)
Optional client leaf particles and flutter under tall deciduous crowns. EnableCanopyAmbience (default on).

HANDBOOK (3.6)
Nine en/ru guide pages rewritten: overview, species groups, trees, seasonal canopy, inspect, config.

CONFIG
OnlyActivateNearPlayers now defaults to false — ecology in all loaded chunks (normal play). Set true only for local perf testing.

Press I on plants, mushrooms, mycelium soil, or trunk logs. VerboseLogging + ReproduceDebug for server detail.
```
