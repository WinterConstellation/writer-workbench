param(
    [switch]$Delete
)

$ErrorActionPreference = "Stop"

$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$distRoot = Join-Path $workspaceRoot "dist"
$tempRoot = [System.IO.Path]::GetTempPath().TrimEnd('\')

$keepDist = @(
    (Join-Path $distRoot "WriterWorkbench-html-workbench")
)

$candidatePaths = @(
    (Join-Path $distRoot "WriterWorkbench"),
    (Join-Path $distRoot "WriterWorkbench-html-workbench-next"),
    (Join-Path $distRoot "WriterWorkbench-menu-remote"),
    (Join-Path $distRoot "WriterWorkbench-relationship-map"),
    (Join-Path $distRoot "WriterWorkbench-win-x64"),
    (Join-Path $distRoot "WriterWorkbench-win-x64-p0input"),
    (Join-Path $tempRoot "WriterWorkbenchLargeTests"),
    (Join-Path $tempRoot "WriterWorkbenchPresetTests"),
    (Join-Path $tempRoot "WriterWorkbenchSessionTests"),
    (Join-Path $tempRoot "WriterWorkbenchShortcutTests"),
    (Join-Path $tempRoot "WriterWorkbenchTests")
)

function Get-DirectorySize {
    param([Parameter(Mandatory)][string]$Path)

    $items = Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
    $size = ($items | Measure-Object Length -Sum).Sum
    [pscustomobject]@{
        Path = $Path
        MB = [math]::Round(($size / 1MB), 2)
        Files = ($items | Where-Object { -not $_.PSIsContainer }).Count
        Dirs = ($items | Where-Object { $_.PSIsContainer }).Count
        LastWrite = (Get-Item -LiteralPath $Path).LastWriteTime
    }
}

function Assert-SafeCandidate {
    param([Parameter(Mandatory)][string]$Path)

    $full = [System.IO.Path]::GetFullPath($Path)
    $underDist = $full.StartsWith($distRoot, [System.StringComparison]::OrdinalIgnoreCase)
    $underTemp = $full.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)
    if (-not ($underDist -or $underTemp)) {
        throw "Refusing to touch path outside dist/temp: $full"
    }

    foreach ($keep in $keepDist) {
        $keepFull = [System.IO.Path]::GetFullPath($keep)
        if ($full.Equals($keepFull, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to touch kept dist artifact: $full"
        }
    }

    return $full
}

$existing = foreach ($path in $candidatePaths) {
    if (Test-Path -LiteralPath $path) {
        $safe = Assert-SafeCandidate -Path $path
        Get-DirectorySize -Path $safe
    }
}

if (-not $existing) {
    Write-Host "No cleanup candidates found."
    exit 0
}

$existing | Sort-Object Path | Format-Table -AutoSize

if (-not $Delete) {
    Write-Host ""
    Write-Host "Dry run only. Re-run with -Delete to remove the listed candidates."
    exit 0
}

foreach ($item in $existing) {
    $safe = Assert-SafeCandidate -Path $item.Path
    Remove-Item -LiteralPath $safe -Recurse -Force
    Write-Host "Deleted: $safe"
}
