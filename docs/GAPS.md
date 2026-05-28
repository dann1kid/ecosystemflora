# Gaps — где идея мода ещё недоработана

> Актуально для **3.1.6**. Валидация баланса — через **логи** (`VerboseLogging` + `ReproduceDebug`) и **осмотр (I)**, не обязательно визуальный обход мира.

См. также: [PROJECT_VISION.md](PROJECT_VISION.md), [PROGRESS.md](PROGRESS.md).

---

## 1. Симуляция vs «ощущение реализма»

| Область | Сейчас | Пробел |
|---------|--------|--------|
| **Луг / цветы** | Independent spread + displacement | Нет стадий жизни (seedling → mature); spread = телепорт блока, не рост |
| **Reeds / lily** | Mat edge + virtual seed (A–D) | Нет предметов семян/ризомов; игрок не видит «канал» spread |
| **Water crowfoot** | Radius-4 independent | Не mat и не ризом — логика **старого** типа; может снова «заливать» мелководье при высоком preset |
| **Деревья** | `log-grown` → sapling | Нет living trunk; рост и смерть — **ванilla treegen**, не экосystem |
| **Ягоды** | Spread + trait clone | Нет стадий куста при spread; мутации trait — опционально и слабо заметны |

**Вывод:** ядро — **конкуренция клеток и ниша**, а не ботаническая модель. Это осознанный компромисс VS API, но «экосистема» для игрока = паттерны на карте, не жизненный цикл.

---

## 2. Aquatic (после v3.1.3–3.1.6)

| Сделано | Остаётся |
|---------|----------|
| Rizome mat + seed для reeds | Баланс **только** через лог + I; нет автотестов frontier на реальном chunk |
| Surface mat + seed для lily | `de.json` handbook **не обновлён** (en/ru — да) |
| Inspect: mode / frontier / seed % | Нет строки «последний spread: rhizome vs seed» в логе/inspect |
| Season curve смягчена | `watercrowfoot` rate 2.0 + radius 4 без mat — **outlier** |
| | Papyrus / reed **land vs water variant** — third-party без auto-resolve |
| | Озёра без gravel bed — spread физически невозможен (OK), но worldgen иногда даёт «мертвые» берега |

**Приоритет доработки:** crowfoot (отдельная модель или снижение rate); third-party reed variants; de handbook.

---

## 3. Конкуренция и ниша

| Тема | Статус |
|------|--------|
| Displacement луг ↔ луг | ✅ сильная сторона |
| Symbiosis дерево ↔ understory | ✅; каскад при рубке |
| Niche moisture/light | ✅ MVP; **лес у воды** — редко проверялся |
| Cross-habitat spacing | Default **off** — reed и flower не «видят» друг друга в spacing |
| Aquatic ↔ terrestrial displacement | Нет — разные vacancy rules |
| Dominant species **без I** | Backlog (HUD / chunk hint) |

---

## 4. Почва и пашня

| Сделано | Пробел |
|---------|--------|
| Soil succession v3.1.2 (meadow +, depleter) | Игроки могут не понимать, **почему** tier меняется — handbook есть, ModDB коротко |
| Fallow drip on spread | Слабо заметно в игре без inspect |
| Spread on empty farmland | ✅ |
| Trampling soil degradation | Off by default; связь с succession неочевидна |

---

## 5. Контент и scope

**В scope, но «тонко»:**

- **14 деревьев** — только sapling spread; нет экологии ствола (порода, возраст, тень от кроны как отдельная сущность).
- **10 ягод** — traits; third-party berry **не** поддержан.
- **Tallgrass** — matrix, без отдельной «съеденности» (выпас — backlog / Fauna mod).
- **Third-party** — JSON контракт ✅; нет каталога совместимых модов, нет версионирования контракта.

**Сознательно вне scope (но дыра в «полной экосистеме»):**

- Fauna (grazing, food search) — только дизайн.
- Living trees, vines, mushrooms, termites.
- Item-based propagation.

---

## 6. UX и документация

| OK | Недоработано |
|----|--------------|
| Handbook en/ru (aquatic mat, tuning) | **de.json** — старые тексты |
| Inspect I — mat frontier | ModDB changelog 3.1.x не собран одним блоком для игроков |
| `ecosystemflora.example.json` | ~40 ключей — порог для новичка |
| PROGRESS / VISION | Были отстающие от 3.1.6 (синхронизация — этот проход) |

**Рекомендация:** один блок «What changed in 3.1.x» на ModDB; в handbook overview — одна строка «press I to debug spread».

---

## 7. Архитектура (vision vs код)

Цель PROJECT_VISION — capabilities / interests. Фактически:

- Один **`PlantRequirements`** + habitat switch (работает, но монолитный профиль).
- **`IEcosystemParticipant`** — да; отдельные `IReproducible` на блоках — нет.
- **Spread modes** (`RhizomeMat`, `SurfaceMat`) — hardcoded в C#, не pluggable registry.

Для v3.x это нормально. Риск: каждый новый habitat (например mat для duckweed) = ещё ветка в `ReproducePlacement`.

---

## 8. Производительность и масштаб

Реализовано: spatial tick, budgets, player radius, chunk scan resume.

**Не задокументировано числами:** поведение при 20k+ reproducers, несколько игроков на разных краях карты. Ожидание: `OnlyActivateNearPlayers` спасает; edge case — «замороженный» мир далеко от игроков (OK by design).

---

## 9. Приоритеты (если продолжать v3.x)

1. **Crowfoot** — mat или rate ↓ (согласованность aquatic).
2. **ModDB + de handbook** — паритет с en/ru для 3.1.x.
3. **Inspect/log** — optional last channel (`rhizome` / `seed` / fail reason) для вашего workflow.
4. **Dominant species hint** — лёгкий UX без карты (tooltip / chat при входе в biome-scale zone).
5. **Third-party berry traits** — только если появится реальный контент-мод.
6. **Fauna companion** — отдельный modid, не раздувание Flora.

---

## 10. Что уже закрыто (не считать gap)

- v3.1.2 soil succession balance + Terrain Slabs guard.
- v3.1.3–5 aquatic mat spread A–D.
- v3.1.1 `BerryTraitMutationChance`.
- v3.1 third-party JSON participants.
- Chunk scan без BE patches; land claims; greenhouse survival.
