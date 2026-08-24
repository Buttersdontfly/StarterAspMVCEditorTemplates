# StarterAspMVCEditorTemplates

An ASP.NET Core MVC starter template where the account pages are built entirely
from `EditorTemplates`, so the input rendering is yours to edit in one place
rather than buried in a Razor Class Library.

```bash
dotnet new install StarterAspMVCEditorTemplates
dotnet new starterasp-mvc -n MyApp
cd MyApp && dotnet run
```

Sign in with `dev@localhost` / `123User!`.

Repository: <https://github.com/__GITHUB_USER__/StarterAspMVCEditorTemplates>
Licence: MIT, (c) __AUTHOR__

## Options

| Option | Values | Default |
|---|---|---|
| `--database` | `sqlite`, `sqlserver` | `sqlite` |
| `--auth` | `none`, `identity`, `pepper`, `protected` | `identity` |
| `--tests` | `true`, `false` | `true` |
| `--seed-email` | any email | `dev@localhost` |

`--database sqlserver` targets LocalDB, which is Windows only. Note that SQLite
cannot order by `decimal`, `DateTimeOffset`, `TimeSpan` or `ulong` — if your
model sorts on money or timestamps-with-offset, choose `sqlserver`.

For any other provider, edit `Directory.Build.props` and the `Use...` call in
`Program.cs`, then regenerate migrations — see
[documentation/seams.md](documentation/seams.md).

Each `--auth` level adds to the one before it: `identity` gives the account
pages, `pepper` adds a secret to every password hash, and `protected` adds
encryption of personal data columns at rest. A random pepper and lookup key are
generated per project into `appsettings.Development.json` — see
[documentation/seams.md](documentation/seams.md) for what they cost you if lost.

## What you get

- Login, logout, register, forgot password, reset password, change password —
  hand-rolled MVC controllers and views, no `Identity.UI` RCL shadowing them
- Editor templates for email, password, person name and address
- A Development-only `/dev/editors` page rendering every editor template in
  valid, invalid and empty states
- A fake email sender that writes to the console, to `.eml` files, and to a
  Development-only `/dev/mailbox` page with clickable reset links
- Bootstrap 5.3, vendored — no CDN, no LibMan
- A seeded development user

## Offline use

After one online `dotnet restore`, the project restores and builds with no
network. The first restore writes `packages.lock.json`, which pins every version
so later restores resolve from the local package cache. A CI job proves this by
restoring with all package sources removed.

## Repository layout

```
src/            the template package and its content
tests/          tests for the template itself
samples/        committed generated output, one per combo (see below)
documentation/  decisions, seams, editor templates, publishing
build/          lint and sample-regeneration scripts
```

`samples/` is checked in on purpose. CI regenerates it and fails on any diff, so
every change to the template shows up as a reviewable diff across both option
combinations, instead of silently breaking generation for users.

## Working on the template

Two parts of the template are **generated, not committed**:

| Path | Produced by |
|---|---|
| `.../wwwroot/lib/` | `build/Get-VendorAssets.ps1` |
| `.../Migrations/` | `build/Generate-Migrations.ps1` |

A fresh clone has neither, and neither survives replacing the repo contents from
an archive. The template still packs and installs without them -- it just
generates an app with no styling and no database tables. So after cloning, or
after any wholesale replacement of the repo:

```powershell
./build/Initialize-Repo.ps1
```

It restores whatever is missing and skips whatever is already there.

## Contributing

Build scripts are PowerShell. `pwsh` runs on Windows, macOS and Linux, and is
preinstalled on GitHub's runners, so there is one set of scripts rather than two.

```powershell
./build/Test-PowerShellSyntax.ps1   # after editing anything in build/
./build/Reset-TemplateInstalls.ps1  # if dotnet new reports duplicate identities
./build/Test-TemplatePaths.ps1   # template.json exclude paths still resolve
./build/Regen-Samples.ps1        # then commit samples/
./build/Lint-Generated.ps1 -Root ./out
./build/New-FileTree.ps1         # refresh documentation/file-tree.md
```

`documentation/file-tree.md` is the canonical layout. Regenerate it whenever you
add or move a file — and if you move something referenced by `template.json`,
run the path check, because a mistyped exclude does not fail generation, it
silently excludes nothing.
