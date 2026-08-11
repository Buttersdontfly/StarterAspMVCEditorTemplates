# Package stamp

This file identifies which delivered snapshot of the repository you have.

    Snapshot:       2026-08-11T09:41:02Z
    Expected tests: 45

If `dotnet test tests/StarterAspMVCEditorTemplates.TemplateTests` reports a
different number of tests than the figure above, you are running an older
snapshot and any fix discussed since then is not in your working copy.

## Included in this snapshot

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
