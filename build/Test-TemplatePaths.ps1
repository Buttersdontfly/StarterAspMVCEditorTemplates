#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates that every path in template.json points at something that exists.

.DESCRIPTION
    The highest-value cheap check in the repo. A mistyped exclude path does NOT
    fail generation -- the template engine simply excludes nothing -- so an
    Identity-only file silently ships in the --auth none combo and the build
    breaks somewhere unrelated. Renaming or moving a file without updating
    template.json has exactly the same effect.

    Exits 1 on any broken path.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$content = Join-Path $repo 'src/StarterAspMVCEditorTemplates.Templates/content/StarterAspMVCEditorTemplates'
$templateJson = Join-Path $content '.template.config/template.json'

# Paths legitimately absent until a generation step has been run.
$knownPending = @{
    'src/StarterAspMVCEditorTemplates/Migrations/**' = 'run build/Generate-Migrations.ps1'
}

$template = Get-Content $templateJson -Raw | ConvertFrom-Json

$patterns = [System.Collections.Generic.List[object]]::new()
foreach ($source in $template.sources) {
    foreach ($modifier in $source.modifiers) {
        foreach ($p in $modifier.exclude) { $patterns.Add(@{ Path = $p; Kind = 'exclude' }) }
        foreach ($p in $modifier.include) { $patterns.Add(@{ Path = $p; Kind = 'include' }) }
    }
}
foreach ($output in $template.primaryOutputs) {
    $patterns.Add(@{ Path = $output.path; Kind = 'primaryOutput' })
}

$failures = @()
$pending = @()

foreach ($entry in $patterns) {
    $full = Join-Path $content $entry.Path
    $hits = @(Get-ChildItem -Path $full -File -Recurse -ErrorAction SilentlyContinue)

    if ($hits.Count -gt 0) { continue }

    if ($knownPending.ContainsKey($entry.Path)) {
        $pending += "$($entry.Path) ($($knownPending[$entry.Path]))"
    }
    else {
        $failures += "$($entry.Kind): $($entry.Path)"
    }
}

foreach ($item in $pending) {
    Write-Host "  MISSING  $item" -ForegroundColor Yellow
}
if ($pending.Count -gt 0) {
    Write-Host ''
    Write-Host 'Generated content is missing. The template will pack and install, but the app it produces will fail at runtime. Run ./build/Initialize-Repo.ps1' -ForegroundColor Yellow
    Write-Host ''
}

foreach ($item in $failures) {
    Write-Host "::error::template.json path matches no files -- $item" -ForegroundColor Red
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "$($failures.Count) broken path(s) in template.json. A bad exclude does not fail generation, it silently excludes nothing, so this must be fixed." -ForegroundColor Red
    exit 1
}

Write-Host "All $($patterns.Count - $pending.Count) template.json paths resolve." -ForegroundColor Green
