# Crea un juego nuevo desde templates\android-2d.
# Uso:
#   powershell -ExecutionPolicy Bypass -File tools\new-game.ps1 -Name RescatePapi -Title "Rescate Papi" -Orientation landscape
# -Name: identificador sin espacios en PascalCase (carpeta, asmdefs, namespace).
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Title,
    [string]$AppId,
    [ValidateSet("landscape", "portrait")][string]$Orientation = "landscape",
    [string]$Company = "Omar Olvera"
)
. "$PSScriptRoot\_common.ps1"

if ($Name -notmatch '^[A-Z][A-Za-z0-9]{2,40}$') { Fail "-Name debe ser PascalCase sin espacios ni acentos (ej. RescatePapi)" }
if (-not $Title) { $Title = ($Name -creplace '([a-z0-9])([A-Z])', '$1 $2') }
if (-not $AppId) { $AppId = "com.omarolvera." + $Name.ToLower() }
if ($AppId -notmatch '^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$') { Fail "-AppId invalido: $AppId (ej. com.omarolvera.rescatepapi)" }

$template = Join-Path $Script:RepoRoot "templates\android-2d"
$dest = Join-Path $Script:RepoRoot "games\$Name"
if (-not (Test-Path $template)) { Fail "No existe la plantilla: $template" }
if (Test-Path $dest) { Fail "Ya existe: $dest" }

Step "Creando $Title ($Name) en games\$Name"
Copy-Item $template $dest -Recurse
Remove-Item (Join-Path $dest "README.md") -ErrorAction SilentlyContinue   # README es de la plantilla

# 1) Renombrar archivos y carpetas que contienen __GAME__ (de lo mas profundo a lo mas alto)
Get-ChildItem $dest -Recurse | Where-Object { $_.Name -like '*__GAME__*' } |
    Sort-Object { $_.FullName.Length } -Descending |
    ForEach-Object { Rename-Item $_.FullName ($_.Name -replace '__GAME__', $Name) }

# 2) Reemplazar tokens en archivos de texto (UTF-8 sin BOM)
$tokens = @{
    '__GAME__' = $Name; '__TITLE__' = $Title; '__APPID__' = $AppId
    '__COMPANY__' = $Company; '__ORIENTATION__' = $Orientation
    '__PRODUCTGUID__' = [guid]::NewGuid().ToString('N')
}
$utf8 = New-Object System.Text.UTF8Encoding $false
$textExt = @('.cs', '.asmdef', '.json', '.md', '.asset', '.txt')
Get-ChildItem $dest -Recurse -File | Where-Object { $textExt -contains $_.Extension } | ForEach-Object {
    $t = [System.IO.File]::ReadAllText($_.FullName)
    $orig = $t
    foreach ($k in $tokens.Keys) { $t = $t.Replace($k, $tokens[$k]) }
    if ($t -ne $orig) { [System.IO.File]::WriteAllText($_.FullName, $t, $utf8) }
}

# 3) Orientacion inicial en ProjectSettings (el build la vuelve a aplicar desde factory.json)
$ps = Join-Path $dest "ProjectSettings\ProjectSettings.asset"
$t = [System.IO.File]::ReadAllText($ps)
$p = if ($Orientation -eq "portrait") { 1 } else { 0 }
$l = 1 - $p
$t = $t -replace 'allowedAutorotateToPortrait: \d', "allowedAutorotateToPortrait: $p"
$t = $t -replace 'allowedAutorotateToPortraitUpsideDown: \d', "allowedAutorotateToPortraitUpsideDown: $p"
$t = $t -replace 'allowedAutorotateToLandscapeRight: \d', "allowedAutorotateToLandscapeRight: $l"
$t = $t -replace 'allowedAutorotateToLandscapeLeft: \d', "allowedAutorotateToLandscapeLeft: $l"
[System.IO.File]::WriteAllText($ps, $t, $utf8)

# 4) Verificacion: no deben quedar tokens sin reemplazar
$left = Get-ChildItem $dest -Recurse -File | Where-Object { $textExt -contains $_.Extension } |
    Select-String -Pattern '__(GAME|TITLE|APPID|COMPANY|ORIENTATION|PRODUCTGUID)__' -List
if ($left) { Fail "Quedaron tokens sin reemplazar en: $(($left.Path) -join ', ')" }

Write-Host "`n[OK] Juego creado: games\$Name" -ForegroundColor Green
Write-Host "     Titulo: $Title | appId: $AppId | orientacion: $Orientation"
Write-Host "`nSiguientes pasos:"
Write-Host "  1. Unity Hub -> Proyectos -> Agregar -> Agregar proyecto desde disco -> games\$Name (abrir una vez y cerrar)"
Write-Host "  2. powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Game $Name"
Write-Host "  3. powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Game $Name"
Write-Host "  4. powershell -ExecutionPolicy Bypass -File tools\install-apk.ps1 -Game $Name"
