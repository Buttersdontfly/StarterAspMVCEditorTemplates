#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Lints generated output for the silent failures a successful build lets through.

.PARAMETER Root
    Folder containing generated projects. Defaults to ./out.
#>
[CmdletBinding()]
param(
    [string]$Root = './out'
)

$ErrorActionPreference = 'Stop'
$failed = $false

function Write-Failure([string]$message) {
    Write-Host "::error::$message" -ForegroundColor Red
    $script:failed = $true
}

$codeFiles = Get-ChildItem $Root -Recurse -File -Include *.cs, *.cshtml, *.csproj, *.json, *.props, *.targets, *.slnx |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$sourceFiles = $codeFiles | Where-Object { $_.Extension -in '.cs', '.cshtml' }

# 1. Leftover template-engine conditionals. Each file type uses different comment
#    syntax, so a mistake in one type is easy to miss by eye.
$conditionals = $codeFiles | Select-String -Pattern '(^|[^A-Za-z])#(if|else|elseif|endif)([^A-Za-z]|$)'
if ($conditionals) {
    $conditionals | ForEach-Object { Write-Host "  $($_.Path):$($_.LineNumber): $($_.Line.Trim())" }
    Write-Failure 'Unprocessed template conditionals left in generated output.'
}

# 2. Unreplaced sourceName.
$sourceName = $codeFiles | Select-String -Pattern 'StarterAspMVCEditorTemplates' -SimpleMatch
if ($sourceName) {
    $sourceName | Select-Object -First 10 | ForEach-Object { Write-Host "  $($_.Path):$($_.LineNumber)" }
    Write-Failure 'sourceName was not replaced everywhere.'
}

# 3. Template engine markers that escaped.
$markers = $sourceFiles | Select-String -Pattern '-:cnd|//#'
if ($markers) {
    Write-Failure 'Template engine markers left in generated output.'
}

# 4. THE USERNAME/EMAIL SEAM INVARIANT.
#    Exactly one file may couple to Identity's login identifier.
#
#    The rule is about IdentityUser, not about the word "UserName". The login
#    and register views legitimately bind m => m.UserName on the INPUT MODEL --
#    that is the field the user types into, and making it easy to add is the
#    whole point of the seam. What must not spread is constructing an
#    IdentityUser or reading its UserName property.
function Remove-Comments([string]$code) {
    $code = [regex]::Replace($code, '@\*.*?\*@', '', 'Singleline')
    $code = [regex]::Replace($code, '/\*.*?\*/', '', 'Singleline')
    return [regex]::Replace($code, '//[^\r\n]*', '')
}

$seamCandidates = $sourceFiles |
    Where-Object { $_.Name -ne 'AccountIdentityConventions.cs' } |
    Where-Object { $_.FullName -notmatch '[\\/]Migrations[\\/]' }

$constructors = $seamCandidates |
    Where-Object { (Remove-Comments (Get-Content $_.FullName -Raw)) -match 'new\s+IdentityUser' }

if ($constructors) {
    $constructors | ForEach-Object { Write-Host ('  ' + $_.FullName) }
    Write-Failure 'IdentityUser is constructed outside AccountIdentityConventions.cs. Route it through CreateUser.'
}

# Scoped to files that actually work with Identity types. "UserName" is an
# ordinary property name that view models and sample data use too, so an
# unscoped match fires on things that are not seam violations at all.
$propertyUses = $seamCandidates |
    Where-Object { $_.Extension -eq '.cs' } |
    Where-Object {
        $code = Remove-Comments (Get-Content $_.FullName -Raw)
        $code -match 'IdentityUser' -and $code -match '\.UserName\b|\bUserName\s*=[^=]'
    }

if ($propertyUses) {
    $propertyUses | ForEach-Object { Write-Host ('  ' + $_.FullName) }
    Write-Failure "Identity's UserName is touched outside AccountIdentityConventions.cs."
}

# 5. Every seam documented in documentation/seams.md must still exist in output.
foreach ($seam in 'SEAM: database provider', 'SEAM: username identity', 'SEAM: email sender') {
    if (-not ($sourceFiles | Select-String -Pattern $seam -SimpleMatch)) {
        Write-Failure "Documented seam missing from generated output: $seam"
    }
}

if ($failed) { exit 1 }
Write-Host 'Generated output looks clean.' -ForegroundColor Green
