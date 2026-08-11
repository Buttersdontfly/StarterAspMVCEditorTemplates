#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies every MSBuild and solution file in the repo is well-formed XML.

.DESCRIPTION
    Runs in about a second and catches a class of bug that otherwise surfaces as
    a confusing SDK error at pack time, pointing at Microsoft.Common.props rather
    than at the file you actually broke.

    The one that motivated this script: an XML comment cannot contain '--', and
    cannot end with '-' immediately before the closing delimiter. Writing '--'
    as an em dash substitute in a comment is easy, invisible on review, and
    breaks every build that imports the file.

    Note that the template's conditional syntax -- <!--#if (Symbol) --> and
    <!--#endif --> -- IS valid XML, so those files are checked like any other.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

$files = Get-ChildItem $repo -Recurse -File -Include *.props, *.targets, *.csproj, *.slnx, *.config |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|out|\.git)[\\/]' }

$failures = @()

foreach ($file in $files) {
    $relative = $file.FullName.Substring($repo.Length + 1)
    try {
        [xml](Get-Content $file.FullName -Raw) | Out-Null
        Write-Host "  OK    $relative" -ForegroundColor DarkGray
    }
    catch {
        $failures += [pscustomobject]@{ Path = $relative; Message = $_.Exception.Message }
        Write-Host "  FAIL  $relative" -ForegroundColor Red
    }
}

if ($failures.Count -gt 0) {
    Write-Host ''
    foreach ($failure in $failures) {
        Write-Host "::error file=$($failure.Path)::$($failure.Message)" -ForegroundColor Red
    }
    Write-Host ''
    Write-Host "$($failures.Count) malformed XML file(s)." -ForegroundColor Red
    Write-Host "If the message mentions '--', an XML comment contains a double hyphen. Use an em dash or rephrase." -ForegroundColor Yellow
    exit 1
}

Write-Host "All $($files.Count) XML files are well-formed." -ForegroundColor Green
