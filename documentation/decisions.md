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

## Check scripts exit explicitly

Every `build/Test-*.ps1` ends with `exit 0`.

A PowerShell script that simply runs off the end does not set `$LASTEXITCODE`.
The caller then reads whatever the last native command left behind, which may be
a failure from something completely unrelated. `Initialize-Repo.ps1` reported
"Build scripts have syntax errors" in CI immediately after printing that all 13
scripts parsed cleanly -- the check had passed and the exit code was stale.

This is the worst shape of bug: it passes locally, where the preceding exit code
happens to be 0, and fails on a fresh runner where it does not.

Two defences. Each check script exits explicitly, and every caller sets
`$global:LASTEXITCODE = 0` before invoking one. `Test-PowerShellSyntax.ps1` also
fails if any sibling `Test-*.ps1` lacks a trailing `exit 0`, so a new check
script cannot reintroduce it.

## `**` is not a globstar in PowerShell

`Test-TemplatePaths.ps1` resolves a `/**` pattern by recursing from the base
directory, not by passing the wildcard to `Get-ChildItem`.

PowerShell treats `**` as an ordinary single-level wildcard. `Get-ChildItem
-Path 'tests/**' -File -Recurse` therefore matched only the directory directly
beneath `tests/`, and `-File` discarded it -- reporting zero matches for a
directory containing six files.

What made it hard to spot: every other `/**` exclude in `template.json` points at
a directory whose direct children are files, and those resolved correctly.
`tests/**` is the only one whose direct child is a directory, so it was the only
pattern that failed.

The original checker was Python, where the equivalent glob is recursive, and it
passed. The bug arrived with the PowerShell port and survived because nothing
local runs this script -- `Install-TemplateLocally.ps1` calls the XML and C#
checks but not this one. CI was the first thing ever to execute it.

## Seeding must survive losing a race

`SeedData.SeedAsync` treats "another process created it first" as success rather
than as an error, at every step.

The original was a plain check-then-act: `if (!await RoleExistsAsync) CreateAsync`.
Two instances can both see the role missing and both insert, and the loser dies
with `UNIQUE constraint failed: AspNetRoles.NormalizedName`. CI found this
because xUnit runs test classes in parallel, so several
`WebApplicationFactory` instances start against the same database at once -- but
it is not a test-only problem. A developer running the app while its tests run,
or any two instances sharing a database, hits exactly the same thing.

Migrations need no equivalent handling: EF Core takes its own exclusive lock
while applying them, visible in the startup log as "Acquiring an exclusive lock
for migration application". An earlier version of this fix added a named mutex
around both, which was redundant for migrations, brought `Global\` naming
concerns on Unix, and cluttered a file users read.

The duplicate check matches on message text rather than a provider-specific
exception type, so it keeps working after the database provider is swapped.

Separately, `TestWebAppFactory` gives each test class its own SQLite file. The
app is safe under concurrent startup either way; separate files keep the tests
independent of each other's data as well.

## SQLite is pinned to the 3.x bundle

`Directory.Build.props` references `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5
explicitly, ahead of the 2.1.x that `Microsoft.EntityFrameworkCore.Sqlite`
resolves on its own.

That 2.1.x native build is flagged by CVE-2025-6965 (GHSA-2m69-gcr7-jv3q), and
no fixed 2.1.x release exists, so the audit warning cannot be cleared by waiting.
Referencing the bundle rather than `SQLitePCLRaw.lib.e_sqlite3` upgrades the
managed core, provider, config and native build together; pinning only the native
half leaves the managed and native versions mismatched.

**Not a floating version.** `3.*` would defeat the offline story: the template
sets `RestorePackagesWithLockFile`, whose whole purpose is that a restore
resolves to the same versions every time and can be served from the local cache.
A floating version reintroduces a resolution step that needs the network and can
change under you between machines. Bump the number deliberately instead, and let
CI verify the bump.

## `contentfiles` is omitted from the EF Design package

With the default asset list, EF Core 10's Design package drops
`BuildHost-net472` and `BuildHost-netcore` folders into the project. They appear
in Solution Explorer and can block a clean with "file in use". They are
build-time infrastructure rather than project content, so `contentfiles` is
dropped from `IncludeAssets` while the rest of the default list is kept and
`dotnet ef` still works. See dotnet/efcore#36970. The generated `.gitignore`
lists both folders as well, in case other tooling recreates them.

## `--auth none` ships no dev pages and no email services

