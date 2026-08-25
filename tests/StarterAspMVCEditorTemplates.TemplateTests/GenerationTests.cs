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
    /// <summary>
    /// Every file the template GENERATED, excluding build output.
    /// </summary>
    /// <remarks>
    /// bin and obj must be excluded here rather than in individual tests.
    /// BuildTests shares this fixture and may run first, and a build copies
    /// content files such as appsettings.Development.json into bin. Any
    /// assertion counting or matching files then sees each one twice, and
    /// whether it does depends on test order -- so the failure is intermittent
    /// and looks like a template bug rather than a test bug.
    /// </remarks>
    private static string[] Files(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Select(p => p.Replace('\\', '/'))
            .Where(p => !p.Contains("/bin/", StringComparison.Ordinal)
                     && !p.Contains("/obj/", StringComparison.Ordinal))
            .ToArray();

    [Theory]
    [InlineData("Controllers/AccountController.cs")]
    [InlineData("Identity/AccountIdentityConventions.cs")]
	[InlineData("Constants/Roles.cs")]
	[InlineData("Utilities/ReflectionHelper.cs")]
    [InlineData("Data/IDataInitializer.cs")]
	[InlineData("Data/DevelopmentDataSeeder.cs")]
	[InlineData("Data/DevelopmentDataSeeder.Log.cs")]
	[InlineData("Data/IdentityRoleSeeder.cs")]
	[InlineData("Data/IdentityRoleSeeder.Log.cs")]
	[InlineData("Data/ProductionDataSeeder.cs")]
	[InlineData("Data/ProductionDataSeeder.Log.cs")]
    [InlineData("Views/Account/Login.cshtml")]
    [InlineData("Views/Shared/_LoginPartial.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/UserName.cshtml")]
    [InlineData("Services/IdentityEmailSenderAdapter.cs")]
    [InlineData("Services/IAppEmailSender.cs")]
    [InlineData("Services/DevConsoleEmailSender.cs")]
    [InlineData("Controllers/DevController.cs")]
    [InlineData("Views/Dev/Editors.cshtml")]
    [InlineData("Views/Dev/Mailbox.cshtml")]
    [InlineData("Views/Shared/_DevNavPartial.cshtml")]
    [InlineData("Views/Home/_DevCards.cshtml")]
    [InlineData("Models/EditorSampleModel.cs")]
    [InlineData("Migrations")]
    public void Identity_only_files_are_present_with_identity(string relativePath)
    {
        var files = Files(fixture.IdentityOutput);
        Assert.Contains(files, f => f.Contains(relativePath, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Controllers/AccountController.cs")]
    [InlineData("Identity/AccountIdentityConventions.cs")]
	[InlineData("Constants/Roles.cs")]
	[InlineData("Utilities/ReflectionHelper.cs")]
    [InlineData("Data/IDataInitializer.cs")]
	[InlineData("Data/DevelopmentDataSeeder.cs")]
	[InlineData("Data/DevelopmentDataSeeder.Log.cs")]
	[InlineData("Data/IdentityRoleSeeder.cs")]
	[InlineData("Data/IdentityRoleSeeder.Log.cs")]
	[InlineData("Data/ProductionDataSeeder.cs")]
	[InlineData("Data/ProductionDataSeeder.Log.cs")]
    [InlineData("Views/Account/")]
    [InlineData("Views/Shared/_LoginPartial.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/UserName.cshtml")]
    [InlineData("Services/")]
    [InlineData("Controllers/DevController.cs")]
    [InlineData("Views/Dev/")]
    [InlineData("Views/Shared/_DevNavPartial.cshtml")]
    [InlineData("Views/Home/_DevCards.cshtml")]
    [InlineData("Models/EditorSampleModel.cs")]
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
    [InlineData("Views/Shared/EditorTemplates/String.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Int32.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Decimal.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Boolean.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Enum.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Dropdown.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/RadioGroup.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/CheckboxList.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Tags.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Color.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Rating.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/Range.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/FileUpload.cshtml")]
    [InlineData("Views/Shared/EditorTemplates/LineItem.cshtml")]
    [InlineData("Models/LineItem.cs")]
    [InlineData("wwwroot/js/editor-templates.js")]
    [InlineData("Data/AppDbContext.cs")]
    [InlineData("Controllers/HomeController.cs")]
    [InlineData("Views/Shared/_Layout.cshtml")]
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
    /// user's UserName.
    ///
    /// Scoped to files that actually work with Identity types. "UserName" is an
    /// ordinary property name that view models and sample data use too: the
    /// gallery sets `UserName = "ada"` on its own sample model, which is not a
    /// seam violation. A file that never mentions IdentityUser cannot be
    /// touching Identity's UserName, so the Identity reference is what makes
    /// this check meaningful rather than a name match.
    ///
    /// Views are out of scope entirely — there `.UserName` is the input model,
    /// covered by the IdentityUser construction test instead.
    /// </summary>
    [Fact]
    public void Identity_username_property_is_only_touched_by_the_conventions_class()
    {
        var offenders = Files(fixture.IdentityOutput)
            .Where(f => f.EndsWith(".cs"))
            .Where(f => !f.EndsWith("AccountIdentityConventions.cs", StringComparison.Ordinal))
            .Where(f => !f.Contains("/Migrations/", StringComparison.Ordinal))
            .Select(f => (Path: f, Code: StripComments(File.ReadAllText(f))))
            .Where(x => x.Code.Contains("IdentityUser", StringComparison.Ordinal))
            .Where(x => References(x.Code))
            .Select(x => x.Path)
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
	[InlineData("Constants/Roles.cs")]
	[InlineData("Utilities/ReflectionHelper.cs")]
    [InlineData("Data/IDataInitializer.cs")]
	[InlineData("Data/DevelopmentDataSeeder.cs")]
	[InlineData("Data/DevelopmentDataSeeder.Log.cs")]
	[InlineData("Data/IdentityRoleSeeder.cs")]
	[InlineData("Data/IdentityRoleSeeder.Log.cs")]
	[InlineData("Data/ProductionDataSeeder.cs")]
	[InlineData("Data/ProductionDataSeeder.Log.cs")]    
    [InlineData("IdentityEmailSenderAdapter")]
    [InlineData("IAppEmailSender")]
    [InlineData("DevConsoleEmailSender")]
    [InlineData("EditorGalleryModel")]
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


    /// <summary>
    /// The plain combo must not link to pages it does not have.
    ///
    /// A dead link compiles perfectly well and only shows up as a 404 when
    /// somebody clicks it, so nothing else in this suite would catch it. The
    /// layout and home page reach the dev pages through optional partials, which
    /// is what makes whole-file exclusion enough here.
    /// </summary>
    [Theory]
    [InlineData("/dev/editors")]
    [InlineData("/dev/mailbox")]
    public void Plain_combo_has_no_links_to_dev_pages(string url)
    {
        var offenders = Files(fixture.PlainOutput)
            .Where(f => f.EndsWith(".cshtml"))
            .Where(f => StripComments(File.ReadAllText(f)).Contains(url, StringComparison.Ordinal))
            .ToList();

        Assert.True(offenders.Count == 0,
            $"--auth none output links to '{url}', which that combo does not serve:\n"
            + string.Join("\n", offenders));
    }

    // Checked against the Identity output: the email sender seam only exists
    // there, since --auth none ships no email services at all.

    /// <summary>
    /// Every editor template must be reachable from the gallery model, or it is
    /// rendered by no test and can break silently.
    ///
    /// Checked structurally rather than by rendering, so it fails with a useful
    /// message the moment a file is added without a matching property.
    /// </summary>
    [Fact]
    public void Every_editor_template_is_covered_by_the_gallery_model()
    {
        var templateDirectory = Path.Combine(
            fixture.IdentityOutput, "src", "IdentityApp", "Views", "Shared", "EditorTemplates");

        var templates = Directory.GetFiles(templateDirectory, "*.cshtml")
            .Select(Path.GetFileNameWithoutExtension)
            .ToList();

        var sampleModel = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("EditorSampleModel.cs", StringComparison.Ordinal)));

        // A template is covered either by a [UIHint] naming it, or by a property
        // whose type resolves to it. Type-name templates are named after the CLR
        // type, which is not what appears in C# source, so those are mapped.
        var clrTypeAliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Boolean"] = "bool",
            ["Int32"] = "int",
            ["Decimal"] = "decimal",
            ["String"] = "string",
            ["Enum"] = "SampleStatus"
        };

        var uncovered = templates
            .Where(name =>
            {
                var needle = clrTypeAliases.TryGetValue(name!, out var alias) ? alias : name!;
                return !sampleModel.Contains(needle, StringComparison.Ordinal);
            })
            .ToList();

        Assert.True(uncovered.Count == 0,
            "These editor templates are not reachable from EditorSampleModel, so no test renders them:\n"
            + string.Join("\n", uncovered)
            + "\n\nAdd a property for each, and list its field name in EditorTemplateTests.ExpectedFields.");
    }


    /// <summary>
    /// The scoping above would be a loophole if the controllers stopped naming
    /// IdentityUser while still reaching Identity through some other route, so
    /// this pins the shape the seam depends on: the account controller works
    /// through UserManager and SignInManager and never constructs or inspects a
    /// user itself.
    /// </summary>
    [Fact]
    public void Account_controller_goes_through_the_conventions_class()
    {
        var controller = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("Controllers/AccountController.cs", StringComparison.Ordinal)));

        var code = StripComments(controller);

        Assert.Contains("AccountIdentityConventions", code);
        Assert.DoesNotContain("new IdentityUser", code);
        Assert.DoesNotContain(".UserName", code);
    }


    /// <summary>
    /// Only the chosen provider's migrations may ship. Both sets in one project
    /// would collide on the model snapshot class, and the wrong set would apply
    /// SQL the other provider cannot execute.
    /// </summary>
    [Fact]
    public void Only_the_chosen_providers_migrations_are_generated()
    {
        var sqlite = Files(fixture.IdentityOutput)
            .Where(f => f.Contains("/Migrations/", StringComparison.Ordinal)).ToList();
        var sqlServer = Files(fixture.SqlServerOutput)
            .Where(f => f.Contains("/Migrations/", StringComparison.Ordinal)).ToList();

        Assert.All(sqlite, f => Assert.DoesNotContain("/SqlServer/", f));
        Assert.All(sqlServer, f => Assert.DoesNotContain("/Sqlite/", f));
        Assert.NotEmpty(sqlite);
        Assert.NotEmpty(sqlServer);
    }

    /// <summary>
    /// SqliteDatabasePath solves a SQLite-only problem, so shipping it with a
    /// server provider would be dead code referencing a package that is not there.
    /// </summary>
    [Fact]
    public void Sqlite_only_helpers_are_absent_from_the_sqlserver_combo()
    {
        Assert.DoesNotContain(Files(fixture.SqlServerOutput),
            f => f.EndsWith("SqliteDatabasePath.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void Connection_string_matches_the_chosen_provider()
    {
        var sqlite = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("src/IdentityApp/appsettings.json", StringComparison.Ordinal)));
        var sqlServer = File.ReadAllText(Files(fixture.SqlServerOutput)
            .Single(f => f.EndsWith("src/SqlServerApp/appsettings.json", StringComparison.Ordinal)));

        Assert.Contains("Data Source=", sqlite);
        Assert.Contains("MSSQLLocalDB", sqlServer);

        foreach (var placeholder in new[] { "CONNECTION-PREFIX", "CONNECTION-SUFFIX" })
        {
            Assert.DoesNotContain(placeholder, sqlite);
            Assert.DoesNotContain(placeholder, sqlServer);
        }

        // The database is named after the project, not after the template.
        // Every generated app sharing one LocalDB database would have them
        // silently overwriting each other's data.
        Assert.Contains("IdentityApp", sqlite);
        Assert.Contains("Database=SqlServerApp;", sqlServer);
        Assert.DoesNotContain("StarterAspMVCEditorTemplates", sqlite);
        Assert.DoesNotContain("StarterAspMVCEditorTemplates", sqlServer);
    }

    [Fact]
    public void Identity_types_are_the_applications_own_and_keyed_on_guid()
    {
        var user = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("Identity/ApplicationUser.cs", StringComparison.Ordinal)));
        var context = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("Data/AppDbContext.cs", StringComparison.Ordinal)));

        Assert.Contains("IdentityUser<Guid>", user);
        Assert.Contains("ApplicationUser, ApplicationRole, Guid", context);
    }


    /// <summary>
    /// Each auth level ships exactly what it needs and nothing more.
    /// </summary>
    [Fact]
    public void Auth_levels_include_the_right_protection_files()
    {
        static bool Has(IEnumerable<string> files, string name) =>
            files.Any(f => f.EndsWith(name, StringComparison.Ordinal));

        var identity = Files(fixture.IdentityOutput);
        var pepper = Files(fixture.PepperOutput);
        var guarded = Files(fixture.ProtectedOutput);

        Assert.False(Has(identity, "PepperedPasswordHasher.cs"), "plain identity must not ship the pepper");
        Assert.True(Has(pepper, "PepperedPasswordHasher.cs"), "pepper level must ship the hasher");
        Assert.True(Has(guarded, "PepperedPasswordHasher.cs"), "protected builds on pepper");

        Assert.False(Has(pepper, "LookupProtector.cs"), "pepper alone must not ship the lookup protector");
        Assert.True(Has(guarded, "LookupProtector.cs"), "protected must ship the lookup protector");
        Assert.True(Has(guarded, "KeyRing.cs"), "protected must ship the key ring");
    }

    /// <summary>
    /// The generated secrets must actually be generated.
    ///
    /// A placeholder surviving into the output would be worse than a missing
    /// value: every project produced by the template would share one pepper, so
    /// anyone with the template would know it, and the pepper would be worth
    /// nothing while appearing to work.
    /// </summary>
    [Theory]
    [InlineData("GENERATED-PEPPER-VALUE")]
    [InlineData("GENERATED-LOOKUP-KEY")]
    public void Generated_secrets_are_replaced(string placeholder)
    {
        foreach (var root in new[] { fixture.PepperOutput, fixture.ProtectedOutput })
        {
            var offenders = Files(root)
                .Where(f => f.EndsWith(".json", StringComparison.Ordinal))
                .Where(f => File.ReadAllText(f).Contains(placeholder, StringComparison.Ordinal))
                .ToList();

            Assert.True(offenders.Count == 0,
                $"'{placeholder}' was not replaced, so every generated project would share this secret:\n"
                + string.Join("\n", offenders));
        }
    }

    /// <summary>
    /// Two projects generated from the same template must not share a pepper.
    /// This is the property that makes generating one worthwhile at all.
    /// </summary>
    [Fact]
    public void Generated_secrets_differ_between_projects()
    {
        static string Secrets(string root) =>
            File.ReadAllText(Files(root)
                .Single(f => f.EndsWith("appsettings.Development.json", StringComparison.Ordinal)));

        Assert.NotEqual(Secrets(fixture.PepperOutput), Secrets(fixture.ProtectedOutput));
    }


    /// <summary>
    /// Login must accept whichever identifier the seam is configured for,
    /// without demanding the other one.
    ///
    /// ASP.NET Core adds an implicit [Required] to non-nullable reference type
    /// properties. With Email non-nullable, switching SignInWithEmail to false
    /// made every sign-in fail on a field the form does not even render, and the
    /// only fix at the call site was a ModelState.Remove -- which is the sort of
    /// thing the seam exists to avoid.
    /// </summary>
    [Fact]
    public void Login_identifiers_are_nullable_so_the_unused_one_is_not_required()
    {
        var model = File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("Models/Account/LoginInputModel.cs", StringComparison.Ordinal)));

        Assert.Contains("string? Email", model);
        Assert.Contains("string? UserName", model);
        Assert.DoesNotContain("public string Email", model);
    }

    /// <summary>
    /// The account controller must not need ModelState surgery to make the seam
    /// work. If a ModelState.Remove appears here, the validation rules are wrong
    /// somewhere else.
    /// </summary>
    [Fact]
    public void Account_controller_does_not_patch_model_state()
    {
        var controller = StripComments(File.ReadAllText(Files(fixture.IdentityOutput)
            .Single(f => f.EndsWith("Controllers/AccountController.cs", StringComparison.Ordinal))));

        Assert.DoesNotContain("ModelState.Remove", controller);
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
