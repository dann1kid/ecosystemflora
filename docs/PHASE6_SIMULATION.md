# Phase 6 — Simulation engine (без компромиссов по scope)

> План на **v3.8+**. Цель: **полная экология во всех loaded chunks** на мощном железе — за счёт умного планировщика и инкрементального состояния, а не geographic cutoffs и урезанных preset’ов.

См. также: [PROJECT_VISION.md §12](PROJECT_VISION.md#12-производительность-roadmap), [PROGRESS.md](PROGRESS.md), [GAPS.md §8](GAPS.md#8-производительность-и-масштаб).

---

## 1. Принципы (что считаем «идеалом»)

| Да | Нет (компромисс, не цель) |
|----|---------------------------|
| Все reproducers в loaded chunks участвуют в симуляции | `OnlyActivateNearPlayers` / `LimitSpreadNearPlayers` как основной режим |
| Ровный ms/tick без spike в одном лугу | «Просто уменьшить `MaxReproduceAttemptsPerTick`» |
| Spread реагирует на изменение мира | Таймер на каждое растение с полным `O(r²)` scan |
| Календарная скорость (attempts/year) сохраняется в среднем | Искусственное замедление preset’ом ради CPU |
| `TickBudgetMs` — страховка от багов | Budget как единственный дизайн-лимит |

**Жёсткое ограничение VS:** один server thread, `BlockAccessor` только на main thread. Выигрыш — меньше и умнее работа, не `Parallel.For`.

---

## 2. Текущая модель (baseline)

```
OnReproduceTick (2s real)
  ├── tree / ferntree / foliage (round-robin по registry)
  └── ProcessDue
        ├── CollectDueEntries  → due-heap (global) или scan byChunk (spatial)
        ├── fair cursor → до MaxReproduceAttemptsPerTick
        └── TrySpawnOffspring → CollectSpreadCandidates → SetBlock
```

**Сильные стороны:** due-heap, `byChunk`, cheap-first preflight, spacing hash, split sample, column climate cache, chunk scan resume.

**Слабое место для «идеала»:** планирование **plant-centric + time-centric**. География и изменения мира не участвуют в решении «кого тикать сейчас». Пики — когда много due в одном регионе.

---

## 3. Целевая архитектура (4 слоя)

```
┌─────────────────────────────────────────────────────────┐
│  Layer 4 — Commit pipeline (SetBlock, displacement)     │
│  Chunk-fair queue, budget ms, max commits/chunk/tick      │
└──────────────────────────▲──────────────────────────────┘
                           │ winners
┌──────────────────────────┴──────────────────────────────┐
│  Layer 3 — Spread intent (cheap)                          │
│  CollectSpreadCandidates / fitness / spacing (no SetBlock)│
└──────────────────────────▲──────────────────────────────┘
                           │ woken entries
┌──────────────────────────┴──────────────────────────────┐
│  Layer 2 — Ecology wake scheduler                       │
│  Event-driven dirty + calendar fallback + chunk-fair RR   │
└──────────────────────────▲──────────────────────────────┘
                           │ reads
┌──────────────────────────┴──────────────────────────────┐
│  Layer 1 — Column ecology state (incremental)           │
│  Soil, niche inputs, flora context, spread preflight    │
│  Version per XZ, invalidate on break/place/succession   │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Слой 1 — `EcologyColumnState` (column cache v2)

**Задача:** один раз на колонку XZ держать всё, что нужно для spread preflight, с точечной invalidation.

**Расширить / объединить:**

- `EnvironmentalColumnCache` (rain, temp)
- `FloraContextSampler` (open/edge/forest, cover)
- `NicheSampler` inputs (moisture/light proxies на поверхности)
- `SoilKind` / succession tier (уже есть applier — читать из кэша)

**API (черновик):**

```csharp
internal sealed class EcologyColumnState
{
    bool TryGetSpreadSnapshot(IBlockAccessor acc, int x, int z, int surfaceY, out SpreadColumnSnapshot snap);
    void InvalidateColumn(int x, int z);
    void InvalidateAround(BlockPos pos, int horizontalRadius);
}
```

`SpreadColumnSnapshot` — struct: worldgen rain, forest cover, context enum, soil kind, surface block id, fluid hint, **version** (ulong).

**Invalidation:** уже есть `InvalidateEnvironmentAround` на break/place — расширить до единого hub, не три отдельных словаря с разными радиусами без координации.

**Выигрыш:** `SampleForSpread` и `SpreadPreflight` читают snapshot → меньше `GetBlock` / neighbor scan на кандидата.

**Риск:** stale snapshot при редких edge cases → version check перед `SetBlock` в commit phase (re-validate top 1 candidate).

---

## 5. Слой 2 — Ecology wake (event-driven due)

**Задача:** растение «просыпается» когда мир рядом изменился, а не только когда истёк таймер.

### 5.1. `EcologyWakeIndex`

Per-chunk список reproducer indices + per-entry `WakeState`:

| Поле | Смысл |
|------|--------|
| `NextCalendarAttemptHours` | как сейчас `NextAttemptHours` (fallback) |
| `WakeGeneration` | bump при событии в радиусе |
| `LastProcessedGeneration` | executor уже обработал это поколение |

**События wake (increment generation на соседей):**

- `DidBreakBlock` / `DidPlaceBlock` (ecology-relevant blocks)
- `SoilSuccessionApplier` после смены почвы
- `FloraSymbiosis` cascade (хост срублен)
- Season boundary (грубо: раз в игровые сутки или при смене месяца — только для species с `WildSpeciesSeason`)
- Chunk load (один wake pass для колонки — уже почти есть через scan)

**Радиус wake:** `max(ReproduceRadius, spacing, FloraContextNeighborRadius)` — один конфиг `EcologyWakeRadiusBlocks` или derive.

### 5.2. Calendar fallback (не убираем)

Стабильная клетка в стабильной нише без событий — **редкий** calendar check (текущий interval сохраняется). Иначе мир «застынет» без player action.

Правило:

```
due = (WakeGeneration > LastProcessedGeneration)
   OR (now >= NextCalendarAttemptHours)
```

После успешной попытки: `LastProcessedGeneration = WakeGeneration`, `NextCalendarAttemptHours = now + interval`.

**Геймплей:** spread станет **реактивнее** к рубке/вспашке/пожару травы — это фича, не баг. Задокументировать в handbook.

---

## 6. Слой 3 — Chunk-fair spread executor

**Задача:** при полном scope (все loaded chunks) **равномерно** тратить ms, не кластеризуя в одном биоме.

**Паттерн:** как `CyclicTreeTrunkScanner` + `ChunkEcologyColumnPass` resume.

### 6.1. `SpreadChunkScheduler`

- `roundRobin` — sorted `Vec2i` чанков с `byChunk.Count > 0`
- `chunkEntryCursor` — per-chunk index в списке reproducers
- Каждый reproduce-тик:
  1. Обойти до `MaxChunksVisitedPerTick` чанков по RR
  2. Из каждого — до `MaxSpreadAttemptsPerChunkPerTick` **woken/due** entries
  3. Остановка по `SpreadBudgetMs` (страховка)

**Интеграция с due-heap:**

- Вариант A (проще): heap только внутри чанка (per-chunk mini-heap по `NextAttemptHours` / wake priority)
- Вариант B: global heap + при pop skip если chunk quota исчерпана (сложнее)

**Рекомендация:** **Вариант A** — `Dictionary<Vec2i, ReproducerDueHeap>` или due-list per chunk; global heap оставить для migration, потом удалить.

### 6.2. Новые конфиги (не throttle scope)

```json
"EnableChunkFairSpread": true,
"MaxSpreadAttemptsPerChunkPerTick": 2,
"MaxSpreadChunksVisitedPerTick": 32
```

При мощном CPU: поднять attempts/chunk и chunks/tick — **мир богаче**, не «отрезание».

---

## 7. Слой 4 — Two-phase placement pipeline

**Задача:** отделить дорогой **оценочный** проход от дорогого **commit** (`SetBlock`, displacement, soil).

### Phase A — Intent (можно несколько за тик, дешевле)

```
TryEvaluateSpread(entry) → PendingSpread? 
  - CollectSpreadCandidates
  - pick best cell
  - НЕ SetBlock
  - enqueue PendingSpread
```

### Phase B — Commit (chunk-fair, budget)

```
Drain pending queue (round-robin by target chunk)
  - re-validate snapshot version + block still air
  - PlaceSpreadBlock / displace
```

**Выигрыш:** оценка 10 кандидатов и 1 commit не в одном spike; можно распределить commits по чанкам.

**Scope (v3.8):** two-phase queue covers spread via `TrySpawnOffspring` (flowers, reeds, lilies, berries, trees as saplings, …). **Mycelium network** (`MyceliumNetworkSpread`) and **wild vines** (`WildVineSpread`) call `SetBlock` directly inside the reproduce `trySpread` callback — not enqueued in `PendingSpreadQueue`.

**Опционально в v3.8:** начать с chunk-fair executor без отдельной pending queue (меньше diff), queue — PR4.

---

## 8. Порядок реализации (PR roadmap)

| PR | Содержание | Риск | Тесты |
|----|------------|------|-------|
| **6.1** | `SpreadChunkScheduler` + `EnableChunkFairSpread` | Низкий | Fairness: N chunks each get ≥1 attempt over K ticks |
| **6.2** | Per-chunk due structure, deprecate global collect path | Средний | Existing `ReproducerRegistryDueQueueTests` + chunk scope |
| **6.3** | `EcologyWakeIndex` + hook break/place/succession | Средний | Wake neighbors on break; calendar fallback unchanged rate |
| **6.4** | `EcologyColumnState` unified snapshot | Средний | Invalidation + spread preflight hits cache |
| **6.5** | `PendingSpreadQueue` two-phase commit | Средний | No double-place; version re-check |
| **6.6** | Season coarse wake + docs/handbook | Низкий | Season mult still applied |
| **6.7** | Player-priority registration + empty-first spread collect | Низкий | Displacement when no vacancy; burst completes nearby chunk |

**Не в 6.x:** multithreading, LOD «fake ecology» далеко от игрока, item propagation.

---

## 9. Критерии готовности (definition of done)

1. **Scope:** при `OnlyActivateNearPlayers: false` spread coverage по loaded chunks не хуже baseline за игровой год (±5% attempts, интеграционный тест / профиль лог).
2. **Fairness:** stddev spread commits per chunk за 10 reproduce-тиков ↓ (метрика в `EnableReproduceTickProfiling`).
3. **Latency:** p95 server tick cost reproduce handler не выше baseline при том же `SpreadBudgetMs`.
4. **Reactivity:** break turf рядом → wake → spread attempt в течение 1–2 reproduce-тиков (playtest + unit).
5. **Документация:** PROGRESS, GAPS §8, handbook tuning — event-driven + chunk-fair как норма, throttles как legacy safety.

---

## 10. Метрики и профилирование (расширить текущее)

Уже есть `ReproduceTickProfiler`. Добавить:

- `chunksVisited`, `attemptsPerChunk` histogram (log line или ring buffer)
- `wakeDriven` vs `calendarDriven` attempt ratio
- `columnCacheHitRate` на spread path

Включение: `EnableReproduceTickProfiling` (без порога registry для dev).

---

## 11. Обратная совместимость конфига

| Ключ | После Phase 6 |
|------|----------------|
| `OnlyActivateNearPlayers` | Оставить; документировать как legacy / weak server |
| `LimitSpreadNearPlayers` | Оставить; не рекомендовать на мощном железе. **Документировать:** ограничивает spread, stress и tree aging (не chunk registration) |
| `MaxReproduceAttemptsPerTick` | Становится **потолок суммарный**; реальный лимит = `chunks × perChunk` |
| `StaggerReproduceAttempts` | Оставить для регистрации |
| `TickBudgetMs` | Safety net |

---

## 12. Открытые вопросы (решить перед 6.3)

1. **Wake на displacement** — когда A вытесняет B, будить B’s neighbors или только origin A?
2. **MP несколько игроков** — chunk RR по всем loaded или weighted near any player? (предложение: pure RR по всем registry chunks — честнее для «идеала»)
3. **Mycelium / vine / mat spread** — ✅ общий reproduce executor + habitat hook (`WildVineSpread`, `MyceliumNetworkSpread`, mat spread for reeds/lilies). Регистрация: vines — column pass; mycelium — BE scan at load.

---

## 13. Следующий шаг

- [x] **PR 6.1** — `SpreadChunkScheduler` + `EnableChunkFairSpread` (default true)
- [x] **PR 6.3** — `EcologyWakeIndex` / `WakeAround` + break/place/displacement/succession hooks
- [x] **PR 6.4** — `EcologyColumnState` + `SpreadColumnSnapshot` + invalidation hub
- [x] **PR 6.5** — `PendingSpreadQueue` two-phase evaluate/commit
- [x] **PR 6.6** — season coarse wake + handbook
- [x] **PR 6.7** — `RegistrationScanQueue` priority/burst; empty-first spread + `EcologyColumnOccupancy` hint
- [x] **PR 6.7b** — `PendingRegistrationQueue` paced apply; `BackgroundRegistrationScanner` worker classify; foliage sync decoupled (`FoliageChunkSyncPass` on main)

### Registration pipeline (6.7b)

| Stage | Thread | What |
|-------|--------|------|
| Snapshot build | Main | Copy `block.Id` per column cell (`MaxRegistrationSnapshotCellsPerTick`) |
| Column classify | Worker | Flower / vine / tree hits from snapshot (no `SetBlock`) |
| Mycelium anchor scan | Main at chunk load | `MyceliumChunkRegistrar` — vanilla BE list → `RegisterMyceliumAnchor` |
| Registry apply | Main | `RegisterReproducer` from pending queue (`MaxRegistryAppliesPerTick`) |
| Foliage sync (chunk mode) | Main | `FoliageCellScheduler.ProcessChunkSyncBatch` when background scan on |

Config: `EnableBackgroundRegistrationScan`, `MaxRegistrationSnapshotCellsPerTick`, `MaxRegistryAppliesPerTick`, `MaxPriorityRegistryAppliesPerTick`, `BurstRegistrationBudgetMs`, `PlayerRegistrationPriorityRadiusBlocks` (16).

### Tick scheduling (6.7c)

Three desynced server tick handlers (defaults **2000 / 2300 / 5500** ms):

| Handler | Interval | Work |
|---------|:--------:|------|
| `OnReproduceTick` | 2000 ms | Chunk-fair spread, pending spread commit, vine/mycelium reproduce |
| `OnChunkScanTick` | 2300 ms | Snapshot build, worker classify, paced registry apply, foliage sync batch |
| `OnStressTick` | 5500 ms | Round-robin stress checks |

Stress no longer shares `TickBudgetMs` with spread. Chunk scan interval is intentionally not a multiple of reproduce — reduces aligned spikes when many chunks load.

**Perf fixes (2026-06-18):** priority radius 16, burst 80 ms, worker null-safety on snapshot, fallen sticks via `SurfacePlacement`, `OnDidBreakBlock` skips wake when block is not ecology plant / registry / forest-context / event target (e.g. `loosestick-free`; breaking `leaves-*` or `log-grown` still wakes).
