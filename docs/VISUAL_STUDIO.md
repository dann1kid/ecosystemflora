# Visual Studio — отладка и мониторинг

## Быстрый старт

1. Откройте **`wildfarming.sln`** в Visual Studio 2022 (17.8+).
2. Если F5 не находит игру, выполните один раз:
   ```powershell
   .\tools\Configure-VS.ps1
   ```
3. Выберите профиль запуска **Vintage Story (F5)** на панели отладки.
4. **F5** — сборка, копирование мода в `Mods\ecosystemflora`, запуск `Vintagestory.exe` с подключённым отладчиком.

**Submodule `community/`:** после `git submodule update --init community` в solution есть проект **ecosystemfloracompat** (content-only). Сборка solution копирует патчи в `Mods\ecosystemfloracompat\` (`community\bin\…`). См. [`community/docs/BUILD.md`](../community/docs/BUILD.md).

## Сборка и деплой

| Проект | Выход | Деплой в игру |
|--------|-------|----------------|
| **wildfarming** (Debug) | `bin\Debug\Mods\ecosystemflora\` | `%AppData%\Vintagestory\Mods\ecosystemflora\` |
| **ecosystemfloracompat** (Debug) | `community\bin\Debug\Mods\ecosystemfloracompat\` | `%AppData%\Vintagestory\Mods\ecosystemfloracompat\` |
| **Release** | те же пути, `Release` | Да (можно отключить) |

Отключить автокопирование в свойствах проекта → **Build** → переменная MSBuild `DeployModToGame=false`,  
или в `Directory.Build.user.props` в корне репозитория.

## Исключения (Exception Settings)

Чтобы отладчик **останавливался на ваших исключениях** в коде мода:

1. **Debug → Windows → Exception Settings** (Ctrl+Alt+E).
2. Разверните **Common Language Runtime Exceptions**.
3. Для разработки мода включите **Thrown** (галочка на уровне CLR) или точечно:
   - `System.NullReferenceException`
   - `System.InvalidOperationException`
   - `System.ArgumentException`
4. Снимите **User-unhandled** только если мешают штатные исключения внутри игры — иначе оставьте как есть и добавляйте **Continue** (F5) при известных безобидных сбоях API.

Сохранить набор настроек: кнопка **Save** в окне Exception Settings → файл `.vssettings` (личный, в репозиторий не коммитить).

## Мониторинг при отладке

| Инструмент | Как открыть | Зачем |
|------------|-------------|--------|
| **Diagnostic Tools** | Автоматически при F5 | CPU, память, события |
| **Perfetto / CPU Usage** | Debug → Performance Profiler | Профилирование тиков `EcosystemSystem` |
| **Output** | View → Output → Debug | `api.Logger` / `Debug.WriteLine` |
| **Immediate Window** | Ctrl+Alt+I | Выражения во время паузы |

Файл **`wildfarming.runsettings`** в корне решения можно выбрать:  
**Test → Configure Run Settings → Select Solution Wide runsettings File** — включает сбор diagnostics hub для сессий с тестами/профилированием.

## Attach to process (игра уже запущена)

1. Запустите Vintage Story обычным ярлыком.
2. В VS: **Debug → Attach to Process** (Ctrl+Alt+P).
3. Процесс: **`Vintagestory.exe`**.
4. Тип кода: **Managed (.NET Core, .NET 5+)** или **Automatic**.
5. Убедитесь, что в `Mods\ecosystemflora` лежит свежая сборка (пересоберите Debug).

## Структура solution

```
wildfarming.sln
├── wildfarming (проект)
└── docs (Solution Items — vision, prompt, этот файл)
```

## Требования

- Visual Studio 2022 с workload **.NET desktop development**
- .NET SDK **10.x** (как у установленной Vintage Story)
- Игра: `Vintagestory.exe` в `%AppData%\Vintagestory\`

## Конфиг мода (в игре)

`%AppData%\VintagestoryData\ModConfig\ecosystemflora.json` — создаётся при первом запуске.

Полный справочник всех ключей: [`CONFIGURATION.md`](CONFIGURATION.md). Шаблон: `assets/ecosystemflora/ecosystemflora.example.json`.

| Поле | По умолчанию | Смысл |
|------|----------------|-------|
| `BalancePreset` | `"natural"` | Пресет перезаписывает 5 полей spread при старте (`custom` — свои значения) |
| `ReproduceRadius` | 4 | Радиус дикого размножения |
| `ReproduceChance` | 0.50 | Шанс попытки (при пресете `natural`) |
| `MinFitness` | 0.45 | Порог только для **reproduce**, не для посадки |
| `MaxFailedSurvivalChecks` | 5 | Неудачных проверок выживания до смерти от стресса |
| `HarshWildPlants` | true | Учитывать min/maxTemp при выживании |
| `GrowthHoursMultiplier` | 1 | Скорость созревания spread-ростков (colonizers); см. [`FLOWER_SPREAD_MATURATION.md`](FLOWER_SPREAD_MATURATION.md) |
| `EnableFlowerSpreadMaturation` | true | Juvenile flower spread (v3.9.6) |
| `EnableTallgrassSpreadMaturation` | true | Veryshort tallgrass spread + promotion queue (v3.9.7); см. [`TALLGRASS_SPREAD_MATURATION.md`](TALLGRASS_SPREAD_MATURATION.md) |
| `LimitSpreadNearPlayers` | false | Spread/stress/деревья только у игроков; регистрация чанков без изменений |
| `CloneBerryTraits` | true | Клонирование traits ягодника при spread |
| `EnableThirdPartyParticipants` | true | JSON-участники из других модов (`ecologyParticipant`) |

## Устранение проблем

| Симптом | Действие |
|---------|----------|
| F5: файл не найден | `.\tools\Configure-VS.ps1` |
| Breakpoint серый | Пересобрать Debug; проверить `ecosystemflora.pdb` в `Mods\ecosystemflora` |
| Мод не обновляется в игре | Проверить `DeployModToGame`; удалить старую папку мода вручную |
| CS1705 System.Runtime | TargetFramework в csproj должен совпадать с игрой (`net10.0`) |
