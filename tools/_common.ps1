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
    $full = (Resolve-Path $Project).Path.TrimEnd('\')
    $open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine.ToLower().Contains($full.ToLower()) }
    if ($open) { Fail "El proyecto esta abierto en el editor de Unity. Cierralo y vuelve a intentar." }
}

# Ejecuta Unity en modo batch y espera; devuelve el codigo de salida
function Invoke-UnityBatch([string]$EditorDir, [string[]]$UnityArgs) {
    $exe = "$EditorDir\Editor\Unity.exe"
    $quoted = $UnityArgs | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } }
    $p = Start-Process -FilePath $exe -ArgumentList ($quoted -join ' ') -Wait -PassThru -NoNewWindow
    return $p.ExitCode
}
