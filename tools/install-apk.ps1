# Instala el APK de desarrollo mas reciente en el emulador/telefono conectado y lo abre.
# Uso: powershell -ExecutionPolicy Bypass -File tools\install-apk.ps1 [-Game RescatePapi]
param([string]$Game = "RescatePapi")
. "$PSScriptRoot\_common.ps1"

$proj = Get-GameProject $Game
$cfg = Get-FactoryConfig $proj
$adb = Get-Adb (Get-UnityEditorDir $proj)

$apk = Get-ChildItem (Join-Path $Script:RepoRoot "builds\$Game") -Filter "*-dev.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) { Fail "No hay APK. Ejecuta primero tools\build.ps1" }

function Get-ReadyDevices { & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" } }

$devs = Get-ReadyDevices
if (-not $devs) {
    $listed = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match "\S" }
    if (-not $listed) {
        Step "No hay dispositivo: abriendo el emulador GameFactory"
        & powershell -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\setup-emulator.ps1" -Start | Out-Null
    } else {
        Step "Dispositivo detectado pero aun no listo ($($listed -join ', '))"
    }
    Write-Host "   Esperando a que Android termine de encender (max. 4 min)..."
    $deadline = (Get-Date).AddMinutes(4)
    do {
        Start-Sleep -Seconds 5
        $devs = Get-ReadyDevices
        if ($devs) {
            $s = (($devs | Select-Object -First 1) -split "\t")[0]
            $boot = (& $adb -s $s shell getprop sys.boot_completed 2>$null) -join ''
            if ($boot.Trim() -ne '1') { $devs = $null }
        }
    } while (-not $devs -and (Get-Date) -lt $deadline)
    if (-not $devs) { Fail "El emulador no termino de encender. Abrelo con tools\setup-emulator.ps1 -Start, espera la pantalla de inicio y reintenta." }
}
$serial = (($devs | Select-Object -First 1) -split "\t")[0]

Step "Instalando $($apk.Name) en $serial"
& $adb -s $serial install -r $apk.FullName
if ($LASTEXITCODE -ne 0) { Fail "adb install fallo" }

Step "Abriendo $($cfg.appId)"
& $adb -s $serial shell monkey -p $cfg.appId -c android.intent.category.LAUNCHER 1 | Out-Null
Write-Host "`n[OK] Instalado y abierto. Logs en vivo:  & '$adb' logcat -s Unity" -ForegroundColor Green
