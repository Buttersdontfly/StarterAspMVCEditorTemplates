# Package stamp

This file identifies which delivered snapshot of the repository you have.

    Snapshot:       2026-08-19T17:17:45Z
    Expected tests: 95

If `dotnet test tests/StarterAspMVCEditorTemplates.TemplateTests` reports a
different number of tests than the figure above, you are running an older
snapshot and any fix discussed since then is not in your working copy.

## Included in this snapshot

Breaking: this is a v0.2.0 shape, not compatible with v0.1.0 projects.

- Login identifiers are nullable: the implicit [Required] on non-nullable
  reference types broke sign-in when SignInWithEmail was false.
- The database is now named after the project, not the template.
- dotnet-ef is now a pinned LOCAL tool (.config/dotnet-tools.json), so a clean
  machine no longer fails with "dotnet-ef does not exist" right after install.
- Local packs version as 9999.0.0-dev.* so they sort above the released package.
- GenerationTests.Files now excludes bin/obj, fixing an order-dependent failure.
- `--auth none|identity|pepper|protected`, each level adding to the previous.
- Pepper and lookup key generated per project into appsettings.Development.json.
- `--database sqlite|sqlserver`. SQL Server targets LocalDB. SQLite remains the
  default. Motivated by SQLite being unable to order by `decimal`.
- Migrations are now per provider: `Migrations/Sqlite` and `Migrations/SqlServer`.
  `Generate-Migrations.ps1` produces both.
- `ApplicationUser : IdentityUser<Guid>` and `ApplicationRole : IdentityRole<Guid>`,
  both empty, replacing the framework types throughout.
- SQL Server is generated and built in the template tests, but not run.

## Run first

```powershell
./build/Set-Metadata.ps1 -Author 'Your Name' -GitHubUser 'yourhandle'
./build/Initialize-Repo.ps1     # regenerates BOTH migration sets
```

Migrations from v0.1.0 are invalid: the schema changed with the Guid keys.
Delete `Migrations/` before regenerating.
