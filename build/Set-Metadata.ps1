#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fills in author and repository metadata across every file that carries it.

.DESCRIPTION
    Four files need the same two values, and they end up in the published
    package, so getting them out of step is both easy and embarrassing:

        Directory.Build.props   Authors, Company, Copyright, RepositoryUrl
        .template.config/template.json   author
        LICENSE                 the copyright line
        README.md               repository links

    Run once after cloning. Re-runnable: it replaces the placeholders, and also
    recognises values it set previously, so changing your GitHub handle later is
    a second run rather than a hunt.

.PARAMETER Author
    Your name as it should appear in the package and the licence.

.PARAMETER GitHubUser
    Your GitHub account or organisation name, used to build repository URLs.

.PARAMETER Year
    Copyright year. Defaults to the current year.

.EXAMPLE
    ./build/Set-Metadata.ps1 -Author 'Jane Doe' -GitHubUser 'janedoe'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Author,

    [Parameter(Mandatory)]
    [string]$GitHubUser,

    [int]$Year = (Get-Date).Year
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent

# Directory.Build.props is handled separately, through the XML DOM.
$targets = @(
    'src/StarterAspMVCEditorTemplates.Templates/content/StarterAspMVCEditorTemplates/.template.config/template.json'
    'LICENSE'
    'README.md'
    'documentation/publishing.md'
)

$changed = 0

<#
    MSBuild files are edited through the XML DOM, not with text substitution.

    An earlier version used a regex on the raw text and a greedy pattern ate the
    closing </Copyright> tag, leaving a Directory.Build.props that would not
    parse. Tightening the pattern fixed that specific case, but the approach was
    the problem: a regex has no idea where an element ends, so every future edit
    is one careless quantifier away from the same class of damage.

    Setting InnerText through XmlDocument cannot produce malformed XML, whatever
    the value contains. PreserveWhitespace keeps the file's formatting and
    comments intact.
#>
function Set-MsBuildProperty {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][hashtable]$Values
    )

    $xml = New-Object System.Xml.XmlDocument
    $xml.PreserveWhitespace = $true
    $xml.Load($Path)

    $modified = $false

    foreach ($name in $Values.Keys) {
        $nodes = $xml.SelectNodes("//PropertyGroup/$name")
        foreach ($node in $nodes) {
            if ($node.InnerText -ne $Values[$name]) {
                $node.InnerText = $Values[$name]
                $modified = $true
            }
        }
    }

    if ($modified) { $xml.Save($Path) }
    return $modified
}

$propsPath = Join-Path $repo 'Directory.Build.props'
$repoUrl = "https://github.com/$GitHubUser/StarterAspMVCEditorTemplates"

if (Set-MsBuildProperty -Path $propsPath -Values @{
        Authors           = $Author
        Company           = $Author
        Copyright         = "Copyright (c) $Year $Author"
        RepositoryUrl     = $repoUrl
        PackageProjectUrl = $repoUrl
    }) {
    Write-Host '  updated  Directory.Build.props' -ForegroundColor Green
    $changed++
}
else {
    Write-Host '  no change Directory.Build.props' -ForegroundColor DarkGray
}

# The remaining targets are plain text or JSON, where substitution is safe.
foreach ($relative in $targets) {
    $path = Join-Path $repo $relative
    if (-not (Test-Path $path)) {
        Write-Warning "Not found, skipping: $relative"
        continue
    }

    $original = Get-Content $path -Raw
    $text = $original

    $text = $text.Replace('__AUTHOR__', $Author)
    $text = $text.Replace('__GITHUB_USER__', $GitHubUser)

    # Catch values written by a previous run, so this stays re-runnable. Both
    # patterns stop at a line break and at '<' so they cannot cross out of a
    # markup element in the markdown files.
    $text = [regex]::Replace($text, 'github\.com/[^/\s"''<>)]+/StarterAspMVCEditorTemplates',
        "github.com/$GitHubUser/StarterAspMVCEditorTemplates")
    $text = [regex]::Replace($text, 'Copyright \(c\) \d{4} [^<\r\n]*',
        "Copyright (c) $Year $Author")

    if ($text -ne $original) {
        Set-Content -Path $path -Value $text -NoNewline
        Write-Host "  updated  $relative" -ForegroundColor Green
        $changed++
    }
    else {
        Write-Host "  no change $relative" -ForegroundColor DarkGray
    }
}

# Anything left behind is a file this script does not know about.
$remaining = Get-ChildItem $repo -Recurse -File -Include *.props, *.json, *.md, LICENSE |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|artifacts|out|samples|\.git)[\\/]' } |
    Where-Object { (Get-Content $_.FullName -Raw) -match '__AUTHOR__|__GITHUB_USER__' }

if ($remaining) {
    Write-Host ''
    Write-Warning 'Placeholders remain in files this script does not handle. Add them to $targets:'
    $remaining | ForEach-Object { Write-Host "  $($_.FullName.Substring($repo.Length + 1))" -ForegroundColor Yellow }
    exit 1
}

# Self-check: this script edits MSBuild files, so it verifies its own output
# rather than leaving a broken build for the next command to discover.
$global:LASTEXITCODE = 0
& (Join-Path $PSScriptRoot 'Test-XmlWellFormed.ps1') | Out-Null
if ($LASTEXITCODE -ne 0) {
    & (Join-Path $PSScriptRoot 'Test-XmlWellFormed.ps1')
    throw 'Set-Metadata produced malformed XML. Restore the files from git and report this.'
}

Write-Host ''
Write-Host "Metadata set for $Author (github.com/$GitHubUser). $changed file(s) changed." -ForegroundColor Green
