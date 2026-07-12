# Phase 7 — External ecology simulation (unloaded chunks)

> План на **v5.0+**. Цель: **экология продолжается в выгруженных chunk columns**, пока игрок далеко — без удержания полного registry в RAM сервера VS.

См. также: [PHASE6_SIMULATION.md](PHASE6_SIMULATION.md) (loaded chunks, in-process), [BACKGROUND_REGISTRATION.md](BACKGROUND_REGISTRATION.md), [PROJECT_VISION.md §12](PROJECT_VISION.md#12-производительность-roadmap), [PROGRESS.md](PROGRESS.md), [GAPS.md](GAPS.md).

**Статус:** 📋 design only — код не реализован.

---

## 1. Проблема (почему Phase 6 недостаточно)

Phase 6 оптимизирует симуляцию **loaded chunks**: chunk-fair spread, background snapshot workers, two-phase commit на main thread.

При `ChunkColumnUnloaded` мод **сбрасывает** локальное состояние:

- `registry.RemoveChunk`
- `SpacingIndex`, `foliageCells`, pending registration
- spread/stress/phenology для этой колонки **останавливаются**

Пока чанк не в памяти VS, мир в этой зоне **заморожен** с точки зрения экологии. Игрок, вернувшись через игровые месяцы, видит ландшафт «как в момент ухода», а не результат календарного времени.

| Режим | Scope | Где считается |
|-------|--------|---------------|
| **Phase 6 (сейчас)** | Loaded chunk columns | C# in-process, main + worker threads |
| **Phase 7 (цель)** | Unloaded + loaded | External store + sim worker; VS **применяет** diffs |

Phase 7 **не заменяет** Phase 6 для loaded chunks на первом этапе — дополняет его. Loaded зона может оставаться authoritative in-process; unloaded — fast-forward во внешнем хранилище.

---

## 2. Принципы

| Да | Нет |
|----|-----|
| Симуляция выгруженных колонок по **игровому календарю** | Полный mirror region-файлов VS в БД |
| VS — **единственный источник правды** для блоков в save | Прямой патч `.vcd` / region files с диска |
| `SetBlock` / `RegisterReproducer` — **только main thread** | Spread/stress в worker threads с `BlockAccessor` |
| Compact **ecology snapshot** per column | HTTP round-trip на каждый reproduce tick |
| Optional режим (`EnableExternalEcologySim`) | Обязательный sidecar для всех установок |
| Graceful shutdown дочернего процесса | Orphan Go-процессы после kill игры |

**Жёсткое ограничение VS** (как в Phase 6): мутация мира — один server thread. Внешний worker считает **намерения** (`pending ops`); игра **инплейтит** блоки при load или paced catch-up.

---

## 3. Архитектура

```
┌──────────────────────────────────────────────────────────────────┐
│  Vintage Story server (ecosystemflora.dll)                        │
│  ┌────────────────┐   export      ┌─────────────────────────────┐ │
│  │ Loaded chunks  │──────────────►│ Ecology DB (per-world)       │ │
│  │ Phase 6 engine │   on unload   │ snapshots, reproducers, ops  │ │
│  └───────▲────────┘               └──────────────▲──────────────┘ │
│          │ import / apply diff                   │                │
│          │ on load + paced catch-up              │ batch sim      │
│  ┌───────┴────────┐               ┌──────────────┴──────────────┐ │
│  │ Main thread    │◄── localhost │ Sim worker (Go, optional)   │ │
│  │ SetBlock commit│    stream    │ job queue, spatial batches    │ │
│  └────────────────┘               └─────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### 3.1. Роли компонентов

| Компонент | Язык | Ответственность |
|-----------|------|-----------------|
| **EcologySimBridge** | C# | lifecycle, export/import, IPC, process host |
| **EcologySimProcessHost** | C# | `Process.Start` / graceful shutdown / PID file |
| **Ecology DB** | SQLite (default) или Postgres | snapshots, reproducer state, pending ops, sim cursor |
| **ecology-sim** | Go | fast-forward unloaded columns; scoring без VS API |
| **Phase 6 pipeline** | C# | loaded chunks — без изменений на этапе 1 |

### 3.2. Поток данных

```
OnChunkColumnUnloaded
  → build EcologyChunkSnapshot (compact)
  → upsert DB
  → drop in-RAM registry (как сейчас)

Sim worker (background)
  → SELECT chunks WHERE lastSimGameHour < world.GameCalendarHours
  → batch spread/stress/phenology (упрощённая копия правил)
  → INSERT pending_ops

OnChunkColumnLoaded
  → fetch pending_ops for (cx, cz)
  → paced ApplyPendingOps on main (budget ms/tick)
  → RegisterReproducer for new parents
  → merge snapshot back into Phase 6 registry
```

---

## 4. Ecology snapshot (compact)

Не копировать весь чанк 32×256×32. Достаточно **ecology-relevant column slice**:

### 4.1. Per-column record (`ecology_columns`)

| Поле | Тип | Назначение |
|------|-----|------------|
| `cx`, `cz` | int | Chunk column coord |
| `world_id` | string | Save / world name |
| `last_sim_game_hour` | double | До какого игрового часа просимулировано |
| `snapshot_version` | ulong | Bump при export из loaded chunk |
| `surface_y` | short[] | Heightmap slice (или sparse diffs) |
| `block_ids` | bytes / RLE | Surface + subsurface ecology bands (почва, растение, вода) |
| `rain`, `temp`, `forest` | float | Worldgen maps at column |
| `exported_at_utc` | timestamp | Debug / recovery |

### 4.2. Per-reproducer record (`ecology_reproducers`)

| Поле | Тип | Назначение |
|------|-----|------------|
| `id` | uuid | Stable id across export/import |
| `cx`, `cz`, `x`, `y`, `z` | int | Position |
| `species_code` | string | Registry species id |
| `next_attempt_hours` | double | Calendar due |
| `wake_generation` | uint | Event wake (Phase 6 parity) |
| `phase` | byte | Flower/fern phenology, tree age bucket, etc. |
| `extra_json` | json | Tree calendar age, berry traits hash, vine anchor |

Деревья: переносить данные, совместимые с `TreeCalendarAgeStore` / senescence phase.

### 4.3. Pending op (`ecology_pending_ops`)

| Поле | Тип | Назначение |
|------|-----|------------|
| `op_id` | uuid | Idempotency |
| `cx`, `cz` | int | Target chunk |
| `x`, `y`, `z` | int | Block pos |
| `block_code` | string | Resolved placement code |
| `op` | enum | `SetBlock`, `RemoveBlock`, `Register`, `Unregister` |
| `sim_game_hour` | double | When sim decided |
| `snapshot_version` | ulong | Reject if stale on apply |
| `applied` | bool | Commit flag |

---

## 5. Согласованность (два источника правды)

| Ситуация | Правило |
|----------|---------|
| Chunk **unloaded** | DB authoritative для ecology state; save на диске — last known blocks |
| Chunk **loaded** | VS save authoritative для блоков; export перезаписывает snapshot |
| Player break/place в loaded chunk | `InvalidateAround` + export delta; bump `snapshot_version` |
| Spread из loaded соседа в unloaded | Sim worker видит pending op / frontier column flag |
| Конфликт op vs текущий блок при apply | Re-validate на main (как two-phase spread Phase 6); op discard + log |

**Merge на load:**

1. Прочитать pending ops с `snapshot_version <=` текущей версии колонки.
2. Для каждого op — `SuitabilityEvaluator` / vacancy check (cheap).
3. `SetBlock` + registry update.
4. Пометить `applied`; устаревшие ops — skip.

---

## 6. Дочерний процесс (spawn / kill)

### 6.1. Техническая возможность

Vintage Story **не песочит** моды: `System.Diagnostics.Process` доступен на **server side**. Отдельный exe — нормальный путь.

**Только `ICoreServerAPI`:** mod `side: Universal` — spawn не на клиенте.

### 6.2. Lifecycle

| Событие VS | Действие |
|------------|----------|
| `SaveGameLoaded` | Kill stale PID (crash recovery); `Start` worker if `EnableExternalEcologySim` |
| `GameWorldSave` | Flush pending export queue; optional `POST /pause` |
| `ModSystem.Dispose` / `EcosystemSystem.Dispose` | Graceful shutdown → wait → `Kill(entireProcessTree: true)` |
| `OnChunkColumnUnloaded` | Export snapshot (не ждать worker) |
| `OnChunkColumnLoaded` | Schedule import / catch-up |

### 6.3. Process host (C# sketch)

```csharp
// EcologySimProcessHost — server-only
// - Resolve: Mods/ecosystemflora/native/{win-x64|linux-x64}/ecology-sim(.exe)
// - Args: --db "{path}" --world "{id}" --listen 127.0.0.1:{port} --token "{secret}"
// - PID file: GetOrCreateDataPath("ecosystemflora")/sim-{worldId}.pid
// - Dispose: POST /shutdown → WaitForExit(3000) → Kill(entireProcessTree: true)
```

### 6.4. Упаковка бинарника

```
Mods/ecosystemflora/
  ecosystemflora.dll
  native/
    win-x64/ecology-sim.exe
    linux-x64/ecology-sim
```

`wildfarming.csproj`: `CopyToOutputDirectory` для `native/**`. Dedicated Linux server — обязателен `linux-x64` build.

### 6.5. Безопасность

- Listen **127.0.0.1** only.
- Случайный порт + shared secret в args (не в логах).
- Нет входящих соединений извне хоста.

---

## 7. IPC и протокол

**Не HTTP request/response на каждый тик.** Долгоживущий канал:

| Транспорт | Приоритет |
|-----------|-----------|
| localhost HTTP + **SSE** или WebSocket | MVP |
| Named pipe / Unix socket (binary) | v5.1 perf |
| gRPC stream | optional |

### 7.1. Messages (MVP)

| Direction | Message | Содержание |
|-----------|---------|------------|
| C# → Go | `ExportChunk` | `EcologyChunkSnapshot` protobuf/json |
| C# → Go | `InvalidateColumn` | pos, radius, new version |
| C# → Go | `SetWorldHour` | current game calendar hours |
| Go → C# | `PendingOpsBatch` | list of ops (pull or push) |
| C# → Go | `AckOps` | applied op ids |
| C# → Go | `Shutdown` | graceful stop |

Go **не вызывает** VS API. Вся логика размещения — портированные правила или shared spec tables (species CSV).

---

## 8. Sim worker (Go)

### 8.1. Scope sim rules (этап 1)

| Система | Offline sim | Примечание |
|---------|-------------|------------|
| Meadow flower spread + phenology | ✅ | Приоритет |
| Tallgrass / fern phases | ✅ | |
| Berry colony mat edge | ✅ | |
| Reed / lily / crowfoot mat | ⚠️ | После aquatic unification |
| Tree aging / senescence | ⚠️ | Нужен `TreeCalendarAgeStore` parity |
| Canopy foliage seasonal | ⚠️ | Catch-up on load (уже есть паттерн) |
| Mycelium network | ❌ | Требует BE state — этап 2+ |
| Wild vines | ❌ | Топология стен — этап 2+ |
| Third-party species | ✅ | Те же CSV (`ecology.csv`, `season.csv`) |

### 8.2. Tiered simulation (масштаб мира)

Полный sim всех колонок карты — отдельный продукт. Практичные уровни:

| Tier | Колонки | Частота sim |
|------|---------|-------------|
| **A — hot** | Ring вокруг игроков + recently unloaded | Каждые N игровых часов |
| **B — warm** | Посещённые за последние M дней (persisted set) | Реже |
| **C — cold** | Остальной мир | Статистический tick / skip |

Конфиг: `ExternalSimHotRadiusBlocks`, `ExternalSimWarmRetentionDays`.

---

## 9. Интеграция с Phase 6

| Loaded chunk | Unloaded chunk |
|--------------|----------------|
| Phase 6 reproduce tick | Go worker fast-forward |
| Background spread solve | Batch в Go |
| `RegisterReproducer` on main | Op `Register` on load |
| `OnChunkColumnUnloaded` → export | Continues in DB |

**Переход loaded → unloaded:** финальный export включает registry slice + column cache hints.

**Переход unloaded → loaded:** import pending ops **до** или **параллельно** с `RegistrationScanQueue` (избежать double-register).

---

## 10. Конфиг (planned keys)

Добавить в `ecosystemflora.json` при реализации (все default **off**):

| Key | Type | Default | Side |
|-----|------|---------|------|
| `EnableExternalEcologySim` | bool | false | server |
| `ExternalSimDbPath` | string | `""` (= auto under ModData) | server |
| `ExternalSimAutoStartProcess` | bool | true | server |
| `ExternalSimExecutablePath` | string | `""` (= bundled native) | server |
| `ExternalSimListenPort` | int | 0 | server |
| `ExternalSimHotRadiusBlocks` | int | 256 | server |
| `ExternalSimCatchUpBudgetMs` | int | 25 | server |
| `ExternalSimMaxOpsPerChunkPerTick` | int | 64 | server |

Документировать в [CONFIGURATION.md](CONFIGURATION.md) при merge кода.

---

## 11. Этапы реализации

### 11.1. Milestone A — persistence без Go

- [ ] `EcologyChunkSnapshot` serialize/deserialize
- [ ] SQLite per-world в `ModData/ecosystemflora/worlds/{id}/`
- [ ] Export on `OnChunkColumnUnloaded`
- [ ] In-process C# `UnloadedChunkFastForward` (proof of concept)
- [ ] Catch-up on `OnChunkColumnLoaded` (paced apply)
- [ ] Tests: round-trip snapshot, op idempotency

### 11.2. Milestone B — process host

- [ ] `EcologySimProcessHost` spawn/kill/PID stale
- [ ] Minimal Go binary: read DB, write pending ops
- [ ] localhost HTTP health + shutdown
- [ ] Integration test: start world → unload chunk → sim → load → blocks changed

### 11.3. Milestone C — rule parity

- [ ] Port spread/scoring для terrestrial + mat
- [ ] Species CSV hot-reload into Go
- [ ] Tiered sim + profiling
- [ ] Handbook + changelog v5.0

### 11.4. Milestone D — advanced

- [ ] Mycelium / vines offline (or explicit «freeze when unloaded»)
- [ ] Postgres option for large dedicated servers
- [ ] Binary IPC

---

## 12. Риски и ограничения

| Риск | Митигация |
|------|-----------|
| Orphan process после crash | PID file + kill on next `SaveGameLoaded` |
| Дублирование логики C# / Go | Shared CSV + contract tests; генерировать tables из одного источника |
| ModDB / antivirus на bundled exe | Optional feature; declare in description |
| Hosting без `Process.Start` | `ExternalSimAutoStartProcess: false` + manual worker |
| Rule drift (sim ≠ in-game) | Re-validate on apply; telemetry `op_discarded_stale` |
| Размер БД | RLE snapshots; prune applied ops; tier C statistical |

---

## 13. Не цели (v5.0)

- Симуляция **несгенерированных** чанков (только explored/once-loaded).
- Замена Phase 6 для loaded chunks.
- Клиентский spawn процессов.
- Патч save-файлов VS минуя game API.
- Fauna / мобы (отдельный modid — см. GAPS).

---

## 14. Версионирование

| Версия | Содержание |
|--------|------------|
| **4.7.x** | Phase 6, third-party bootstraps (текущее) |
| **5.0.0** | Phase 7 MVP: export/import + optional Go worker + catch-up on load |
| **5.1+** | Rule parity, tiered sim, binary IPC |

Player-facing notes — в [CHANGELOG.md](CHANGELOG.md) при релизе.

---

## 15. Ссылки на текущий код (touch points)

| Файл | Изменения при реализации |
|------|--------------------------|
| `src/Ecosystem/EcosystemSystem.cs` | `OnChunkColumnUnloaded` / `Loaded` → bridge; dispose host |
| `src/WF.cs` | Wire `EcologySimBridge` lifecycle |
| `src/Ecosystem/TreeCalendarAgeStore.cs` | Export/import tree age |
| `src/Ecosystem/ReproducerRegistry.cs` | Serialize registry slice per chunk |
| **new** `src/Ecosystem/ExternalSim/` | Bridge, ProcessHost, Snapshot, Db |

---

*Документ создан: 2026-07-09. Обсуждение: external sim для unloaded chunks + spawn/kill Go worker из мода.*
