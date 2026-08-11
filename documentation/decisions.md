# Decisions

A running log of what was decided and why, so the reasoning survives the conversation.

## Scope

| Decision | Choice | Reasoning |
|---|---|---|
| Database | SQLite, always. No option. | The no-database path duplicated Microsoft's own MVC template, and carried a disproportionate share of complexity (throwing provider, custom exception, docs explaining why the app builds but won't run). Deleted. |
| Provider swap | Edit `Directory.Build.props` + one call in `Program.cs` | The generated project needs a stopper `Directory.Build.props` regardless, so putting the EF `PackageReference` there is free. CPM is off in the output so the swap stays a single-file edit. |
| Auth | `--auth identity\|none`, default `identity` | Identity default means the editor templates are visible out of the box, which removed the need for a separate demo page. |
| `--auth none` output | Empty `AppDbContext` on SQLite, editor templates, kitchen sink. No account pages. | |
| Combos to test | 2 | Down from 6. Everything runs on `ubuntu-latest`, so the full matrix fits on every PR. |
| Target framework | `net10.0`, single-target | Multi-targeting doubles the matrix for little gain. |
| CSS | Bootstrap 5.3, vendored into `wwwroot/lib` and committed | No LibMan or CDN restore, so it works offline. |

## Offline behaviour

`RestorePackagesWithLockFile=true` in the generated project, **no lock file shipped**.

A committed lock file would be wrong: the restore graph differs per combo, and the
template ships one file set. `--locked-mode` would then hard-fail. Instead the
first restore (online, straight after generation) writes the correct lock file for
that combo; every restore afterwards is version-pinned and served from the global
package cache.

To be precise about the mechanism: the lock file does not itself make restore
offline. It pins versions so later restores hit `~/.nuget/packages` rather than
resolving against nuget.org. Offline works because the first restore populated
the cache. The `offline-restore` CI job verifies this by clearing the sources and
restoring again, so the claim is tested rather than assumed.

## Editor templates: mixed dispatch, on purpose

| Template | Resolves by | Why |
|---|---|---|
| `EmailAddress.cshtml` | `[DataType(DataType.EmailAddress)]` | MVC picks it up from the data type automatically. |
| `Password.cshtml` | `[DataType(DataType.Password)]` | Same. |
| `AddressInputModel.cshtml` | Type name | Complex types resolve by type name, not data type. |
| `PersonNameInputModel.cshtml` | Type name | Same. |

Each template carries a header comment stating *why* it resolves, so the
inconsistency reads as deliberate rather than accidental. See
`editor-templates.md` for the full resolution order.

## Username / email separation

Not a template option. A seam, in one file: `Identity/AccountIdentityConventions.cs`.

Controllers never touch `UserName` — they call the conventions class. That
invariant is enforced by the L1 lint (`build/Lint-Generated.ps1`), which fails CI
if `UserName` appears in any generated `.cs` or `.cshtml` outside that file and
`SeedData.cs`. The point is that you never have to hunt for the coupling: it
cannot spread without breaking the build.

Separating them is a three-step edit, documented in `seams.md`.

## Testing strategy

| Layer | What it does | Where |
|---|---|---|
| L1 | Lints generated output for leftover conditionals, unreplaced `sourceName`, seam violations | `build/Lint-Generated.ps1` |
| L2 | Instantiates both combos, builds with `--warnaserror`, runs generated tests | `ci.yml` / `build-matrix` |
| L3 | Golden samples: regenerating `samples/` must produce no diff | `build/Regen-Samples.ps1` |
| L4 | Runtime smoke: register → read reset link from dev mailbox → reset → log in | `tests/` (to build) |
| L5 | `/dev/editors` kitchen sink renders every template in valid/invalid/empty state; snapshotted | template + `tests/` (to build) |

L3 is the direct answer to "don't let it silently break": every template change
becomes a reviewable diff across both combos in the PR, rather than something a
user discovers later.

L5 is why the dev-only kitchen sink page survived the scope cut — adding a new
editor template without registering it there fails CI, so coverage cannot rot.

