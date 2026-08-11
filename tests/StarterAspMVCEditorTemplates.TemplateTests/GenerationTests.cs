using Xunit;

namespace StarterAspMVCEditorTemplates.TemplateTests;

/// <summary>
/// Asserts what each combo produces.
///
/// The failure this exists to catch: a mistyped path in template.json's exclude
/// list does NOT fail generation. The engine excludes nothing, the file ships in
/// the wrong combo, and the first symptom is a compile error about a missing
/// type -- or worse, no symptom at all until a user reports it.
/// </summary>
[Collection("template")]
public class GenerationTests(TemplateFixture fixture)
{
    private static string[] Files(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .ToArray();

    [Theory]
    [InlineData("Controllers/AccountController.cs")]
    [InlineData("Identity/AccountIdentityConventions.cs")]
    [InlineData("Data/SeedData.cs")]
    [InlineData("Views/Account/Login.cshtml")]
    [InlineData("Views/Shared/_LoginPartial.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/UserName.cshtml")]
    [InlineData("Services/IdentityEmailSenderAdapter.cs")]
    [InlineData("Migrations")]
    public void Identity_only_files_are_present_with_identity(string relativePath)
    {
        var files = Files(fixture.IdentityOutput);
        Assert.Contains(files, f => f.Contains(relativePath, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Controllers/AccountController.cs")]
    [InlineData("Identity/AccountIdentityConventions.cs")]
    [InlineData("Data/SeedData.cs")]
    [InlineData("Views/Account/")]
    [InlineData("Views/Shared/_LoginPartial.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/UserName.cshtml")]
    [InlineData("Services/IdentityEmailSenderAdapter.cs")]
    [InlineData("Migrations/")]
    public void Identity_only_files_are_absent_without_identity(string relativePath)
    {
        var files = Files(fixture.PlainOutput);
        var leaked = files.Where(f => f.Contains(relativePath, StringComparison.Ordinal)).ToList();

        Assert.True(leaked.Count == 0,
            $"'{relativePath}' should be excluded by --auth none but was generated:\n"
            + string.Join("\n", leaked));
    }

    [Theory]
    [InlineData("Views/Shared/EditorTemplates/EmailAddress.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Password.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/AddressInputModel.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/PersonNameInputModel.cshtml")]
    [InlineData("Controllers/DevController.cs")]
    [InlineData("Data/AppDbContext.cs")]
    [InlineData("Services/DevConsoleEmailSender.cs")]
    public void Shared_files_are_present_in_both_combos(string relativePath)
    {
        Assert.Contains(Files(fixture.IdentityOutput), f => f.Contains(relativePath, StringComparison.Ordinal));
        Assert.Contains(Files(fixture.PlainOutput), f => f.Contains(relativePath, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Source_name_is_replaced_everywhere(bool identity)
    {
        var root = identity ? fixture.IdentityOutput : fixture.PlainOutput;
        var expected = identity ? "IdentityApp" : "PlainApp";

        var offenders = Files(root)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".csproj") || f.EndsWith(".slnx"))
            .Where(f => File.ReadAllText(f).Contains("StarterAspMVCEditorTemplates", StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"sourceName was not replaced with '{expected}' in:\n" + string.Join("\n", offenders));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void No_template_conditionals_survive_generation(bool identity)
    {
        var root = identity ? fixture.IdentityOutput : fixture.PlainOutput;

        var offenders = new List<string>();
        foreach (var file in Files(root).Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".csproj") || f.EndsWith(".slnx")))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("#if") || trimmed.StartsWith("#endif")
                    || trimmed.StartsWith("<!--#if") || trimmed.StartsWith("<!--#endif"))
                {
                    offenders.Add($"{file}:{i + 1}: {trimmed}");
                }
            }
        }

        Assert.True(offenders.Count == 0,
            "Unprocessed template conditionals:\n" + string.Join("\n", offenders));
    }

    /// <summary>
    /// The seam invariant, enforced. Controllers and views must never touch
    /// IdentityUser.UserName -- everything goes through
    /// AccountIdentityConventions. If that stops being true, separating username
    /// from email stops being a three-step edit.
    /// </summary>
    /// <summary>
    /// The seam invariant: exactly one file may couple to Identity's login
    /// identifier.
    ///
    /// The rule is about IdentityUser.UserName, not the word "UserName". The
    /// login and register views legitimately bind `m => m.UserName` on the INPUT
    /// MODEL -- that is the field the user types into, and it is precisely what
    /// the seam is meant to make easy to add. What must not spread is
    /// constructing an IdentityUser, or reading its UserName property, anywhere
    /// but AccountIdentityConventions.
    /// </summary>
    [Fact]
    public void Identity_user_is_only_constructed_by_the_conventions_class()
    {
        var offenders = Files(fixture.IdentityOutput)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml"))
            .Where(f => !f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal))
            .Where(f => !f.Contains("/Migrations/", StringComparison.Ordinal))
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(
                StripComments(File.ReadAllText(f)), @"new\s+IdentityUser"))
            .ToList();

        Assert.True(offenders.Count == 0,
            "IdentityUser is constructed outside AccountIdentityConventions.cs. Route it through "
            + "CreateUser instead, or flipping SignInWithEmail stops being a one-line change:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// No C# outside the conventions class may read or assign the Identity
    /// user's UserName. Views are exempt from this particular rule because in a
    /// view `.UserName` refers to the input model, checked above instead.
    /// </summary>
    [Fact]
    public void Identity_username_property_is_only_touched_by_the_conventions_class()
    {
        var offenders = Files(fixture.IdentityOutput)
            .Where(f => f.EndsWith(".cs"))
            .Where(f => !f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal))
            .Where(f => !f.Contains("/Migrations/", StringComparison.Ordinal))
            // Input models DECLARE a UserName property; that is the field the
            // user fills in, not Identity's.
            .Where(f => !f.Contains("/Models/Account/", StringComparison.Ordinal))
            .Where(f => References(StripComments(File.ReadAllText(f))))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Identity's UserName is touched outside AccountIdentityConventions.cs:\n"
            + string.Join("\n", offenders));
    }

