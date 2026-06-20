# Generates assets/ecosystemflora/lang/en-configfields.json and ru-configfields.json
# Run: dotnet build && powershell -File tools/GenerateConfigFieldLang.ps1

$ErrorActionPreference = "Stop"
$repo = if (Test-Path "$PSScriptRoot\..\wildfarming.csproj") { Join-Path $PSScriptRoot ".." } else { $PSScriptRoot }
$dll = Join-Path $repo "bin\Debug\Mods\ecosystemflora\ecosystemflora.dll"
if (-not (Test-Path $dll)) {
    throw "Build the mod first: dotnet build $repo\wildfarming.csproj"
}

Add-Type -Path $dll
$fields = [WildFarming.Ecosystem.Config.EcosystemConfigSchema]::Fields

function Split-Camel([string]$name) {
    if ([string]::IsNullOrEmpty($name)) { return $name }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append($name[0])
    for ($i = 1; $i -lt $name.Length; $i++) {
        $c = $name[$i]
        if ([char]::IsUpper($c) -and -not [char]::IsUpper($name[$i - 1])) { [void]$sb.Append(' ') }
        [void]$sb.Append($c)
    }
    return $sb.ToString()
}

function Get-FieldText($field) {
    $n = $field.Name
    $kind = $field.Kind.ToString()
    $titleEn = Split-Camel $n
    $titleRu = $titleEn

    switch -Regex ($n) {
        '^BalancePreset$' {
            return @{
                title_en = "Balance preset"; title_ru = "Пресет баланса"
                desc_en = "natural/lush/sparse apply bundled spread values. custom keeps your manual spread tuning across restarts."
                desc_ru = "natural/lush/sparse задают набор spread. custom сохраняет ваши ручные значения между перезапусками."
            }
        }
        '^Enable(?<rest>.+)$' {
            $rest = $Matches.rest
            $featEn = (Split-Camel $rest).ToLowerInvariant()
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: enables $featEn. Off: disables it (less simulation work; behavior falls back where noted in handbook)."
                desc_ru = "Вкл.: включает $featEn. Выкл.: отключает (меньше нагрузки; поведение см. handbook)."
            }
        }
        '^Use(?<rest>.+)$' {
            $rest = $Matches.rest
            $featEn = (Split-Camel $rest).ToLowerInvariant()
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: uses $featEn in spread/survival scoring. Off: ignores that layer (simpler, less niche-aware)."
                desc_ru = "Вкл.: учитывает $featEn в spread/выживании. Выкл.: слой игнорируется (проще, менее «нишево»)."
            }
        }
        '^Apply(?<rest>.+)$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: applies this worldgen/climate gate to spread fitness. Off: ignores that gate."
                desc_ru = "Вкл.: этот климатический фильтр участвует в fitness. Выкл.: фильтр не используется."
            }
        }
        '^Prefer(?<rest>.+)$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: prefers empty cells when spreading (gap colonization). Off: treats empty and occupied more evenly."
                desc_ru = "Вкл.: при spread приоритет пустым клеткам. Выкл.: пустые и занятые ближе по весу."
            }
        }
        '^Respect(?<rest>.+)$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: ecology respects protected claims (no spread/stress/trees inside). Off: claims ignored."
                desc_ru = "Вкл.: экология не трогает защищённые участки. Выкл.: участки игнорируются."
            }
        }
        '^Clone(?<rest>.+)$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "On: offspring copies parent bush genetics. Off: vanilla random wild traits on new bushes."
                desc_ru = "Вкл.: потомство копирует гены родителя. Выкл.: ванильные случайные черты."
            }
        }
        '(?i)Chance$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher (→1): more likely. Lower (→0): rarer. Affects how often this roll succeeds."
                desc_ru = "Больше (→1): чаще срабатывает. Меньше (→0): реже. Влияет на успех броска."
            }
        }
        '(?i)Fitness$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: pickier placement (fewer weak sites). Lower: colonizes marginal cells more often."
                desc_ru = "Больше: избирательнее (меньше слабых клеток). Меньше: чаще на marginal-клетках."
            }
        }
        '(?i)Penalty$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: stronger penalty (slower spread there). Lower: milder penalty."
                desc_ru = "Больше: сильнее штраф (медленнее spread). Меньше: слабее штраф."
            }
        }
        '(?i)Bonus$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: stronger bonus (faster spread there). Lower: weaker bonus."
                desc_ru = "Больше: сильнее бонус (быстрее spread). Меньше: слабее бонус."
            }
        }
        '(?i)Multiplier$|(?i)Scale$|(?i)Strength$|(?i)Activity$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: stronger/faster effect. Lower: subtler/slower. 1.0 ≈ default tuning."
                desc_ru = "Больше: сильнее/быстрее. Меньше: слабее/медленнее. 1.0 ≈ дефолт."
            }
        }
        '(?i)Radius$|(?i)Blocks$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: larger area included. Lower: tighter/local effect."
                desc_ru = "Больше: шире зона действия. Меньше: локальнее."
            }
        }
        '(?i)PerTick$|Max(?i).*(Attempts|Checks|Registrations|Applies|Commits|Scans|Columns|Cells|Growth|CatchUp|Work)' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: more work per tick (faster catch-up, higher CPU). Lower: gentler pacing."
                desc_ru = "Больше: больше работы за тик (быстрее догоняет, выше CPU). Меньше: мягче темп."
            }
        }
        '(?i)BudgetMs$|TickBudgetMs$|SpreadBudgetMs$|RegistrationBudgetMs$|StressBudgetMs$|FoliageBudgetMs$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: more milliseconds allowed per tick (smoother sim, more CPU). 0 = use parent budget / unlimited alias."
                desc_ru = "Больше: больше мс за тик (ровнее, выше CPU). 0 = родительский бюджет / без лимита."
            }
        }
        '(?i)IntervalMs$|TickIntervalMs$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: less frequent ticks (less CPU, slower reactions). Lower: more frequent ticks."
                desc_ru = "Больше: реже тики (меньше CPU, медленнее реакции). Меньше: чаще тики."
            }
        }
        '(?i)IntervalHours$|RecheckHours$|CacheHours$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: slower rechecks/caching (less CPU). Lower: faster updates."
                desc_ru = "Больше: реже пересчёт/дольше кэш (меньше CPU). Меньше: чаще обновления."
            }
        }
        '(?i)Seconds$|CooldownSeconds$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: longer delay between actions. Lower: more responsive."
                desc_ru = "Больше: длиннее пауза. Меньше: отзывчивее."
            }
        }
        '(?i)AttemptsPerYear$|PerYear$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: more attempts each in-game year (denser world). Lower: sparser spread."
                desc_ru = "Больше: больше попыток за игровой год (густее). Меньше: реже spread."
            }
        }
        '(?i)Threshold$|Margin$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: stricter gate (harder to pass). Lower: easier to pass."
                desc_ru = "Больше: строже порог (труднее пройти). Меньше: легче пройти."
            }
        }
        '(?i)Spacing$' {
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: plants must stay farther apart (patchier meadows). Lower: denser stands allowed."
                desc_ru = "Больше: дальше друг от друга (пятнистее). Меньше: плотнее можно."
            }
        }
        '^FoliageSyncMode$' {
            return @{
                title_en = "Foliage sync mode"; title_ru = "Режим синхронизации листвы"
                desc_en = "chunk = sync on chunk load (default). hybrid = chunk + random tick. random = legacy random only."
                desc_ru = "chunk = при загрузке чанка (дефолт). hybrid = chunk + случайный тик. random = только legacy."
            }
        }
        '^FoliageSyncMode$|^BalancePreset$' { break }
        '^HarshWildPlants$' {
            return @{
                title_en = "Harsh wild plants"; title_ru = "Строгий климат"
                desc_en = "On: species climate/soil bounds apply (wrong niche → stress). Off: softer survival."
                desc_ru = "Вкл.: границы климата/почвы вида (не та ниша → стресс). Выкл.: мягче."
            }
        }
        '^EcosystemEnabled$' {
            return @{
                title_en = "Ecosystem enabled"; title_ru = "Экосистема включена"
                desc_en = "On: spread, competition, stress, most ecology ticks. Off: mod ecology idle."
                desc_ru = "Вкл.: spread, конкуренция, стресс, тики. Выкл.: экология простаивает."
            }
        }
        '^OnlyActivateNearPlayers$' {
            return @{
                title_en = "Only near players (playtest)"; title_ru = "Только у игроков (playtest)"
                desc_en = "On: spread, stress, trees, chunk scans only within ~192 blocks of a player. Off: all loaded chunks."
                desc_ru = "Вкл.: spread/стресс/деревья/сканы только ~192 блока от игрока. Выкл.: все загруженные чанки."
            }
        }
        '^LimitSpreadNearPlayers$' {
            return @{
                title_en = "Limit spread near players"; title_ru = "Spread только у игроков"
                desc_en = "On: spread/stress/tree aging only near players; chunk registration unchanged. Off: full loaded-chunk sim."
                desc_ru = "Вкл.: spread/стресс/деревья у игроков; регистрация чанков как была. Выкл.: полная симуляция."
            }
        }
        '^VerboseLogging$' {
            return @{
                title_en = "Verbose logging"; title_ru = "Подробные логи"
                desc_en = "On: extra Notification/Warning lines (CPU cost). Off: errors/startup only."
                desc_ru = "Вкл.: больше Notification/Warning (нагрузка). Выкл.: только ошибки/старт."
            }
        }
        '^ReproduceDebug$' {
            return @{
                title_en = "Spread debug log"; title_ru = "Лог spread (debug)"
                desc_en = "On: log spread attempts (use with VerboseLogging). Off: silent spread."
                desc_ru = "Вкл.: логировать попытки spread (с VerboseLogging). Выкл.: без лога."
            }
        }
        '^StaggerReproduceAttempts$' {
            return @{
                title_en = "Stagger spread attempts"; title_ru = "Размазать spread"
                desc_en = "On: random initial delay per plant (smoother CPU). Off: synchronized first ticks."
                desc_ru = "Вкл.: случайная задержка (ровнее CPU). Выкл.: синхронный старт."
            }
        }
        '^BerryTraitMutationChance$' {
            return @{
                title_en = "Berry trait mutation"; title_ru = "Мутация черт ягод"
                desc_en = "Higher: spread offspring more often lose a random trait. 0 = no mutations."
                desc_ru = "Больше: чаще теряется случайная черта при spread. 0 = без мутаций."
            }
        }
        '^EcologyWakeRadiusBlocks$' {
            return @{
                title_en = "Ecology wake radius"; title_ru = "Радиус пробуждения"
                desc_en = "0 = auto from spread radius/spacing. Higher: wake more neighbors on block changes."
                desc_ru = "0 = авто от радиуса/spacing. Больше: больше соседей просыпается при изменениях."
            }
        }
        default {
            if ($kind -eq "Boolean") {
                return @{
                    title_en = $titleEn; title_ru = $titleRu
                    desc_en = "On: feature active. Off: feature inactive."
                    desc_ru = "Вкл.: функция активна. Выкл.: неактивна."
                }
            }
            if ($kind -eq "String") {
                return @{
                    title_en = $titleEn; title_ru = $titleRu
                    desc_en = "Select value/mode. Each option changes algorithm — see handbook."
                    desc_ru = "Выбор режима. Каждое значение меняет алгоритм — см. handbook."
                }
            }
            return @{
                title_en = $titleEn; title_ru = $titleRu
                desc_en = "Higher: stronger or more frequent. Lower: weaker or more limited."
                desc_ru = "Больше: сильнее или чаще. Меньше: слабее или уже."
            }
        }
    }
}