With `--auth none` the generated project excludes `Services/**`,
`Controllers/DevController.cs`, `Views/Dev/**`, and the account-flow and
editor-gallery tests.

Without the account flows nothing sends mail and nothing renders the gallery, so
the sender, the mailbox and their tests were all dead code -- the mailbox page in
particular could never show a message. The editor templates themselves still
ship and still work; what goes is the scaffolding around them.

Two consequences worth noting:

- The shared `_Layout.cshtml` and `Views/Home/Index.cshtml` reach the dev pages
  through `<partial optional="true" />` rather than conditionals, so no shared
  view needs template syntax. Same technique as `_LoginPartial`.
- `AngleSharp` is now a conditional `PackageReference`, since only the excluded
  tests use it. That also removes its audit warning from the plain combo.

`GenerationTests` gained a check that the plain combo contains no links to
`/dev/editors` or `/dev/mailbox`. A dead link compiles fine and only shows up as
a 404 when somebody clicks it, so nothing else in the suite would catch it.

## Writing CLI flags in XML comments

`--` is illegal inside an XML comment, which makes writing a double-dash
command-line flag in a comment inside `.props` or `.csproj` a build break. It has
happened twice here: once with `--` used as an em dash, and once writing the
auth option by name.

`Test-XmlWellFormed.ps1` catches it before any pack, and now reports the file and
line rather than dumping the whole document into the error. Use an em dash for
punctuation, and name flags without the leading dashes inside XML comments.

## The seam check is scoped to files that use Identity

`UserName` is an ordinary property name. The gallery's sample model has one, and
sets `UserName = "ada"` on itself -- which is not a seam violation, but an
unscoped text match flagged it.

Both the test and `Lint-Generated.ps1` now require a file to mention
`IdentityUser` before its `UserName` usage counts. A file that never names the
Identity type cannot be touching Identity's property.

To stop the scoping becoming a loophole, a second test pins the shape the seam
relies on: `AccountController` references `AccountIdentityConventions`, never
constructs an `IdentityUser`, and never reads `.UserName`.

## `data-no-post` marks controls that intentionally have no name

`EditorTemplateTests` fails any input rendered without a `name`, because that is
normally a template that has lost its field prefix and binds nothing -- a defect
invisible in the browser.

Two controls are nameless on purpose: the visible text box in `Tags`, and the
swatch in `Color`. Naming either would post a duplicate or a half-typed value.

Rather than keeping a list of exceptions in the test, those controls carry
`data-no-post`, so the intent lives in the markup next to the decision. A second
test asserts the converse -- anything carrying `data-no-post` must genuinely have
no `name` -- so the marker cannot be used to wave through a real mistake.

## Publishing triggers on a tag push or a release

`publish.yml` originally ran on `release: published` only, on the reasoning that
creating a release is the explicit decision to ship. In practice that surprised
more than it protected: pushing a tag looked like it should publish and silently
did nothing, with no run in the Actions tab to explain why.

It now triggers on both a `v*` tag push and a published release. They coexist
safely because the push uses `--skip-duplicate`: whichever runs second finds the
version already on nuget.org and exits cleanly.

Deliberateness comes from the `nuget-release` environment instead, which is a
better place for it. A required reviewer there gates every run whatever started
it, rather than relying on a trigger choice nobody can see.

`workflow_dispatch` with a `dry_run` input (default true) exists for both
problems. It packs, installs the packed nupkg, builds both combos, and stops
before pushing. It also skips the OIDC login, so the pipeline can be exercised
before the trusted publishing policy exists. Gated by the same environment, so
it grants nothing extra.

## SQL Server is an option; SQLite stays the default

`--database sqlite|sqlserver`, default `sqlite`.

The reason SQL Server was needed is worth recording, because it is not obvious
and it bites in normal use: **SQLite has no native `decimal`**. EF Core can read
and write the values, and compare for equality, but ordering has to happen on the
client, so `OrderBy` on a `decimal` throws
`SQLite cannot order by expressions of type 'decimal'`. The same applies to
`DateTimeOffset`, `TimeSpan` and `ulong`. A value converter to `double` works but
loses precision, which is the wrong trade for money.

That is a real defect in what shipped: `LineItem.UnitPrice` is a `decimal`, so
ordering line items by price fails on SQLite today.

### Testing

