#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Restores the generated artifacts that are not committed, then verifies the repo.

.DESCRIPTION
    Two parts of the template are GENERATED rather than hand-written:

      wwwroot/lib/   vendored Bootstrap and jQuery  -> Get-VendorAssets.ps1
      Migrations/    EF Core migrations             -> Generate-Migrations.ps1

    Neither is present in a fresh clone, and neither survives replacing the repo
    with a source archive. Without them the template still packs and installs
    cleanly -- it just produces an app with no styling and no database tables,
    which fails at runtime with "no such table: AspNetRoles" rather than
    anywhere near the actual cause.

    Run this after cloning, after replacing the repo contents, and any time the
    generated app misbehaves in a way that smells like missing scaffolding.

.PARAMETER SkipAssets
    Skip vendoring. Useful offline: this is the only step that needs the network.
#>
[CmdletBinding()]
param(
    [switch]$SkipAssets
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$content = Join-Path $repo 'src/StarterAspMVCEditorTemplates.Templates/content/StarterAspMVCEditorTemplates/src/StarterAspMVCEditorTemplates'

Write-Host 'Checking build scripts parse...' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Test-PowerShellSyntax.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Build scripts have syntax errors. Fix those first.' }

# --- Vendored client-side assets -----------------------------------------
$bootstrap = Join-Path $content 'wwwroot/lib/bootstrap/css/bootstrap.min.css'
if ($SkipAssets) {
    Write-Host 'Skipping vendored assets (-SkipAssets).' -ForegroundColor DarkYellow
}
elseif (Test-Path $bootstrap) {
    Write-Host 'Vendored assets present.' -ForegroundColor Green
}
else {
    Write-Host 'Vendored assets missing. Downloading...' -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Get-VendorAssets.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Vendoring failed.' }
}

# --- EF Core migrations ---------------------------------------------------
$migrations = Join-Path $content 'Migrations'
if ((Test-Path $migrations) -and (Get-ChildItem $migrations -Filter *.cs -ErrorAction SilentlyContinue)) {
    Write-Host 'Migrations present.' -ForegroundColor Green
}
else {
    Write-Host 'Migrations missing. Generating...' -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Generate-Migrations.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Migration generation failed.' }
}

Write-Host ''
Write-Host 'Repository is ready.' -ForegroundColor Green
Write-Host 'Next: ./build/Regen-Samples.ps1, then commit samples/.' -ForegroundColor DarkGray
