# Changelog — Ecosystem - Flora

Player-facing release notes. Dev history: [`PROGRESS.md`](PROGRESS.md).

**Last public release:** **3.1.12** (ModDB)  
**This release:** **3.6.0**

Requirements: Vintage Story **1.22+**. Do not run alongside Wild Farming Revival.

---

## Since 3.1.12 — at a glance

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

## ModDB paste — full update text

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
