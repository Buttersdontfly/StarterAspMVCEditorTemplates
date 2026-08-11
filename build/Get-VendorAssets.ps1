#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads Bootstrap and jQuery into the template's wwwroot/lib and commits-ready.

.DESCRIPTION
    Client-side assets are vendored and committed rather than restored from a CDN
    or LibMan, because a generated project must work with no network. This is the
    ONLY script in the repo that touches the network.

    Run it once, review, and commit wwwroot/lib.
#>
[CmdletBinding()]
param(
    [string]$BootstrapVersion = '5.3.3',
    [string]$JQueryVersion = '3.7.1',
    [string]$ValidateVersion = '1.21.0',
    [string]$UnobtrusiveVersion = '4.0.0'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$lib = Join-Path $repo 'src/StarterAspMVCEditorTemplates.Templates/content/StarterAspMVCEditorTemplates/src/StarterAspMVCEditorTemplates/wwwroot/lib'

$downloads = @(
    @{ Url = "https://cdn.jsdelivr.net/npm/bootstrap@$BootstrapVersion/dist/css/bootstrap.min.css";       Path = 'bootstrap/css/bootstrap.min.css' }
    @{ Url = "https://cdn.jsdelivr.net/npm/bootstrap@$BootstrapVersion/dist/css/bootstrap.min.css.map";   Path = 'bootstrap/css/bootstrap.min.css.map' }
    @{ Url = "https://cdn.jsdelivr.net/npm/bootstrap@$BootstrapVersion/dist/js/bootstrap.bundle.min.js";  Path = 'bootstrap/js/bootstrap.bundle.min.js' }
    @{ Url = "https://cdn.jsdelivr.net/npm/jquery@$JQueryVersion/dist/jquery.min.js";                     Path = 'jquery/jquery.min.js' }
    @{ Url = "https://cdn.jsdelivr.net/npm/jquery-validation@$ValidateVersion/dist/jquery.validate.min.js"; Path = 'jqueryval/jquery.validate.min.js' }
    @{ Url = "https://cdn.jsdelivr.net/npm/jquery-validation-unobtrusive@$UnobtrusiveVersion/dist/jquery.validate.unobtrusive.min.js"; Path = 'jqueryval/jquery.validate.unobtrusive.min.js' }
)

foreach ($item in $downloads) {
    $target = Join-Path $lib $item.Path
    New-Item -ItemType Directory -Force -Path (Split-Path $target -Parent) | Out-Null
    Write-Host "  $($item.Path)" -ForegroundColor Cyan
    Invoke-WebRequest -Uri $item.Url -OutFile $target
}

# Record what was vendored, so upgrades are a diff rather than an archaeology exercise.
$manifest = @"
# Vendored client-side assets

Downloaded by ``build/Get-VendorAssets.ps1``. Do not edit these files by hand.

| Package | Version |
|---|---|
| bootstrap | $BootstrapVersion |
| jquery | $JQueryVersion |
| jquery-validation | $ValidateVersion |
| jquery-validation-unobtrusive | $UnobtrusiveVersion |

Committed on purpose: the template must work offline, which rules out CDN
references and LibMan restore. To upgrade, re-run the script with new version
parameters and commit the diff.
"@

Set-Content -Path (Join-Path $lib 'README.md') -Value $manifest
Write-Host 'Vendored assets updated.' -ForegroundColor Green
