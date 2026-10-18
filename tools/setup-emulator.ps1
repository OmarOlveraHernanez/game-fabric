# Game Factory - instala y crea un emulador Android usando el SDK que trae Unity.
# Todo queda en D: (imagen, emulador y AVD). No requiere Android Studio.
# Uso:
#   powershell -ExecutionPolicy Bypass -File tools\setup-emulator.ps1          (instalar y crear)
#   powershell -ExecutionPolicy Bypass -File tools\setup-emulator.ps1 -Start   (solo abrir el emulador)

param([switch]$Start)
$ErrorActionPreference = 'Stop'
$AvdName  = 'GameFactory'
$Image    = 'system-images;android-35;google_apis;x86_64'
$AvdHome  = 'D:\Unity\Cache\avd'

function Step($m) { Write-Host "`n>> $m" -ForegroundColor Cyan }
function Fail($m) { Write-Host "[ERROR] $m" -ForegroundColor Red; exit 1 }

# --- Localizar el SDK de Android dentro de Unity ---
$roots = @("C:\Program Files\Unity\Hub\Editor")
$sec = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
if (Test-Path $sec) { try { $p = Get-Content $sec -Raw | ConvertFrom-Json; if ($p) { $roots += [string]$p } } catch {} }
$ap = $null
foreach ($r in $roots | Select-Object -Unique) {
    if (-not (Test-Path $r)) { continue }
    $cand = Get-ChildItem $r -Directory | Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "Editor\Data\PlaybackEngines\AndroidPlayer" } |
        Where-Object { Test-Path "$_\SDK" } | Select-Object -First 1
    if ($cand) { $ap = $cand; break }
}
if (-not $ap) { Fail "No encontre una version de Unity con Android SDK. Revisa los modulos en Unity Hub." }
$sdk = "$ap\SDK"
Write-Host "SDK de Android: $sdk"

$env:JAVA_HOME = "$ap\OpenJDK"
$env:ANDROID_HOME = $sdk
$env:ANDROID_SDK_ROOT = $sdk
$env:ANDROID_AVD_HOME = $AvdHome
$emu = "$sdk\emulator\emulator.exe"

if ($Start) {
    if (-not (Test-Path $emu)) { Fail "El emulador no esta instalado. Ejecuta el script sin -Start primero." }
    Step "Abriendo emulador $AvdName (la primera vez tarda 1-3 minutos)"
    Start-Process $emu -ArgumentList "-avd $AvdName -gpu auto -no-snapshot-save"
    exit 0
}

# --- Variables persistentes (para que Unity y el emulador las vean siempre) ---
Step "Guardando ANDROID_AVD_HOME=$AvdHome (para que los emuladores vivan en D:)"
New-Item -ItemType Directory -Force -Path $AvdHome | Out-Null
[Environment]::SetEnvironmentVariable("ANDROID_AVD_HOME", $AvdHome, "User")

# --- sdkmanager / avdmanager ---
$sdkm = Get-ChildItem "$sdk\cmdline-tools" -Recurse -Filter sdkmanager.bat -ErrorAction SilentlyContinue | Select-Object -First 1
$avdm = Get-ChildItem "$sdk\cmdline-tools" -Recurse -Filter avdmanager.bat -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $sdkm -or -not $avdm) { Fail "No encontre sdkmanager/avdmanager en $sdk\cmdline-tools" }
$sdkm = $sdkm.FullName; $avdm = $avdm.FullName

Step "Aceptando licencias de Android"
("y`n" * 30) | & $sdkm --sdk_root="$sdk" --licenses | Out-Null

Step "Descargando emulador e imagen Android 15 x86_64 (~2 GB, puede tardar)"
("y`n" * 5) | & $sdkm --sdk_root="$sdk" "emulator" "platform-tools" $Image
if ($LASTEXITCODE -ne 0) { Fail "sdkmanager fallo (codigo $LASTEXITCODE). Si dice 'Access denied', ejecuta PowerShell como administrador." }

Step "Revisando aceleracion por hardware"
& $emu -accel-check
if ($LASTEXITCODE -ne 0) {
    Write-Host @"

[AVISO] La aceleracion no esta disponible. Sin ella el emulador sera muy lento o no abrira.
Solucion:
  1. Windows -> 'Activar o desactivar las caracteristicas de Windows'
  2. Marca 'Plataforma de hipervisor de Windows' y reinicia.
  3. Si sigue fallando: activa la virtualizacion (Intel VT-x / AMD SVM) en la BIOS.
"@ -ForegroundColor Yellow
}

Step "Creando el dispositivo virtual '$AvdName' (Pixel 6)"
"no" | & $avdm create avd -n $AvdName -k $Image -d pixel_6 --force
if ($LASTEXITCODE -ne 0) { Fail "avdmanager no pudo crear el AVD." }

Write-Host "`n[OK] Emulador listo. Para abrirlo:" -ForegroundColor Green
Write-Host "     powershell -ExecutionPolicy Bypass -File tools\setup-emulator.ps1 -Start"
