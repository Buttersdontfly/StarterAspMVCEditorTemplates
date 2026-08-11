#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates the EF migrations that ship inside the template.

.DESCRIPTION
    Migrations cannot be written by hand: EF derives them from the model, and the
    snapshot must match exactly or the next `migrations add` misbehaves. So this
    generates a throwaway project from the template, runs `dotnet ef migrations
    add` against it, and copies the result back into the template content.

    Run after ANY change to AppDbContext or the Identity configuration, then
    commit the result.

    Requires: .NET SDK, and dotnet-ef (installed automatically if missing).
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo

try {
    $content = 'src/StarterAspMVCEditorTemplates.Templates/content/StarterAspMVCEditorTemplates'
    $work = Join-Path ([System.IO.Path]::GetTempPath()) "mig-$(Get-Random)"

    if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
        Write-Host 'Installing dotnet-ef...' -ForegroundColor Cyan
        dotnet tool install --global dotnet-ef
        if ($LASTEXITCODE -ne 0) { throw 'Could not install dotnet-ef.' }
    }

    # No | Out-Null here: it would swallow dotnet's output, including errors.
    & (Join-Path $PSScriptRoot 'Install-TemplateLocally.ps1')

    # --no-restore, then restore explicitly. The post-creation restore inside
    # `dotnet new` reports failure through a generic 'Post action failed'
    # message that hides the real NuGet error, which makes diagnosis miserable.
    dotnet new starterasp-mvc -n MigrationHost -o $work --auth identity --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Template instantiation failed.' }

    dotnet restore (Join-Path $work 'src/MigrationHost/MigrationHost.csproj')
    if ($LASTEXITCODE -ne 0) {
        throw @'
Restore failed for the generated project.

If the errors are NU1901-NU1904, a NuGet audit warning is being treated as an
error. Audit findings must stay warnings in this repo -- check that
WarningsNotAsErrors in the generated Directory.Build.props still lists
NU1901;NU1902;NU1903;NU1904.
'@
    }

    # Build explicitly BEFORE calling dotnet ef. `dotnet ef` builds internally and
    # reports only "Build failed. Use dotnet build to see the errors." -- it
    # swallows the compiler output entirely, which makes any compile error in the
    # template a guessing game. Building here surfaces the real diagnostics.
    Write-Host 'Building the generated project...' -ForegroundColor Cyan
    dotnet build (Join-Path $work 'src/MigrationHost/MigrationHost.csproj')
    if ($LASTEXITCODE -ne 0) {
        throw @"
The generated project does not compile. The errors above are the real ones.

The generated project is still on disk so you can open and inspect it:
    $work

Fix the corresponding file under
    $content/src/StarterAspMVCEditorTemplates/
then re-run this script.
"@
    }

    dotnet ef migrations add Initial `
        --project (Join-Path $work 'src/MigrationHost/MigrationHost.csproj') `
        --output-dir Migrations
    if ($LASTEXITCODE -ne 0) { throw 'Migration generation failed.' }

    $target = Join-Path $content 'src/StarterAspMVCEditorTemplates/Migrations'
    Remove-Item $target -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $work 'src/MigrationHost/Migrations') $target -Recurse

    # Generated files carry the throwaway project name. Put the token back so
    # sourceName substitution works when a user generates their own project.
    Get-ChildItem $target -Filter *.cs -Recurse | ForEach-Object {
        (Get-Content $_.FullName -Raw).Replace('MigrationHost', 'StarterAspMVCEditorTemplates') |
            Set-Content $_.FullName -NoNewline
    }

    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Migrations regenerated. Review the diff and commit.' -ForegroundColor Green
}
finally {
    Pop-Location
}
