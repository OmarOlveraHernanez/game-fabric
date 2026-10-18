# Ejecuta las pruebas EditMode (Core + juego) en modo batch.
# Uso: powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Game ChihuahuaJump [-Platform EditMode|PlayMode] [-Quiet]
param(
    [string]$Game = "ChihuahuaJump",
    [ValidateSet("EditMode", "PlayMode")][string]$Platform = "EditMode",
    [switch]$Quiet
)
. "$PSScriptRoot\_common.ps1"

$proj = Get-GameProject $Game
$editor = Get-UnityEditorDir $proj
Assert-UnityClosed $proj

$outDir = Join-Path $Script:RepoRoot "builds\$Game"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$results = Join-Path $outDir "tests-$Platform.xml"
$log = Join-Path $outDir "tests-$Platform.log"
if (Test-Path $results) { Remove-Item $results -Force }

if (-not $Quiet) { Step "Pruebas $Platform de $Game" }
$code = Invoke-UnityBatch $editor @(
    "-batchmode", "-projectPath", $proj, "-buildTarget", "Android",
    "-runTests", "-testPlatform", $Platform, "-testResults", $results, "-logFile", $log
)

if (-not (Test-Path $results)) { Fail "No se generaron resultados (codigo $code). Revisa $log" }
[xml]$x = Get-Content $results
$run = $x.'test-run'
Write-Host ("Total: {0}  OK: {1}  Fallidas: {2}  Omitidas: {3}" -f $run.total, $run.passed, $run.failed, $run.skipped)
if ([int]$run.failed -gt 0) {
    $x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
        Write-Host "  [FALLA] $($_.fullname)" -ForegroundColor Red
        Write-Host "          $(($_.SelectSingleNode("failure/message")).InnerText.Trim())"
    }
    exit 1
}
if ($code -ne 0) { Fail "Unity termino con codigo $code. Revisa $log" }
Write-Host "[OK] Todas las pruebas pasaron" -ForegroundColor Green
