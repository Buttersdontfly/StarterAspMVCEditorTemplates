#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates samples/ from the current template.

.DESCRIPTION
    CI runs this and fails on any diff, so every template change shows up as a
    reviewable diff per combo instead of silently breaking generation for users.
    Run after any template change, then commit samples/.

    Note the normalisation step near the end. The template generates a random
    pepper and lookup key per project, which is deliberate and tested elsewhere.
    Left alone, those values differ on every run, so the golden-sample check
    would report a diff forever and could never be satisfied by regenerating and
    committing. The two requirements are in direct conflict, and this is where
    that conflict is resolved: the random values are replaced with fixed
    placeholders so the samples stay byte-for-byte reproducible.

    Only the values change. If a future edit alters the STRUCTURE of
    appsettings.Development.json -- renames a section, drops a key -- that still
    shows up as a diff, which is the coverage worth keeping.
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

    # Replace the per-project random secrets with fixed placeholders. See the
    # note in the description for why this is necessary rather than a cheat.
    $normalised = 0
    Get-ChildItem ./samples -Recurse -Filter 'appsettings.Development.json' | ForEach-Object {
        $text = Get-Content $_.FullName -Raw
        $original = $text

        # Matched on the surrounding key rather than on the value's shape, so a
        # change to how the values are generated does not quietly stop this
        # working and reintroduce the endless diff.
        $text = [regex]::Replace($text,
            '("Value"\s*:\s*")[^"]*(")',
            '${1}SAMPLE-PEPPER-NOT-A-SECRET${2}')
        $text = [regex]::Replace($text,
            '("v1"\s*:\s*")[^"]*(")',
            '${1}SAMPLE-LOOKUP-KEY-NOT-A-SECRET${2}')

        if ($text -ne $original) {
            Set-Content -Path $_.FullName -Value $text -NoNewline
            $normalised++
        }
    }

    Write-Host "samples/ regenerated. Normalised secrets in $normalised file(s)." -ForegroundColor Green
}
finally {
    Pop-Location
}
