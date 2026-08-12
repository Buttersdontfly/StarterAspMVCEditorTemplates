#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks template C# source for syntax mistakes that a build cannot catch here.

.DESCRIPTION
    Template source cannot be compiled directly: it contains `#if (UseIdentity)`
    conditionals resolved by the template engine, and a `sourceName` token in
    place of the user's project name. Errors in it therefore surface only after
    pack, install, generate and build.

    An earlier version of this script loaded Roslyn from the SDK folder. That
    does not work reliably -- the compiler assemblies need their full dependency
    graph, and Add-Type cannot resolve it, so the check silently skipped itself
    and provided no value at all.

    So this is deliberately narrow and dependency-free. It checks the specific
    mistakes that have actually broken this repo, and does not pretend to be a
    parser. Semantic errors still need a real build, which CI does for both
    combos.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$content = Join-Path $repo 'src/StarterAspMVCEditorTemplates.Templates/content'

$files = Get-ChildItem $content -Recurse -File -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

$failed = $false

function Write-Problem([string]$Relative, [int]$Line, [string]$Message) {
    Write-Host "::error file=$Relative,line=$Line::$Message" -ForegroundColor Red
    $script:failed = $true
}

foreach ($file in $files) {
    $relative = $file.FullName.Substring($repo.Length + 1)
    $lines = Get-Content $file.FullName
    $problems = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $matches = [regex]::Matches($line, '"""')
        if ($matches.Count -ne 1) { continue }   # 0 = nothing, 2+ = single-line raw string, both fine

        $position = $matches[0].Index
        $before = $line.Substring(0, $position)
        $after = $line.Substring($position + 3)

        # An OPENING delimiter is preceded by $, @, (, = or whitespace.
        # A CLOSING delimiter is preceded by nothing but whitespace on its line.
        $isClosing = [string]::IsNullOrWhiteSpace($before)
        if ($isClosing) { continue }

        # This is an opening delimiter. For a multi-line raw string the rest of
        # the line must be empty: content on the opening line is a compile error
        # (CS8997, "Unterminated raw string literal") and cascades badly -- one
        # such mistake produced 62 errors in this repo.
        if (-not [string]::IsNullOrWhiteSpace($after)) {
            Write-Problem $relative ($i + 1) 'Raw string literal has content on the opening line. Put the content on the next line, and the closing triple-quote on a line of its own.'
            $problems++
        }
    }

    # Delimiters must pair up across the file.
    $text = Get-Content $file.FullName -Raw
    $total = ([regex]::Matches($text, '"""')).Count
    if ($total % 2 -ne 0) {
        Write-Problem $relative 1 "Odd number of triple-quote delimiters ($total): a raw string literal is unterminated."
        $problems++
    }

    if ($problems -eq 0) {
        Write-Host "  OK    $relative" -ForegroundColor DarkGray
    }
    else {
        Write-Host "  FAIL  $relative" -ForegroundColor Red
    }
}

if ($failed) {
    Write-Host ''
    Write-Host 'Syntax problems in template source. Fix them here rather than after pack + generate + build.' -ForegroundColor Red
    exit 1
}

Write-Host "All $($files.Count) C# files pass the syntax checks." -ForegroundColor Green

# Explicit success exit code, not optional.
#
# A PowerShell script that simply ends does NOT set $LASTEXITCODE -- the caller
# reads whatever the last native command left behind, which may be a failure
# from something entirely unrelated. That made every caller of this script
# unreliable: it passed locally where the previous exit code happened to be 0,
# and failed in CI where it did not.
exit 0
