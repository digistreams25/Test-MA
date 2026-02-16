# PowerShell script to find AecPropertyDataMgd.dll and AecBaseMgd.dll

Write-Host "Searching for AEC DLLs in AutoCAD 2024 installation..." -ForegroundColor Cyan
Write-Host ""

$paths = @(
    "C:\Program Files\Autodesk\AutoCAD 2024\AecPropertyDataMgd.dll",
    "C:\Program Files\Autodesk\AutoCAD 2024\C3D\AecPropertyDataMgd.dll",
    "C:\Program Files\Autodesk\AutoCAD 2024\AecBaseMgd.dll",
    "C:\Program Files\Autodesk\AutoCAD 2024\C3D\AecBaseMgd.dll"
)

$found = @()
$notFound = @()

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Host "✓ FOUND: $path" -ForegroundColor Green
        $found += $path
    } else {
        Write-Host "✗ NOT FOUND: $path" -ForegroundColor Red
        $notFound += $path
    }
}

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Yellow
Write-Host "Found: $($found.Count) files" -ForegroundColor Green
Write-Host "Not Found: $($notFound.Count) files" -ForegroundColor Red

if ($found.Count -gt 0) {
    Write-Host ""
    Write-Host "=== CORRECT PATHS TO USE ===" -ForegroundColor Cyan
    foreach ($f in $found) {
        Write-Host $f -ForegroundColor White
    }
}

# Also do a recursive search in Autodesk folder
Write-Host ""
Write-Host "Performing deep search (this may take a moment)..." -ForegroundColor Cyan
$deepSearch = Get-ChildItem "C:\Program Files\Autodesk\" -Recurse -Filter "AecPropertyDataMgd.dll" -ErrorAction SilentlyContinue
if ($deepSearch) {
    Write-Host ""
    Write-Host "=== DEEP SEARCH RESULTS ===" -ForegroundColor Yellow
    $deepSearch | ForEach-Object { Write-Host $_.FullName -ForegroundColor Magenta }
}
