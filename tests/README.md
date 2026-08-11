# tests

Tests for the **template itself**. Not to be confused with the test project that
ships inside generated output.

## StarterAspMVCEditorTemplates.TemplateTests

Packs, installs and generates both combos once per run, then asserts on the
result:

- **`GenerationTests`** -- which files each combo produces, `sourceName`
  replacement, no surviving conditionals, the `UserName` seam invariant, and that
  every documented seam still exists in the output.
- **`BuildTests`** -- both combos compile with `--warnaserror`.

The install is isolated with `DOTNET_CLI_HOME`, so a test run cannot disturb the
template version installed on your machine.

Requires the repository to be initialised first (`build/Initialize-Repo.ps1`),
since the Identity combo expects `Migrations/` to be present.

## Tests inside the template

`src/.../content/.../tests/` ships with generated projects and runs against the
generated app: account flows end to end, and the editor template gallery. CI runs
those with `dotnet test` on the generated output.
