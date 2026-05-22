# Visual Studio — отладка и мониторинг

## Быстрый старт

1. Откройте **`wildfarming.sln`** в Visual Studio 2022 (17.8+).
2. Если F5 не находит игру, выполните один раз:
   ```powershell
   .\tools\Configure-VS.ps1
   ```
3. Выберите профиль запуска **Vintage Story (F5)** на панели отладки.
4. **F5** — сборка, копирование мода в `Mods\wildfarming`, запуск `Vintagestory.exe` с подключённым отладчиком.

## Сборка и деплой

| Конфигурация | Выход | Деплой в игру |
|--------------|-------|----------------|
| **Debug** | `bin\Debug\Mods\wildfarming\` | Да → `%AppData%\Vintagestory\Mods\wildfarming\` |
| **Release** | `bin\Release\Mods\wildfarming\` | Да (можно отключить) |

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
5. Убедитесь, что в `Mods\wildfarming` лежит свежая сборка (пересоберите Debug).

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

`%AppData%\VintagestoryData\ModConfig\wildfarming-ecosystem.json` — создаётся при первом запуске.

| Поле | По умолчанию | Смысл |
|------|----------------|-------|
| `ReproduceRadius` | 4 | Радиус дикого размножения |
| `ReproduceChance` | 0.08 | Шанс попытки |
| `MinFitness` | 0.65 | Порог только для **reproduce**, не для посадки |
| `MaxFailedSurvivalChecks` | 5 | Сколько раз ×18ч в плохом климате до смерти ростка |
| `HarshWildPlants` | true | Учитывать min/maxTemp при выживании |

## Устранение проблем

| Симптом | Действие |
|---------|----------|
| F5: файл не найден | `.\tools\Configure-VS.ps1` |
| Breakpoint серый | Пересобрать Debug; проверить `wildfarming.pdb` в `Mods\wildfarming` |
| Мод не обновляется в игре | Проверить `DeployModToGame`; удалить старую папку мода вручную |
| CS1705 System.Runtime | TargetFramework в csproj должен совпадать с игрой (`net10.0`) |
