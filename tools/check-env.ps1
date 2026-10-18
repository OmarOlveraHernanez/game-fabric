# Game Factory - verificacion del entorno (solo lectura, no instala ni cambia nada)
# Uso: powershell -ExecutionPolicy Bypass -File tools\check-env.ps1

$ok = 0; $fail = 0
function Pass($m) { Write-Host "[OK]    $m" -ForegroundColor Green; $script:ok++ }
function Miss($m) { Write-Host "[FALTA] $m" -ForegroundColor Red; $script:fail++ }
function Info($m) { Write-Host "        $m" -ForegroundColor Gray }

Write-Host "`n=== Herramientas de linea de comandos ===" -ForegroundColor Cyan
foreach ($t in @(
    @{n='Git';         c={ git --version }},
    @{n='Git LFS';     c={ git lfs version }},
    @{n='Claude Code'; c={ claude --version }}
)) {
    try { $v = & $t.c 2>$null; if ($LASTEXITCODE -eq 0 -and $v) { Pass "$($t.n): $v" } else { Miss $t.n } }
    catch { Miss $t.n }
}
$codeExe = @("$env:LOCALAPPDATA\Programs\Microsoft VS Code\Code.exe","C:\Program Files\Microsoft VS Code\Code.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($codeExe) {
    Pass "VS Code: $codeExe"
    $cli = Get-Command code -ErrorAction SilentlyContinue
    if (-not $cli) { Info "(el comando 'code' no esta en PATH; no es grave)"; $cli = Join-Path (Split-Path $codeExe) "bin\code.cmd" } else { $cli = $cli.Source }
    $ext = & $cli --list-extensions 2>$null
    if ($ext -match 'visualstudiotoolsforunity.vstuc') { Pass "  Extension Unity" } else { Miss "  Extension Unity de VS Code" }
} else { Miss "VS Code" }
$gitName = git config --global user.name 2>$null
if ($gitName) { Pass "Git user.name: $gitName" } else { Miss "Git user.name sin configurar" }

$bash = "C:\Program Files\Git\bin\bash.exe"
if (Test-Path $bash) { Pass "Git Bash: $bash" } else { Miss "Git Bash no encontrado en ruta por defecto" }

Write-Host "`n=== Unity ===" -ForegroundColor Cyan
$hub = @("C:\Program Files\Unity Hub\Unity Hub.exe","$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe","D:\Program Files\Unity Hub\Unity Hub.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $hub) { $hub = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*","HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" -ErrorAction SilentlyContinue | Where-Object { $_.DisplayName -like 'Unity Hub*' } | Select-Object -First 1).InstallLocation }
if (-not $hub) { $proc = Get-Process "Unity Hub" -ErrorAction SilentlyContinue | Select-Object -First 1; if ($proc) { $hub = $proc.Path } }
if (-not $hub -and (Test-Path (Join-Path $env:APPDATA "UnityHub"))) { $hub = "(configuracion encontrada en %APPDATA%\UnityHub)" }
if ($hub) { Pass "Unity Hub: $hub" } else { Miss "Unity Hub" }

$roots = @("C:\Program Files\Unity\Hub\Editor")
$sec = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
if (Test-Path $sec) { try { $p = Get-Content $sec -Raw | ConvertFrom-Json } catch { $p = $null }; if ($p) { $roots += [string]$p } }
$editors = @()
foreach ($r in $roots | Select-Object -Unique) {
    if (Test-Path $r) { $editors += Get-ChildItem $r -Directory | Where-Object { Test-Path (Join-Path $_.FullName "Editor\Unity.exe") } }
}
$adb = $null
if ($editors.Count -eq 0) { Miss "Ningun editor de Unity instalado" }
foreach ($e in $editors) {
    Pass "Unity $($e.Name) en $($e.FullName)"
    $ap = Join-Path $e.FullName "Editor\Data\PlaybackEngines\AndroidPlayer"
    if (Test-Path $ap) { Pass "  Android Build Support" } else { Miss "  Android Build Support (modulo)"; continue }
    if (Test-Path "$ap\OpenJDK") { Pass "  OpenJDK" } else { Miss "  OpenJDK (submodulo)" }
    if (Test-Path "$ap\SDK")     { Pass "  Android SDK" } else { Miss "  Android SDK (submodulo)" }
    if (Test-Path "$ap\NDK")     { Pass "  Android NDK" } else { Miss "  Android NDK (submodulo)" }
    $cand = "$ap\SDK\platform-tools\adb.exe"; if (Test-Path $cand) { $adb = $cand }
}

Write-Host "`n=== Dispositivo Android ===" -ForegroundColor Cyan
if (-not $adb) { $c = Get-Command adb -ErrorAction SilentlyContinue; if ($c) { $adb = $c.Source } }
if ($adb) {
    $devs = & $adb devices 2>$null | Select-Object -Skip 1 | Where-Object { $_ -match "\S" }
    $ready = $devs | Where-Object { $_ -match "\tdevice$" }
    if ($ready) { Pass "Telefono conectado: $($ready -join ', ')" }
    elseif ($devs) { Miss "Telefono detectado pero no autorizado: acepta la huella en la pantalla del telefono"; Info ($devs -join ' | ') }
    else { Miss "Ningun telefono conectado por USB (o depuracion USB apagada)" }
} else { Miss "adb no encontrado (se instala con Android SDK de Unity)" }

Write-Host "`n=== Disco ===" -ForegroundColor Cyan
foreach ($d in @('C','D')) {
    $drv = Get-PSDrive $d -ErrorAction SilentlyContinue
    if ($drv) {
        $gb = [math]::Round($drv.Free / 1GB, 1)
        if ($gb -ge 25) { Pass "${d}: $gb GB libres" } else { Miss "${d}: solo $gb GB libres (recomendado 25+)" }
    }
}

Write-Host "`nResultado: $ok OK, $fail FALTA" -ForegroundColor ($(if ($fail -eq 0) { 'Green' } else { 'Yellow' }))