$en = [ordered]@{}
$ru = [ordered]@{}

foreach ($f in ($fields | Sort-Object Name)) {
    $text = Get-FieldText $f
    $en["ecosystemflora:config-field-$($f.Name)"] = $text.title_en
    $en["ecosystemflora:config-field-$($f.Name)-desc"] = $text.desc_en
    $ru["ecosystemflora:config-field-$($f.Name)"] = $text.title_ru
    $ru["ecosystemflora:config-field-$($f.Name)-desc"] = $text.desc_ru
}

$en["ecosystemflora:config-field-FoliageSyncMode-val-chunk"] = "Chunk sync"
$en["ecosystemflora:config-field-FoliageSyncMode-val-hybrid"] = "Hybrid"
$en["ecosystemflora:config-field-FoliageSyncMode-val-random"] = "Random tick"
$ru["ecosystemflora:config-field-FoliageSyncMode-val-chunk"] = "Синхронизация чанков"
$ru["ecosystemflora:config-field-FoliageSyncMode-val-hybrid"] = "Гибрид"
$ru["ecosystemflora:config-field-FoliageSyncMode-val-random"] = "Случайный тик"

$langDir = Join-Path $repo "assets\ecosystemflora\lang"
$en | ConvertTo-Json | Set-Content -Path (Join-Path $langDir "en-configfields.json") -Encoding UTF8
$ru | ConvertTo-Json | Set-Content -Path (Join-Path $langDir "ru-configfields.json") -Encoding UTF8
Write-Host "Generated $($fields.Count) config field lang entries."