SQLite gets the full treatment. SQL Server is generated and **built but never
run**. Building catches what actually differs per provider -- a migration that
does not compile, a conditional leaving the wrong provider wired up -- while
running would need LocalDB, which is Windows only, and the application code is
identical either way. `BuildTests` also asserts the SQL Server output contains
`UseSqlServer` and not `UseSqlite`, because a conditional that silently kept
SQLite would still compile.

### Migrations

Provider specific, so there are two sets in `Migrations/Sqlite` and
`Migrations/SqlServer`, and the template excludes whichever does not apply.
Shipping both would collide on the model snapshot class.
`Generate-Migrations.ps1` produces both by generating one project per provider.
Generating needs no live server, only the provider assembly.

### Connection string

`appsettings.json` carries a `CONNECTION-STRING-PLACEHOLDER` replaced by a
`switch` generator in `template.json`. Comment-style conditionals would leave
invalid JSON in the repository; this keeps the file parseable by ordinary tooling
at rest.

## Identity types are the application's own, keyed on Guid

`ApplicationUser : IdentityUser<Guid>` and `ApplicationRole : IdentityRole<Guid>`,
both deliberately empty.

Empty because introducing them later changes the schema, so having them from the
start makes adding a property an edit rather than a migration away from the
framework type. Guid rather than the default string key because it is opaque,
leaks neither row counts nor creation order, and survives moving rows between
databases. The cost is index width, irrelevant at the scale this template targets.

## Generation assertions ignore build output

`GenerationTests.Files` excludes `bin/` and `obj/`.

`BuildTests` shares the fixture, and a build copies content files such as
`appsettings.Development.json` into `bin`. Any assertion that counts or matches
files then sees each one twice, and whether it does depends on which test class
ran first -- so the failure is intermittent and reads as a template bug rather
than a test bug. It surfaced as `Sequence contains more than one element` from a
`Single()` that had been correct in isolation.

The exclusion belongs in the shared helper rather than in the one test that
noticed, because every other file assertion had the same latent problem.

## dotnet-ef is a pinned local tool

`.config/dotnet-tools.json` pins `dotnet-ef`, and the scripts run it with
`dotnet tool run dotnet-ef` after `dotnet tool restore`.

A global tool was the obvious choice and the wrong one. `dotnet tool install
--global` puts the executable in `~/.dotnet/tools`, which is not on PATH until
the shell restarts. So on a clean machine, or after wiping the tools folder, the
script installed dotnet-ef and then immediately failed with "dotnet-ef does not
exist" -- an error that reads like a missing dependency when the dependency had
just been installed.

A local tool needs no PATH entry, restores with the repository, and pins the
version so migrations are generated by the same EF Core the project references.

## Local packs are versioned above released ones

`Install-TemplateLocally.ps1` packs as `9999.0.0-dev.<timestamp>`.

It used `0.0.0-dev.<timestamp>`, which sorts BELOW anything on nuget.org. Once
the package was published, every local install printed "An update for template
package is available" and suggested replacing the working copy with the released
version -- exactly backwards while developing the template.

The timestamp is still there, and still load-bearing: NuGet caches by id and
version, so a fixed local version can serve stale content after a template edit.

## Login identifiers are nullable

`LoginInputModel.Email` and `.UserName` are both `string?`.

ASP.NET Core adds an **implicit `[Required]`** to every non-nullable reference
type property. The login form renders one identifier or the other, never both,
so the one that is not rendered posts nothing and fails that implicit rule --
producing a validation error about a field the user was never shown. Flipping
`SignInWithEmail` to false therefore broke every sign-in, and the only fix at the
call site was `ModelState.Remove(nameof(input.Email))`, which is exactly the kind
of scattered patching the seam exists to prevent.

Making both nullable removes the implicit requirement and leaves
`IValidatableObject.Validate` as the single place deciding which identifier is
needed. A test asserts `AccountController` contains no `ModelState.Remove`: if
one is ever needed again, the validation rules are wrong somewhere else.

## The database is named after the project

`appsettings.json` holds
`CONNECTION-PREFIX<sourceName>CONNECTION-SUFFIX`, with only the surrounding
fragments generated per provider.

The name has to be literal file content because `sourceName` replacement rewrites
the file's own text, and the engine does not re-scan what a generated symbol just
inserted. A project name written inside the generator value would have survived
as `StarterAspMVCEditorTemplates` in every generated project -- so every app on a
machine would share one LocalDB database and silently overwrite each other's data.
Splitting into prefix and suffix keeps the name in the file and avoids depending
on the order two replacements happen to run in.
