# Funciones compartidas por los scripts de tools/. Se carga con:  . "$PSScriptRoot\_common.ps1"

$Script:RepoRoot = Split-Path $PSScriptRoot -Parent

function Fail($m) { Write-Host "[ERROR] $m" -ForegroundColor Red; exit 1 }
function Step($m) { Write-Host "`n>> $m" -ForegroundColor Cyan }

function Get-GameProject([string]$Game) {
    $proj = Join-Path $Script:RepoRoot "games\$Game"
    if (-not (Test-Path "$proj\ProjectSettings\ProjectVersion.txt")) { Fail "No existe el proyecto Unity: $proj" }
    return $proj
}

function Get-FactoryConfig([string]$Project) {
    $f = Join-Path $Project "factory.json"
    if (-not (Test-Path $f)) { Fail "Falta $f" }
    return Get-Content $f -Raw | ConvertFrom-Json
}

function Get-UnityRoots {
    $roots = @("C:\Program Files\Unity\Hub\Editor")
    $sec = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $sec) { try { $p = Get-Content $sec -Raw | ConvertFrom-Json; if ($p) { $roots += [string]$p } } catch {} }
    return $roots | Select-Object -Unique
}

# Devuelve la carpeta del editor (la que contiene Editor\Unity.exe) para la version que pide el proyecto
function Get-UnityEditorDir([string]$Project) {
    $line = Get-Content "$Project\ProjectSettings\ProjectVersion.txt" | Where-Object { $_ -like "m_EditorVersion:*" } | Select-Object -First 1
    $ver = ($line -split ":", 2)[1].Trim()
    foreach ($r in Get-UnityRoots) {
        $d = Join-Path $r $ver
        if (Test-Path "$d\Editor\Unity.exe") { return $d }
    }
    Fail "No encontre Unity $ver instalado (lo pide $Project). Instalalo desde Unity Hub."
}

function Get-Adb([string]$EditorDir) {
    $adb = "$EditorDir\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
    if (Test-Path $adb) { return $adb }
    $c = Get-Command adb -ErrorAction SilentlyContinue
    if ($c) { return $c.Source }
    Fail "No encontre adb. Revisa el modulo Android SDK de Unity."
}

function Assert-UnityClosed([string]$Project) {
    $norm = { param($x) ($x -replace '/', '\').TrimEnd('\').ToLower() }
    $full = & $norm (Resolve-Path $Project).Path
    $open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and (& $norm $_.CommandLine).Contains($full) }
    if ($open) { Fail "El proyecto esta abierto en el editor de Unity. Cierralo y vuelve a intentar." }
    if (Test-Path (Join-Path $Project "Temp\UnityLockfile")) {
        try { $fs = [System.IO.File]::Open((Join-Path $Project "Temp\UnityLockfile"), 'Open', 'ReadWrite', 'None'); $fs.Close() }
        catch { Fail "Unity tiene el proyecto bloqueado (Temp\UnityLockfile). Cierra Unity por completo y reintenta." }
    }
    # Con el editor cerrado, Unity a veces deja procesos auxiliares que bloquean Library\PackageCache
    if (-not (Get-Process Unity -ErrorAction SilentlyContinue)) {
        $leftovers = Get-Process -ErrorAction SilentlyContinue | Where-Object { $_.ProcessName -like 'Unity.ILPP*' -or $_.ProcessName -like 'bee_backend*' -or $_.ProcessName -like 'UnityShaderCompiler*' }
        if ($leftovers) {
            Write-Host "   Cerrando procesos auxiliares de Unity que quedaron abiertos: $(($leftovers.ProcessName | Select-Object -Unique) -join ', ')"
            $leftovers | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
        }
    }
    $code = Get-Process Code -ErrorAction SilentlyContinue
    if ($code) { Write-Host "[AVISO] VS Code esta abierto: si falla con 'EPERM' o 'operation not permitted', cierralo y reintenta." -ForegroundColor Yellow }
}

# Ejecuta Unity en modo batch y espera; devuelve el codigo de salida
function Invoke-UnityBatch([string]$EditorDir, [string[]]$UnityArgs) {
    $exe = "$EditorDir\Editor\Unity.exe"
    $quoted = $UnityArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    $p = Start-Process -FilePath $exe -ArgumentList ($quoted -join ' ') -Wait -PassThru -NoNewWindow
    return $p.ExitCode
}

# Si el log de Unity muestra un bloqueo de archivos, explica la causa probable
function Explain-UnityLog([string]$Log) {
    if (-not (Test-Path $Log)) { return }
    if (Select-String -Path $Log -Pattern 'EPERM|operation not permitted' -Quiet) {
        Write-Host "[CAUSA] Un programa tiene bloqueada la cache de Unity (Library\PackageCache)." -ForegroundColor Yellow
        Write-Host "        Casi siempre es VS Code (su extension de C#). Cierra VS Code (Archivo -> Salir) y Unity, y reintenta." -ForegroundColor Yellow
    }
    $cs = Select-String -Path $Log -Pattern 'error CS\d+' | Select-Object -First 5
    if ($cs) {
        Write-Host "[CAUSA] Errores de compilacion:" -ForegroundColor Yellow
        $cs | ForEach-Object { Write-Host "        $($_.Line.Trim())" }
    }
}