## Publishing

Trusted publishing via `NuGet/login@v1` and `id-token: write`. Versioning by
MinVer from `v*` tags. Publish triggers on GitHub Release published. Package ID
`StarterAspMVCEditorTemplates`.

See `publishing.md` for the setup steps, including the private-repo caveat.

## Open

- Environment name for the publish gate — assumed `nuget-release`.
- `.slnx` (XML solution format) is used so the test project can be conditionally
  included with real XML comment conditionals. Requires the .NET 10 SDK, which we
  target anyway. Fallback if this causes tooling friction: ship two `.sln` files.

## Not yet done

- **Migrations are not generated.** `build/Generate-Migrations.ps1` produces them,
  but it needs the .NET SDK. Until it is run once and the output committed, the
  Identity combo will start with an empty database, because `Program.cs` calls
  `MigrateAsync()` and there is nothing to apply. This is the first thing to run
  on a machine with the SDK.
- **Bootstrap and jQuery are not vendored.** `wwwroot/lib/README.md` lists the
  expected files. They must be downloaded once and committed, since the offline
  requirement rules out CDN and LibMan.

## NuGet audit warnings are warnings, not errors

`TreatWarningsAsErrors` is on, but `NU1901`-`NU1904` are excluded via
`WarningsNotAsErrors` in both `Directory.Build.props` files.

The reasoning matters more than the setting. `TreatWarningsAsErrors` is about
your own code: a warning is a defect you introduced and can fix. Audit warnings
come from an advisory database that changes underneath you. A build that passes
today fails tomorrow when an advisory is published against a package you never
touched -- usually a transitive dependency you cannot upgrade.

For a template distributed to other people, that non-determinism is
unacceptable: `dotnet new` would break for every user the moment an advisory
landed. Findings still surface as warnings.

Concretely, this bit immediately: `Microsoft.EntityFrameworkCore.Sqlite` pulls
`SQLitePCLRaw.lib.e_sqlite3`, whose bundled native SQLite is flagged by
CVE-2025-6965 (GHSA-2m69-gcr7-jv3q). The advisory covers all 2.1.x versions and
there is no fixed release; it is tracked upstream in dotnet/efcore#38257. A
commented-out pin to the newer native bundle sits in the generated
`Directory.Build.props` for anyone who needs a clean audit, with a warning about
the managed/native version skew it creates.

Review findings with `dotnet list package --vulnerable --include-transitive`.

## Local template installs need a clean slate

`build/Install-TemplateLocally.ps1` is the only place that packs and installs
for local use, and every other script calls it. It exists because two failure
modes bite hard otherwise:

- **Duplicate identities.** Repeated `dotnet new install` of the same package can
  leave several registrations sharing one template identity. The engine then
  throws `Sequence contains more than one matching element` on any use of the
  template. The helper uninstalls in a loop until the id is gone before
  installing. `build/Reset-TemplateInstalls.ps1` does the cleanup on its own.

- **Stale package cache.** NuGet caches by id + version. Without git tags MinVer
  falls back to `0.0.0-alpha.0` on every pack, so after editing the template you
  can reinstall and silently get the previous content. The helper passes
  `-p:MinVerVersionOverride=0.0.0-dev.<timestamp>` so each local pack is unique.
  This is local only -- release versioning still comes from git tags.

## XML comments cannot contain `--`

`build/Test-XmlWellFormed.ps1` parses every `.props`, `.targets`, `.csproj`,
`.slnx` and `.config` in the repo. It runs first in CI, and again inside
`Install-TemplateLocally.ps1` before packing.

It exists because a `--` inside an XML comment is illegal, easy to write when
reaching for an em dash, invisible on review, and surfaces as an SDK error that
points at `Microsoft.Common.props` rather than at the file that is actually
broken. Comments in this repo use an em dash instead.

The template's conditional syntax (`<!--#if (Symbol) -->`) is valid XML and
passes the check unchanged.

## Template source is checked before it is packed

