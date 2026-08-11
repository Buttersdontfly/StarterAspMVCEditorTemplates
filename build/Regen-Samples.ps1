#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates samples/ from the current template.

.DESCRIPTION
    CI runs this and fails on any diff, so every template change shows up as a
    reviewable diff per combo instead of silently breaking generation for users.
    Run after any template change, then commit samples/.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo

try {
    Remove-Item ./samples/identity, ./samples/none -Recurse -Force -ErrorAction SilentlyContinue

    # No | Out-Null here: it would swallow dotnet's output, including errors.
    & (Join-Path $PSScriptRoot 'Install-TemplateLocally.ps1')

    dotnet new starterasp-mvc -n SampleIdentityApp -o ./samples/identity --auth identity --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Identity combo failed to generate.' }

    dotnet new starterasp-mvc -n SamplePlainApp -o ./samples/none --auth none --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'None combo failed to generate.' }

    # Keep the diff signal clean.
    Get-ChildItem ./samples -Recurse -Force |
        Where-Object { $_.Name -in 'bin', 'obj', 'packages.lock.json' } |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Write-Host 'samples/ regenerated.' -ForegroundColor Green
}
finally {
    Pop-Location
}
