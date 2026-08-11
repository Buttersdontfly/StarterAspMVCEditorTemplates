#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Parses every PowerShell script in build/ and reports syntax errors.

.DESCRIPTION
    PowerShell only parses a script when it runs, so a syntax error in a script
    you have not run yet is invisible. Worse, a broken script fails at the point
    a caller invokes it, so the error surfaces attributed to the caller.

    The parser is built into PowerShell, so this needs nothing extra.

    Run it after editing any script, before committing.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scripts = Get-ChildItem $PSScriptRoot -Filter *.ps1 | Sort-Object Name
$failed = $false

foreach ($script in $scripts) {
    $tokens = $null
    $errors = $null

    [System.Management.Automation.Language.Parser]::ParseFile(
        $script.FullName, [ref]$tokens, [ref]$errors) | Out-Null

    if ($errors.Count -eq 0) {
        Write-Host ('  OK    ' + $script.Name) -ForegroundColor DarkGray
        continue
    }

    $failed = $true
    Write-Host ('  FAIL  ' + $script.Name) -ForegroundColor Red

    foreach ($parseError in $errors) {
        $line = $parseError.Extent.StartLineNumber
        Write-Host ('        line ' + $line + ': ' + $parseError.Message) -ForegroundColor Red
    }
}

if ($failed) {
    Write-Host ''
    Write-Host 'Syntax errors in build scripts.' -ForegroundColor Red
    Write-Host 'Common cause: PowerShell escapes with a backtick, not a backslash. A backslash-quote inside a double-quoted string ends the string early.' -ForegroundColor Yellow
    exit 1
}

Write-Host ('All ' + $scripts.Count + ' PowerShell scripts parse cleanly.') -ForegroundColor Green