    private static bool References(string code) =>
        System.Text.RegularExpressions.Regex.IsMatch(code, @"\.UserName\b|\bUserName\s*=[^=]");

    /// <summary>
    /// Removes comments before checking for UserName.
    ///
    /// The seam is documented IN comments -- the commented-out alternative in
    /// the input models, the EditorFor line in the login and register views, the
    /// note in AccountController saying it deliberately never touches UserName.
    /// Matching those would make the test fire on its own documentation, which
    /// would train everyone to ignore it.
    /// </summary>
    private static string StripComments(string code)
    {
        code = System.Text.RegularExpressions.Regex.Replace(code, @"@\*.*?\*@", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);   // Razor
        code = System.Text.RegularExpressions.Regex.Replace(code, @"/\*.*?\*/", "",
            System.Text.RegularExpressions.RegexOptions.Singleline);   // C# block
        code = System.Text.RegularExpressions.Regex.Replace(code, @"//[^\r\n]*", "");  // C# line
        return code;
    }


    /// <summary>
    /// No file that survives --auth none may reference an Identity-only
    /// namespace or type.
    ///
    /// This is the failure mode that whole-file exclusion invites: the excluded
    /// files are correct, but a SHARED file still points at them. Generation
    /// succeeds, the file list is exactly right, and the combo fails to compile
    /// with an error naming the shared file rather than the exclusion. The
    /// generation assertions cannot see it, because nothing is missing or extra.
    /// </summary>
    [Theory]
    [InlineData("Models.Account")]
    [InlineData("StarterAspMVCEditorTemplates.Identity")]
    [InlineData("AccountIdentityConventions")]
    [InlineData("SeedData")]
    [InlineData("IdentityEmailSenderAdapter")]
    public void Plain_combo_never_references_identity_only_code(string identityOnlyReference)
    {
        // sourceName has been replaced by now, so match on the tail of the
        // namespace rather than the original project name.
        var needle = identityOnlyReference.Replace("StarterAspMVCEditorTemplates.", "");

        // Comments are stripped: several shared files legitimately MENTION the
        // Identity-only pieces while explaining why they do not depend on them.
        var offenders = Files(fixture.PlainOutput)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml"))
            .Where(f => StripComments(File.ReadAllText(f)).Contains(needle, StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"--auth none output references '{needle}', which that combo excludes:\n"
            + string.Join("\n", offenders));
    }

    /// <summary>
    /// The shared view imports must not pull in a namespace that only exists in
    /// one combo. Asserted directly because _ViewImports.cshtml applies to every
    /// view, so a bad using there breaks the whole combo at once.
    /// </summary>
    [Fact]
    public void Shared_view_imports_are_combo_agnostic()
    {
        var viewImports = Files(fixture.PlainOutput)
            .Single(f => f.EndsWith("Views/_ViewImports.cshtml", StringComparison.Ordinal));

        var text = StripComments(File.ReadAllText(viewImports));
        Assert.DoesNotContain("Models.Account", text);
    }


    /// <summary>
    /// Metadata placeholders must be filled in before anything is published.
    /// Author, copyright and repository URL are baked into the package, and a
    /// package on nuget.org authored by "__AUTHOR__" is not easily undone --
    /// package versions cannot be replaced, only unlisted.
    /// </summary>
    [Fact]
    public void No_metadata_placeholders_remain()
    {
        var files = new[] { "Directory.Build.props", "LICENSE", "README.md" }
            .Select(f => Path.Combine(fixture.RepoRoot, f))
            .Where(File.Exists);

        var offenders = files
            .Where(f => File.ReadAllText(f).Contains("__AUTHOR__", StringComparison.Ordinal)
                     || File.ReadAllText(f).Contains("__GITHUB_USER__", StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0,
            "Metadata placeholders are still present. Run build/Set-Metadata.ps1:\n"
            + string.Join("\n", offenders));
    }


    /// <summary>
    /// The seam must remain a ONE-LINE change. If someone reintroduces
    /// commented-out alternative implementations, flipping the constant stops
    /// being sufficient and the seam quietly becomes advisory again -- which is
    /// exactly what this design replaced.
    /// </summary>
    [Fact]
    public void Seam_has_no_commented_out_alternative_implementations()
    {
        var conventions = Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal));

        var text = File.ReadAllText(conventions);

        // A commented-out body is the tell: "// return new" or "// await userManager".
        var suspicious = System.Text.RegularExpressions.Regex.Matches(
                text, @"//\s*(return|await|new\s+IdentityUser|userManager\.)")
            .Select(m => m.Value.Trim())
            .ToList();

        Assert.True(suspicious.Count == 0,
            "AccountIdentityConventions contains commented-out implementations. The constant should "
            + "switch behaviour on its own:\n" + string.Join("\n", suspicious));
    }

