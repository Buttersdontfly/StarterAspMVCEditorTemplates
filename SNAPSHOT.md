# Package stamp

This file identifies which delivered snapshot of the repository you have.

    Snapshot:       2026-08-12T23:32:34Z
    Expected tests: 81

If `dotnet test tests/StarterAspMVCEditorTemplates.TemplateTests` reports a
different number of tests than the figure above, you are running an older
snapshot and any fix discussed since then is not in your working copy.

## Included in this snapshot

- Seam check scoped to files that use IdentityUser; the gallery's sample model
  has an ordinary UserName property and was a false positive.
- 20 new editor templates added, existing 5 harmonised to the same house style.
- New: Models/EditorSampleModel.cs, Models/LineItem.cs, wwwroot/js/editor-templates.js.
- documentation/editor-templates.md rewritten as a full guide.
- Fixed illegal `--` in two XML comments; Test-XmlWellFormed now reports the
  offending line instead of the whole file.
- --auth none no longer ships dev pages or email services; layout and home page
  reach them through optional partials.
- SQLite upgraded to the 3.x bundle (SQLitePCLRaw.bundle_e_sqlite3 3.0.5),
  clearing the CVE-2025-6965 audit warning.
- EF Design package no longer emits BuildHost-net472 / BuildHost-netcore.
- SeedData is now idempotent under concurrent startup. Parallel test classes
  both created the Admin role and the loser hit a UNIQUE constraint failure.
- Test-TemplatePaths.ps1: `**` is not a globstar in PowerShell, so `tests/**`
  reported zero matches. It now recurses from the base directory.
- All build/Test-*.ps1 now `exit 0` explicitly; callers reset $LASTEXITCODE.
  Fixes CI failing right after reporting that every check passed.
- CI rewritten: golden-samples check now detects untracked files, fetch-depth 0
  everywhere for MinVer, node reuse off, Initialize-Repo in every packing job.
- publish.yml refuses to publish without migrations and vendored assets.
- Template tests disable MSBuild node reuse and shut down build servers first,
  fixing intermittent "Access to the path ... Tasks.dll is denied" failures.
- `SignInWithEmail` is `static readonly`, not `const`: a const folded the other
  mode's branches away, and CS0162 + TreatWarningsAsErrors failed the build.
- `Set-Metadata.ps1` edits `Directory.Build.props` through the XML DOM instead
  of by regex, so it cannot produce malformed XML at all. It also validates its
  own output before reporting success.
- `AccountIdentityConventions.SignInWithEmail` now genuinely switches behaviour.
  Separating username from email is flipping one constant; nothing to uncomment.
- `LoginInputModel` / `RegisterInputModel` always carry a `UserName` property,
  with validation that follows the constant.
- `SeedData` builds its user through the conventions, so the seam is one file.
- Seam tests rewritten around `new IdentityUser` and `IdentityUser.UserName`
  rather than the bare word, since views legitimately bind the input model's
  `UserName`.

## Run first

```powershell
./build/Set-Metadata.ps1 -Author 'Your Name' -GitHubUser 'yourhandle'
./build/Initialize-Repo.ps1
```

One test fails on purpose until `Set-Metadata.ps1` has been run:
`No_metadata_placeholders_remain`. `Migrations/` and `wwwroot/lib/` are
generated and are not in the archive.
