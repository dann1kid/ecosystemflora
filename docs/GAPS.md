# Gaps — где идея мода ещё недоработана

> Актуально для **4.11.25** (ModDB). Валидация баланса — через **логи** (`VerboseLogging` + `ReproduceDebug`) и **осмотр (I)**, не обязательно визуальный обход мира.

См. также: [PROJECT_VISION.md](PROJECT_VISION.md), [PROGRESS.md](PROGRESS.md).

---

## 1. Симуляция vs «ощущение реализма»

| Область | Сейчас | Пробел |
|---------|--------|--------|
| **Луг / цветы** | Juvenile spread → maturation; post-spread cooldown; seasonal snow; inspect на ростках | Полный жизненный цикл всё ещё abstract; росток inspect — «не в реестре» до созревания (намеренно) |
| **Reeds / lily** | Mat edge + virtual seed (A–D); inspect: mode, frontier, seed %, **last spread channel** | Нет предметов семян/ризомов; игрок не видит «канал» spread в мире, только в I |
| **Water crowfoot** | **RhizomeMat** edge ±1 (`CrowfootMatSpread`); колонка section→tip/top; глубина 2–6; seed **0**; guards (fish trap, BE) | Отдельный worker-путь `CollectCrowfootCells` **без** early `IsFrontier` (sync-путь проверяет); playtest tempo мелководья |
| **Деревья** | Wild spread → **log-grown seedling** (не sapling); yearly maturation; crown forms; **niche lifespan debt** (4.11.22–25); immature spread gate; senescence; **wildfire** — no bud near fire, orphan foliage prune ([`TREE_AGING.md`](TREE_AGING.md), [`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md)) | living trunk stress death; sapling burst on death |
| **Древовидный папоротник** | [`FERNTREE.md`](FERNTREE.md) | playtest spread/senescence в тропиках |
| **Дикие лианы** | Hang/corner/wall latch spread (4.7) — [`WILD_VINE.md`](WILD_VINE.md) | playtest на зданиях/стволах; нет stress/climate gate на tip |
| **Грибница** | network spread + chunk load anchors | нет своих блоков; баланс mat vs vanilla regrowth — playtest |
| **Ягоды** | Spread + trait clone (`game:` only) + **calendar maturation**; third-party mat edge (bdshrub и др.) | trait clone для **third-party** berry BE; density tuned blackberry/raspberry/currant — playtest |
| **Third-party wild** | Runtime bootstraps: Wildcraft Fruit/Trees, Floral Zones (**7/7**, 211 entries), fruitvine climate-only, B+ CSV auto-curves (4.7–4.9) | Floral Zones **trees** (`sapling`/`lognarrow`); playtest с модами; live merge `ecology.csv` для declared third-party |

**Вывод:** ядро — **конкуренция клеток и ниша**, не ботаническая модель. «Экосистема» для игрока = паттерны на карте + inspect, не полный life cycle.

---

## 2. Aquatic (после v3.1.3–3.1.6, crowfoot mat **4.7**)

**Дизайн-принцип:** мелководье и подходящие ячейки **должны зарастать** — рогоз, тростник, папирус (mat), водяной лютик (подводная колонна с mat-edge). Это натуралистично. Gaps здесь — **баланс, third-party, UX**, не «сделать реже, чтобы не заливало».

| Сделано | Остаётся |
|---------|----------|
| Rizome mat + seed для reeds | Баланс **только** через лог + I; нет автотестов frontier на **реальном** chunk (unit-тесты на mock accessor — есть) |
| Surface mat + seed для lily | `de.json` handbook **не обновлён** (~333 строк vs ~970 en/ru) |
| Inspect: mode / frontier / seed % / **last spread** (rhizome / seed / fail) | Нет строки в **логе** (только inspect) |
| **Crowfoot mat-edge** (`CrowfootMatSpread`, `MatSpreadDispatch`, `ecology.csv` `RhizomeMat`, spread 0.75) | Background solve: `CollectCrowfootCells` не дублирует early frontier gate (sync path — да) |
| Crowfoot guards — fish trap / solid / occupied section (`CrowfootSpreadGuard`) | Papyrus / reed **land vs water variant** — third-party без auto-resolve |
| Underwater plant **snow** — submerged cells skipped (`PlantSnowCover`, 4.7) | Озёра без gravel bed — spread невозможен (OK); worldgen иногда даёт «мёртвые» берега |
| Shore sedge mat + scythe/knife fixes (4.5–4.7) | Playtest: crowfoot tempo vs reeds/lily в одном озере |

**Приоритет доработки:** third-party reed variants; `de` handbook; crowfoot worker frontier parity; spread channel в optional log.

---

## 3. Конкуренция и ниша

| Тема | Статус |
|------|--------|
| Displacement луг ↔ луг | ✅ сильная сторона |
| Symbiosis дерево ↔ understory | ✅; **gradual stress death** (4.1.5+) — no instant cascade |
| Niche moisture/light | ✅ MVP; **лес у воды** — редко проверялся |
| Cross-habitat spacing | ✅ default **on** (`ApplyCrossHabitatSpacing: true`, 4.2) |
| Meadow ↔ tree trunk protection | ✅ spread guard + arboreal preflight (4.7) |
| Aquatic ↔ terrestrial displacement | ❌ нет — разные vacancy rules |
| Dominant species **без I** | ❌ backlog (HUD / chunk hint) |

---

## 4. Почва и пашня

| Сделано | Пробел |
|---------|--------|
| Soil succession v3.1.2 | Игроки могут не понимать, **почему** tier меняется |
| Fallow drip on spread | Слабо заметно без inspect |
| Spread on empty farmland | ✅ |
| Trampling soil degradation | Off by default |

---

## 5. Контент и scope

**В scope, но «тонко»:** деревья, tree fern, wild vines, ягоды, tallgrass matrix, third-party JSON + **runtime bootstraps** (Wildcraft, Floral Zones).

**Сознательно вне scope:** Fauna, living trees, custom mushroom blocks, item-based propagation.

---

## 6. UX и документация

| OK | Недоработано |
|----|--------------|
| Handbook en/ru; ecology history hint in inspect (`EnableEcologyHistoryHint`, 4.2) | **de.json** — устаревший guide + ~⅓ ключей en/ru |
| Inspect I — last spread, flowers, ferntree, vines, mycelium, crowfoot mat | ~50 ключей в example JSON — порог для новичка |
| `CONFIGURATION.md` — 209 keys (4.5.4); `/ecospeciesreload` | ModDB paste / handbook de parity |

---

## 7. Архитектура (vision vs код)

- Один **`PlantRequirements`** + habitat switch — ✅.
- **Spread modes** hardcoded — не pluggable registry (норма).
- Crowfoot / reeds / lily / berries — общая **`MatSpreadDispatch`**, но отдельные topology-классы.
- Новый habitat = ветка в `ReproducePlacement` (норма для v3.x–4.x).

---

## 8. Производительность и масштаб

Phase 6 engine + background spread/registration (см. [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md)). **Unloaded chunks** не симулируются — при unload registry сбрасывается; план — Phase 7 [`PHASE7_EXTERNAL_SIMULATION.md`](PHASE7_EXTERNAL_SIMULATION.md).

**Не задокументировано числами:** 20k+ reproducers, несколько игроков на разных краях карты.

---

## 9. Приоритеты (backlog)

| # | Тема | Статус |
|---|------|--------|
| 1 | Two-phase / spread pacing | ✅ cooldown on failed commit (v3.9.20+); дальше — playtest tempo при lush + event wake |
| 2 | **Aquatic model unification (crowfoot mat)** | ✅ **4.7** (`52ba7a3`) — mat-edge, guards, CSV tune; minor: worker frontier gate |
| 3 | **de handbook** | ❌ паритет с en/ru |
| 4 | Spread debug log | ❌ optional last channel в Notification (inspect уже есть) |
| 5 | Dominant species hint | ❌ лёгкий UX без карты |
| 6 | Third-party berry **traits** | ❌ только `game:fruitingbush-wild-*`; mat spread для third-party — ✅ |
| 7 | Third-party wild ecology playtest | ❌ Wildcraft + Floral Zones 7/7 в одном мире |
| 8 | Floral Zones trees bootstrap | ❌ `sapling` / `lognarrow` без worldgen ecology |
| 9 | Fauna companion | ❌ отдельный modid (вне scope) |
| 10 | **Unloaded chunk ecology (Phase 7)** | 📋 design — [`PHASE7_EXTERNAL_SIMULATION.md`](PHASE7_EXTERNAL_SIMULATION.md); target **v5.0** |

---

## 10. Что уже закрыто (не считать gap)

- v3.1.2 soil succession; v3.1.3–5 aquatic mat spread A–D (reeds seed, lily mat).
- v3.9.6–9 flower/tallgrass spread maturation; v3.9.20 flower cooldown on commit, failed chance pause, inspect last spread.
- Phase 6 chunk-fair spread, event wake, background solve, registration perf.
- Chunk scan; land claims; third-party JSON participants.
- **4.4.1** — `SpeciesSpreadRateScale`; **4.5.0** species CSV registry; **4.5.4** CSV parity, `/ecospeciesreload`, `CONFIGURATION.md`.
- **4.7.0** — crowfoot rhizome mat; seasonal snow (incl. underwater skip); wildfire canopy guard + orphan foliage prune; wild tree log-grown seedlings; meadow trunk spread guard; wild vine hang/corner; third-party bootstraps (Wildcraft, Floral Zones 5→7); fruitvine climate-only; B+ auto-curves; third-party berry mat edge; `ecosystemfloracompat` submodule.
- **4.9–4.11** — Floral Zones Cape + Cosmopolitan; per-world config + setup wizard; potato/X3D perf; crown forms; tree niche lifespan stress + lean niche sample.
