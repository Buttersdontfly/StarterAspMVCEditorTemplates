#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Removes every local install of this template and reports what is left.

.DESCRIPTION
    Use when `dotnet new` reports duplicate template identities, or throws
    "Sequence contains more than one matching element". That happens when the
    same template identity is registered more than once -- typically after
    repeated local installs of an identically-versioned package.

    Safe to run any time; it only touches this template's package id.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$packageId = 'StarterAspMVCEditorTemplates'

Write-Host 'Currently installed template packages:' -ForegroundColor Cyan
& dotnet new uninstall

for ($attempt = 1; $attempt -le 10; $attempt++) {
    $installed = & dotnet new uninstall 2>&1 | Out-String
    if ($installed -notmatch [regex]::Escape($packageId)) {
        Write-Host "`n$packageId is fully uninstalled." -ForegroundColor Green
        return
    }

    Write-Host "Uninstalling $packageId (attempt $attempt)..." -ForegroundColor DarkGray
    & dotnet new uninstall $packageId 2>&1 | Out-Null
}

Write-Warning "$packageId still appears after 10 attempts. Remaining installs:"
& dotnet new uninstall
