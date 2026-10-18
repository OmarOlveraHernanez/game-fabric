# Genera el APK de desarrollo (Dev) o el AAB firmado de lanzamiento (Release).
# Uso: powershell -ExecutionPolicy Bypass -File tools\build.ps1 [-Game RescatePapi] [-Mode Dev|Release]
param(
    [string]$Game = "RescatePapi",
    [ValidateSet("Dev", "Release")][string]$Mode = "Dev"
)
. "$PSScriptRoot\_common.ps1"

$proj = Get-GameProject $Game
$cfg = Get-FactoryConfig $proj
$editor = Get-UnityEditorDir $proj
Assert-UnityClosed $proj

$name = ($cfg.productName -replace '[^A-Za-z0-9]', '')
$outDir = Join-Path $Script:RepoRoot "builds\$Game"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$ext = if ($Mode -eq "Dev") { "dev.apk" } else { "release.aab" }
$out = Join-Path $outDir "$name-$($cfg.version)-$ext"
$log = Join-Path $outDir "build-$Mode.log"
$method = if ($Mode -eq "Dev") { "BuildDevApk" } else { "BuildReleaseAab" }

Step "Build $Mode de $Game v$($cfg.version) con Unity $(Split-Path $editor -Leaf)"
Write-Host "   Log: $log  (la primera vez tarda varios minutos)"
$t = Get-Date
$code = Invoke-UnityBatch $editor @(
    "-batchmode", "-quit", "-projectPath", $proj, "-buildTarget", "Android",
    "-executeMethod", "GameFactory.Core.Editor.FactoryBuild.$method",
    "-buildOutput", $out, "-logFile", $log
)
$secs = [int]((Get-Date) - $t).TotalSeconds

if ($code -ne 0 -or -not (Test-Path $out)) {
    Write-Host "`n--- Ultimas lineas relevantes del log ---" -ForegroundColor Yellow
    if (Test-Path $log) { Select-String -Path $log -Pattern "error|exception|GameFactory|failed" -CaseSensitive:$false | Select-Object -Last 25 | ForEach-Object { $_.Line } }
    Fail "Build fallido (codigo $code, ${secs}s). Revisa $log"
}
$mb = [math]::Round((Get-Item $out).Length / 1MB, 1)
Write-Host "`n[OK] $out ($mb MB, ${secs}s)" -ForegroundColor Green