Template content cannot be compiled directly: it contains `#if (UseIdentity)`
conditionals for the template engine and a `sourceName` token instead of a real
project name. So errors in it historically surfaced only after pack, install,
generate and build -- four steps and a couple of minutes away from the edit that
caused them.

Three checks now run before any pack, locally via
`Install-TemplateLocally.ps1` and first in CI:

| Check | Catches |
|---|---|
| `Test-XmlWellFormed.ps1` | `--` in XML comments, malformed `.props`/`.csproj` |
| `Test-CSharpSyntax.ps1` | Roslyn syntax errors: broken raw strings, unbalanced braces |
| `Test-TemplatePaths.ps1` | `template.json` paths that match no files |

They are syntax-level only. Semantic errors -- unknown types, wrong overloads,
nullability -- still need a real build of generated output, which the CI matrix
does for both combos.

The bug that prompted `Test-CSharpSyntax.ps1`: a multi-line raw string literal
must have its content on the line AFTER the opening `"""`, with the closing
`"""` on its own line. Four email bodies were written with HTML on the opening
line, producing 62 cascading errors from one mistake.

## SQLite paths are resolved, not trusted

`Data/SqliteDatabasePath.cs` rewrites the connection string before the DbContext
is registered. Two failures made it necessary, both reported as
`SQLite Error 14: unable to open database file`:

- SQLite creates the database **file** but never the **directories** above it, so
  `Data Source=App_Data/app.db` fails until `App_Data` exists.
- A relative `Data Source` resolves against the current working directory, not
  the project. `dotnet run` from a parent folder, a published app, and
  integration tests via `WebApplicationFactory` each have a different working
  directory, so the same connection string silently points at different files.

The helper makes the path absolute under `ContentRootPath` and creates the
directory. In-memory databases pass through untouched, so tests can still use
`Data Source=:memory:`.

It is part of the database provider seam and should be deleted when moving to a
server-based provider.

## Build scripts are parsed before they are trusted

`build/Test-PowerShellSyntax.ps1` parses every script in `build/` with
PowerShell's own parser.

PowerShell only parses a script when it runs, so a syntax error in a script
nobody has run yet is invisible -- and when it does run, the failure is
attributed to whichever script invoked it, not to the broken file. That made one
bad string literal look like a packaging failure.

The recurring cause is worth naming: PowerShell escapes with a backtick. A
backslash before a quote does not escape it, it ends the string.

This check is wired into CI but deliberately NOT into
`Install-TemplateLocally.ps1`: a script cannot usefully vouch for its siblings if
it cannot parse itself. Run it directly after editing anything in `build/`.

## Packaged paths are kept short on purpose

Two changes, after `dotnet pack` failed with NU5123 (path too long):

**The doubling was a bug.** `ContentTargetFolders=content` combined with a source
folder already named `content` produced package paths starting `content/content/`,
pushing every file 8 characters deeper for no reason. The `Content` item now sets
`PackagePath` explicitly and `ContentTargetFolders` is unset.

**The vendored asset folders were merged.** `jquery-validation/` and
`jquery-validation-unobtrusive/` became one `jqueryval/`, the convention the
older ASP.NET MVC templates used. Together the two fixes take the deepest
packaged path from 154 to 126 characters.

Length is a structural constraint here, not a nuisance: a template package
contains a whole project tree, and the package path is
`content/<sourceName>/src/<sourceName>/...` before any real content starts. The
project name appears twice, so a long project name costs double. NU5123 is also
in `NoWarn` for the template package, since some depth is unavoidable and
`TreatWarningsAsErrors` would otherwise make a warning fatal.

Worth remembering when adding deeply nested files to the template.

## Generated content is not committed, so it must be restored

`wwwroot/lib/` and `Migrations/` are produced by scripts, not written by hand,
and are absent from a fresh clone.

The failure mode is nasty because nothing complains at the right moment: the
template packs, installs and generates a project perfectly well without them.
The app then starts, `MigrateAsync` finds no migrations and does nothing, the
database is created empty, and the first Identity query fails with
`SQLite Error 1: no such table: AspNetRoles` -- a stack trace pointing at the
seeder, several steps from the actual cause.

