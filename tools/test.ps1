# Ejecuta las pruebas (Core + juego) en modo batch.
# Uso: powershell -ExecutionPolicy Bypass -File tools\test.ps1 [-Game RescatePapi | -All] [-Platform EditMode|PlayMode] [-Quiet]
param(
    [string]$Game = "RescatePapi",
    [switch]$All,
    [ValidateSet("EditMode", "PlayMode")][string]$Platform = "EditMode",
    [switch]$Quiet
)
. "$PSScriptRoot\_common.ps1"

function Invoke-GameTests([string]$G) {
    $proj = Get-GameProject $G
    $editor = Get-UnityEditorDir $proj
    Assert-UnityClosed $proj

    $outDir = Join-Path $Script:RepoRoot "builds\$G"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    $results = Join-Path $outDir "tests-$Platform.xml"
    $log = Join-Path $outDir "tests-$Platform.log"
    if (Test-Path $results) { Remove-Item $results -Force }

    Step "Pruebas $Platform de $G"
    $code = Invoke-UnityBatch $editor @(
        "-batchmode", "-projectPath", $proj, "-buildTarget", "Android",
        "-runTests", "-testPlatform", $Platform, "-testResults", $results, "-logFile", $log
    )
    if (-not (Test-Path $results)) {
        Write-Host "[ERROR] $G : no se generaron resultados (codigo $code). Revisa $log" -ForegroundColor Red
        Explain-UnityLog $log
        return $false
    }
    [xml]$x = Get-Content $results
    $run = $x.'test-run'
    Write-Host ("  Total: {0}  OK: {1}  Fallidas: {2}  Omitidas: {3}" -f $run.total, $run.passed, $run.failed, $run.skipped)
    if ([int]$run.failed -gt 0) {
        $x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            Write-Host "  [FALLA] $($_.fullname)" -ForegroundColor Red
            Write-Host "          $(($_.SelectSingleNode("failure/message")).InnerText.Trim())"
        }
        return $false
    }
    if ($code -ne 0) { Write-Host "[ERROR] $G : Unity termino con codigo $code. Revisa $log" -ForegroundColor Red; return $false }
    Write-Host "  [OK] $G" -ForegroundColor Green
    return $true
}

$games = if ($All) {
    Get-ChildItem (Join-Path $Script:RepoRoot "games") -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName "ProjectSettings\ProjectVersion.txt") } |
        ForEach-Object { $_.Name }
} else { @($Game) }

$failed = @()
foreach ($g in $games) { if (-not (Invoke-GameTests $g)) { $failed += $g } }

if ($failed.Count -gt 0) { Fail "Pruebas fallidas en: $($failed -join ', ')" }
Write-Host "`n[OK] Todas las pruebas pasaron ($($games -join ', '))" -ForegroundColor Green
