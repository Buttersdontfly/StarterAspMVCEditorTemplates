#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Packs the template and installs it locally, from a clean slate.

.DESCRIPTION
    Shared by the other build scripts. Does three things that all matter:

    1. UNINSTALLS every existing copy first. `dotnet new install` can leave
       several registrations that share one template identity, and the engine
       then throws "Sequence contains more than one matching element" on any
       attempt to use it. Repeated installs of the same version, or installs
       from different paths, both get you there.

    2. Gives each pack a UNIQUE version. NuGet caches by id + version, so
       re-packing 0.0.0-alpha.0 after a template edit can install a stale
       package from the global packages folder -- you change a file, rerun, and
       silently get the old content. A timestamped version makes that
       impossible.

    3. Returns the path to the nupkg it installed.

.PARAMETER Quiet
    Suppress dotnet's own output.
#>
[CmdletBinding()]
param(
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$packageId = 'StarterAspMVCEditorTemplates'
$verbosity = if ($Quiet) { 'quiet' } else { 'minimal' }

Push-Location $repo
try {
    # --- 1. Remove every existing registration -------------------------------
    # The list can contain several entries for the same id, so loop until
    # `dotnet new uninstall` stops reporting it. Cap the attempts so a
    # misbehaving install cannot spin forever.
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        $installed = & dotnet new uninstall 2>&1 | Out-String
        if ($installed -notmatch [regex]::Escape($packageId)) { break }

        Write-Host "Removing a previous install of $packageId (attempt $attempt)..." -ForegroundColor DarkGray
        & dotnet new uninstall $packageId 2>&1 | Out-Null

        if ($attempt -eq 10) {
            Write-Warning "Could not fully uninstall $packageId. Run 'dotnet new uninstall' to see what remains."
        }
    }

    # --- 2. Pack with a unique version --------------------------------------
    # Validate XML first: a malformed .props fails the pack with an error that
    # points at Microsoft.Common.props instead of at the file you broke.
    $global:LASTEXITCODE = 0
    & (Join-Path $PSScriptRoot 'Test-XmlWellFormed.ps1')
    if ($LASTEXITCODE -ne 0) {
        throw 'Malformed XML in the repo. Fix the files listed above before packing.'
    }

    # Syntax-check the C# too. Catching it here beats finding out after pack,
    # install, generate and build.
    $global:LASTEXITCODE = 0
    & (Join-Path $PSScriptRoot 'Test-CSharpSyntax.ps1')
    if ($LASTEXITCODE -ne 0) {
        throw 'Syntax errors in template source. Fix the files listed above before packing.'
    }

    # MinVer derives the real version from git tags. That is right for a
    # release, but for a local dev loop it produces the same 0.0.0-alpha.0 every
    # time (and warns MINVER1001 if the repo has no git history at all), which
    # is exactly the stale-cache trap above. Override it here only.
    $devVersion = "0.0.0-dev.$(Get-Date -Format 'yyyyMMddHHmmss')"

    Remove-Item (Join-Path $repo 'artifacts') -Recurse -Force -ErrorAction SilentlyContinue

    & dotnet pack src/StarterAspMVCEditorTemplates.Templates `
        -o ./artifacts `
        -v $verbosity `
        -p:MinVerVersionOverride=$devVersion
    if ($LASTEXITCODE -ne 0) {
        throw 'Pack failed. The dotnet errors are above; re-run with -v detailed if they are not specific enough.'
    }

    $nupkg = Get-ChildItem (Join-Path $repo 'artifacts') -Filter '*.nupkg' |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $nupkg) { throw 'Pack produced no .nupkg.' }

    # --- 3. Install ----------------------------------------------------------
    & dotnet new install $nupkg.FullName --force
    if ($LASTEXITCODE -ne 0) { throw "Template install failed for $($nupkg.Name)." }

    # The path is published through an environment variable rather than returned
    # on the pipeline. Returning it meant callers wrote `| Out-Null` to discard
    # it -- which also discarded every line of dotnet's output, so a failed pack
    # reported "Pack failed." with no diagnostics at all.
    $env:TEMPLATE_NUPKG = $nupkg.FullName
    Write-Host "Installed $($nupkg.Name)" -ForegroundColor Green
}
finally {
    Pop-Location
}