Three mitigations:

- `build/Initialize-Repo.ps1` restores whatever is missing. Run it after cloning
  or after replacing the repo contents.
- `Program.cs` checks for migrations before seeding and throws an actionable
  message naming the command to run.
- `Test-TemplatePaths.ps1` reports missing generated paths as MISSING with a
  pointer to `Initialize-Repo.ps1`.

## Console links are HTML-decoded

The development email sender extracts the first link from the HTML body for its
console line. That link must be `WebUtility.HtmlDecode`d first.

The href inside the body is HTML encoded, so the ampersand separating query
parameters is written `&amp;`. A browser decodes it on click, so the link on
`/dev/mailbox` works. Text copied out of a console does not pass through a
browser, so ASP.NET receives a parameter named `amp;token`, the real `token`
binds as null, and the reset fails with "invalid token" -- a message that points
at token expiry or corruption rather than at encoding.

The `.eml` file carries the same decoded link in an `X-Dev-Link` header, so it
survives being read as text instead of rendered.

## `--warnaserror` on the command line is not the same as TreatWarningsAsErrors

Nothing passes `--warnaserror` when building generated output. The generated
project already sets `TreatWarningsAsErrors` in its own `Directory.Build.props`,
alongside `WarningsNotAsErrors` for the NuGet audit codes NU1901-NU1904.

The command-line flag promotes warnings at the MSBuild level, where
`WarningsNotAsErrors` does not apply. Audit findings therefore became errors
again, and both combos failed to build on a vulnerability in a transitive
dependency that has no fix. It also tested a stricter configuration than any user
will ever run.

`BuildTests` asserts the mechanism separately, so removing
`TreatWarningsAsErrors` from the template cannot go unnoticed.

## The seam check ignores comments and generated code

`UserName` legitimately appears in three kinds of place that are not violations:

- Comments documenting the seam -- the commented-out alternative in the input
  models, the `EditorFor` line in the login and register views, and the note in
  `AccountController` stating it deliberately never touches `UserName`.
- EF migrations, which are generated code and necessarily name the column.
- `AccountIdentityConventions.cs` and `SeedData.cs`, which own the mapping.

Both `GenerationTests` and `Lint-Generated.ps1` strip comments and skip
`Migrations/` before matching. A check that fires on its own documentation trains
people to ignore it.

## Shared files must not reference excluded code

The `--auth none` combo failed to compile with CS0234 while its file list was
exactly right: `_ViewImports.cshtml` imported
`StarterAspMVCEditorTemplates.Models.Account`, a namespace that only exists with
Identity.

This is the failure mode whole-file exclusion invites, and it is worth naming
because the generation assertions cannot catch it. Nothing was missing and
nothing was extra -- a SHARED file simply pointed at an excluded one. The error
also names the shared file rather than the exclusion, so it reads like a broken
import instead of a combo problem.

Two changes:

- `_ViewImports.cshtml` no longer imports the account models. The five account
  views name their model type in full, so the dependency lives only in files that
  are themselves excluded alongside the models.
- `GenerationTests` asserts that nothing surviving `--auth none` references
  `Models.Account`, `AccountIdentityConventions`, `SeedData` or
  `IdentityEmailSenderAdapter`, with comments stripped first -- several shared
  files legitimately mention those names while explaining why they do not depend
  on them.

The general rule when adding to the template: a file included in both combos may
not reference a type that exists in only one, and `_ViewImports.cshtml` is the
easiest place to get this wrong because it applies to every view at once.

## Package metadata lives in one place

Author, company, copyright and repository URLs are set in the repo-level
`Directory.Build.props`; `template.json`, `LICENSE`, `README.md` and
`publishing.md` carry the same values.

`build/Set-Metadata.ps1` fills all of them from two arguments, and is
re-runnable: it recognises values it wrote previously, so changing a GitHub
handle later is one command rather than a hunt through five files.