    /// <summary>
    /// Both branches must actually exist, so flipping the constant reaches real
    /// code rather than a stub.
    /// </summary>
    [Fact]
    public void Seam_implements_both_username_modes()
    {
        var conventions = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal)));

        Assert.Contains("FindByEmailAsync", conventions);   // email mode
        Assert.Contains("FindByNameAsync", conventions);    // separated mode
        Assert.Contains("SignInWithEmail", conventions);
    }


    /// <summary>
    /// SignInWithEmail must not be a `const`.
    ///
    /// A const bool is folded at compile time, making every branch for the other
    /// mode unreachable. CS0162 plus TreatWarningsAsErrors then fails the build
    /// -- in the generated Razor views as well as the conventions class. The
    /// seam would compile in only one of its two modes, which defeats it.
    ///
    /// This looks like a style nit and is not: it is the difference between the
    /// flip working and the project not building.
    /// </summary>
    [Fact]
    public void Sign_in_convention_is_not_a_compile_time_constant()
    {
        var conventions = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal)));

        Assert.DoesNotContain("const bool SignInWithEmail", conventions);
        Assert.Contains("static readonly bool SignInWithEmail", conventions);
    }

    [Theory]
    [InlineData("SEAM: database provider")]
    [InlineData("SEAM: username identity")]
    [InlineData("SEAM: email sender")]
    public void Documented_seams_exist_in_generated_output(string seam)
    {
        var found = Files(fixture.IdentityOutput)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".cshtml") || f.EndsWith(".props"))
            .Any(f => File.ReadAllText(f).Contains(seam, StringComparison.Ordinal));

        Assert.True(found, $"'{seam}' is documented in seams.md but no longer appears in generated output.");
    }
}
