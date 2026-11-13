[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$SourceRoot
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path -LiteralPath $ProjectRoot).ProviderPath
$manifestPath = Join-Path $PSScriptRoot '../Docs/automation-reports/evidence/daily-freeze-2026-11-12/local-asset-sha256.json'
$manifest = @(Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json)
$prefix = '^Assets/(?:Free HDR Skyboxes Pack(?:/|\.meta$)|RainMaker(?:/|\.meta$)|Resources/(?:WorldClimateVendor|WorldMapMusic|IntegratedAmbience)(?:/|\.meta$))'
if (-not (Test-Path -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -PathType Leaf)) {
    throw 'ProjectRoot must be the root of the Unity checkout.'
}

function Get-CheckedPath([string]$Root, [string]$Relative) {
    $file = [IO.FileInfo]::new((Join-Path $Root $Relative))
    if ($file.Exists -and ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing a linked file: $($file.FullName)"
    }
    for ($dir = $file.Directory; $null -ne $dir; $dir = $dir.Parent) {
        if ($dir.Exists -and ($dir.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing a linked directory: $($dir.FullName)"
        }
    }
    return $file
}

function Test-Asset([IO.FileInfo]$File, $Entry) {
    return $File.Exists -and $File.Length -eq $Entry.bytes -and
        (Get-FileHash -LiteralPath $File.FullName -Algorithm SHA256).Hash -eq $Entry.sha256
}

$paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $manifest) {
    if ($entry.path -notmatch $prefix -or $entry.path -match '(^|/)\.\.?(/|$)|//|[\\:]' -or
        $entry.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $entry.bytes -lt 0 -or -not $paths.Add($entry.path)) {
        throw "Invalid or duplicate manifest entry: $($entry.path)"
    }
}
if ($paths.Count -eq 0) { throw 'Asset manifest is empty.' }

$pending = @()
$different = @()
foreach ($entry in $manifest) {
    $target = Get-CheckedPath $project $entry.path
    if (-not $target.Exists) { $pending += $entry }
    elseif (-not (Test-Asset $target $entry)) { $different += $entry.path }
}
if ($different.Count) {
    $different | ForEach-Object { Write-Warning "Differs: $_" }
    throw "$($different.Count) existing file(s) differ from RC2. Keep your files and use a separate checkout for exact reproduction."
}
if ($pending.Count -and -not $SourceRoot) {
    $pending | Group-Object { $_.path -replace '^((?:Assets/Resources/[^/]+|Assets/[^/]+?))(?:\.meta)?(?:/.*)?$', '$1' } |
        ForEach-Object { Write-Warning ('Missing {0}: {1} file(s)' -f $_.Name, $_.Count) }
    throw "$($pending.Count) of $($paths.Count) RC2 asset files are missing. Use -SourceRoot with an authorized copy of the original project or the local asset bundle."
}
if ($pending.Count) {
    $source = (Resolve-Path -LiteralPath $SourceRoot).ProviderPath
    $inputs = foreach ($entry in $pending) {
        $file = Get-CheckedPath $source $entry.path
        if (-not (Test-Asset $file $entry)) { throw "Source is missing or differs from RC2: $($entry.path)" }
        $file
    }
    for ($i = 0; $i -lt $pending.Count; $i++) {
        $target = Get-CheckedPath $project $pending[$i].path
        [IO.Directory]::CreateDirectory($target.DirectoryName) | Out-Null
        [IO.File]::Copy($inputs[$i].FullName, $target.FullName, $false)
        if (-not (Test-Asset ([IO.FileInfo]::new($target.FullName)) $pending[$i])) {
            throw "Copied file failed verification: $($pending[$i].path)"
        }
    }
}
Write-Output "Verified $($paths.Count) RC2 asset files. Restored $($pending.Count)."