`GenerationTests.No_metadata_placeholders_remain` fails while any placeholder is
still present. That test is expected to fail on a fresh clone -- it is the
reminder. The cost of getting this wrong is asymmetric: a package version on
nuget.org cannot be replaced, only unlisted, so a package authored by
`__AUTHOR__` is permanent.

`LICENSE` is packed via `PackageLicenseFile` rather than declared with
`PackageLicenseExpression`, so the licence text ships with the package instead
of being merely asserted.

The icon at the repo root is packed as `PackageIcon` and copied into
`.template.config/` for the `ide.host.json` reference, which had pointed at a
file that did not exist.

## The seam switch is `static readonly`, not `const`

`AccountIdentityConventions.SignInWithEmail` is `public static readonly bool`.

Making it `const` is the obvious choice and is wrong here. A const bool is folded
at compile time, so every branch for the other mode becomes unreachable, and
CS0162 combined with `TreatWarningsAsErrors` fails the build -- not only in the
conventions class but in the Razor views, whose generated code carries the same
`if`. The result is a seam that compiles in exactly one of its two modes, which
is the opposite of what it is for.

`static readonly` costs one field read per branch and keeps the flip to a single
edit. `GenerationTests.Sign_in_convention_is_not_a_compile_time_constant` guards
against it being "tidied" back.

## MSBuild files are edited through the XML DOM

`Set-Metadata.ps1` writes `Directory.Build.props` via `XmlDocument` with
`PreserveWhitespace`, not with text substitution.

The first version used a regex, and a greedy `.+` consumed the closing
`</Copyright>` tag, producing a file that would not parse. Tightening the pattern
fixed that case, but the approach was the fault: a regex has no notion of where
an element ends, so every later edit is one careless quantifier from the same
damage. `InnerText` cannot produce malformed XML whatever the value contains.

Plain text and JSON targets still use substitution, where it is safe.

The script also runs `Test-XmlWellFormed.ps1` on its own output before reporting
success, so it cannot leave a broken build for the next command to find.

## Child builds run without MSBuild node reuse

`TemplateFixture` sets `MSBUILDDISABLENODEREUSE=1` and
`DOTNET_CLI_USE_MSBUILD_SERVER=0` for every child process, passes
`-nodeReuse:false` on the build command line, and runs
`dotnet build-server shutdown` before the first build.

MSBuild keeps worker nodes alive between builds, and those nodes hold task
assemblies loaded. `Microsoft.AspNetCore.Mvc.Testing.Tasks.dll` is one, pulled in
by the test project inside generated output. A node surviving from an earlier run
keeps a file lock on it in the NuGet package folder, and the next restore fails
with `Access to the path ... is denied` -- which reads as a permissions problem
and is really a stale process.

It is also intermittent, since it depends on whether a node happens to still be
alive, and an intermittent failure in the build tests would undermine trust in
the whole suite. `BuildTests` recognises the signature and says so in the
failure message rather than leaving it to be rediscovered.

## CI notes

A few choices in `ci.yml` that are not obvious:

- **`git status --porcelain`, not `git diff`, for the golden samples.**
  Regeneration can ADD files, and `git diff` does not report untracked ones. The
  original check would have passed green on a repo where `samples/` was never
  committed, proving nothing. A separate step first asserts that `samples/` has
  committed content at all.
- **`fetch-depth: 0` on every job.** MinVer derives the version from tags, and a
  shallow clone has none, so every pack would be `0.0.0-alpha.0`.
- **`working-directory` rather than `dotnet build ./out`.** Passing a directory
  relies on there being exactly one project or solution to discover.
- **Node reuse disabled globally.** Same file-locking problem as the template
  tests; these jobs build several times each.
- **`Initialize-Repo.ps1` in every job that packs.** Migrations and vendored
  assets are generated, not committed, and a template packed without them
  installs fine and then fails at runtime.

`publish.yml` refuses to publish if either is missing. That check is worth the
seconds: a nuget.org version cannot be replaced, only unlisted.
