# Gaps — где идея мода ещё недоработана

> Актуально для **4.2.0+**. Валидация баланса — через **логи** (`VerboseLogging` + `ReproduceDebug`) и **осмотр (I)**, не обязательно визуальный обход мира.

См. также: [PROJECT_VISION.md](PROJECT_VISION.md), [PROGRESS.md](PROGRESS.md).

---

## 1. Симуляция vs «ощущение реализма»

| Область | Сейчас | Пробел |
|---------|--------|--------|
| **Луг / цветы** | Juvenile spread → maturation; post-spread cooldown; inspect на ростках | Полный жизненный цикл всё ещё abstract; росток inspect — «не в реестре» до созревания (намеренно) |
| **Reeds / lily** | Mat edge + virtual seed (A–D); inspect: mode, frontier, seed %, **last spread channel** | Нет предметов семян/ризомов; игрок не видит «канал» spread в мире, только в I |
| **Water crowfoot** | Independent column spread в 2–8 блоков воды над илом | **Не mat** — другой код, чем reeds/lily; **заполнение мелководья — норма**, не баг (см. §2) |
| **Деревья** | `log-grown` → sapling; senescence + stump/logs; **wildfire** — no bud near fire, orphan foliage prune in chunk sync ([`CANOPY_PHENOLOGY.md`](CANOPY_PHENOLOGY.md)) | living trunk stress; sapling burst on death |
| **Древовидный папоротник** | [`FERNTREE.md`](FERNTREE.md) | playtest spread/senescence в тропиках |
| **Дикие лианы** | [`WILD_VINE.md`](WILD_VINE.md) | playtest на зданиях/стволах; нет stress/climate gate на tip |
| **Грибница** | network spread + chunk load anchors | нет своих блоков; баланс mat vs vanilla regrowth — playtest |
| **Ягоды** | Spread + trait clone + **calendar maturation** (4.2) | density tuned blackberry/raspberry/currant |

**Вывод:** ядро — **конкуренция клеток и ниша**, не ботаническая модель. «Экосистема» для игрока = паттерны на карте + inspect, не полный life cycle.

---

## 2. Aquatic (после v3.1.3–3.1.6)

**Дизайн-принцип:** мелководье и подходящие ячейки **должны зарастать** — рогоз, тростник, папирус (mat), водяной лютик (подводная колонна). Это натуралистично. Gaps здесь — **согласованность механики и темп**, не «сделать реже, чтобы не заливало».

| Сделано | Остаётся |
|---------|----------|
| Rizome mat + seed для reeds | Баланс **только** через лог + I; нет автотестов frontier на реальном chunk |
| Surface mat + seed для lily | `de.json` handbook **не обновлён** (en/ru — да) |
| Inspect: mode / frontier / seed % / **last spread** (rhizome / seed / fail) | Нет строки в **логе** (только inspect) |
| Crowfoot заполняет подходящее мелководье | **Outlier в коде:** independent radius, не mat — будущая **единая aquatic-модель** без искусственного торможения colonization |
| | Papyrus / reed **land vs water variant** — third-party без auto-resolve |
| | Озёра без gravel bed — spread невозможен (OK); worldgen иногда даёт «мёртвые» берега |

**Приоритет доработки:** unified aquatic spread model (crowfoot в ту же семантику, что reeds, **без** «rate ↓ чтобы не заливало»); third-party reed variants; de handbook.

---

## 3. Конкуренция и ниша

| Тема | Статус |
|------|--------|
| Displacement луг ↔ луг | ✅ сильная сторона |
| Symbiosis дерево ↔ understory | ✅; **gradual stress death** (4.1.5+) — no instant cascade |
| Niche moisture/light | ✅ MVP; **лес у воды** — редко проверялся |
| Cross-habitat spacing | Default **on** (`ApplyCrossHabitatSpacing: true`, 4.2) — meadow and shore compete in spacing index |
| Aquatic ↔ terrestrial displacement | Нет — разные vacancy rules |
| Dominant species **без I** | Backlog (HUD / chunk hint) |

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

**В scope, но «тонко»:** деревья, tree fern, wild vines, ягоды, tallgrass matrix, third-party JSON.

**Сознательно вне scope:** Fauna, living trees, custom mushroom blocks, item-based propagation.

---

## 6. UX и документация

| OK | Недоработано |
|----|--------------|
| Handbook en/ru | **de.json** — старые тексты |
| Inspect I — last spread, flowers, ferntree, vines | ~50 ключей в example JSON — порог для новичка |

---

## 7. Архитектура (vision vs код)

- Один **`PlantRequirements`** + habitat switch.
- **Spread modes** hardcoded — не pluggable registry.
- Новый habitat = ветка в `ReproducePlacement` (норма для v3.x).

---

## 8. Производительность и масштаб

Phase 6 engine + background spread/registration (см. [`PHASE6_SIMULATION.md`](PHASE6_SIMULATION.md)).

**Не задокументировано числами:** 20k+ reproducers, несколько игроков на разных краях карты.

---

## 9. Приоритеты (если продолжать v3.x)

1. **Two-phase / spread pacing** — ✅ cooldown on failed commit (v3.9.20+); дальше — playtest tempo при lush + event wake.
2. **Aquatic model unification** — crowfoot в общую aquatic-семантику **без** ослабления colonization мелководья.
3. **de handbook** — паритет с en/ru.
4. **Spread debug log** — optional last channel в Notification (inspect уже есть).
5. **Dominant species hint** — лёгкий UX без карты.
6. **Third-party berry traits** — только при реальном контент-моде.
7. **Fauna companion** — отдельный modid.

---

## 10. Что уже закрыто (не считать gap)

- v3.1.2 soil succession; v3.1.3–5 aquatic mat spread A–D.
- v3.9.6–9 flower/tallgrass spread maturation; v3.9.20 flower cooldown on commit, failed chance pause, inspect last spread.
- Phase 6 chunk-fair spread, event wake, background solve, registration perf.
- Chunk scan; land claims; third-party JSON participants.
