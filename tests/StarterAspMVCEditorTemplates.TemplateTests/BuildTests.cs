using Xunit;

namespace StarterAspMVCEditorTemplates.TemplateTests;

/// <summary>
/// Builds both combos. Slower than the generation assertions, and worth it:
/// generation succeeding says nothing about whether the result compiles, and the
/// --auth none combo has no other coverage at all.
/// </summary>
[Collection("template")]
public class BuildTests(TemplateFixture fixture)
{
    // No --warnaserror here, deliberately.
    //
    // The generated project already sets TreatWarningsAsErrors in its own
    // Directory.Build.props, together with WarningsNotAsErrors for the NuGet
    // audit codes NU1901-NU1904. That combination is what we want, and it is
    // what a user gets.
    //
    // Passing --warnaserror on the command line is a different mechanism: it
    // promotes warnings at the MSBuild level, where WarningsNotAsErrors does not
    // apply, so audit findings become errors again and the build fails on a
    // vulnerability in a transitive dependency nobody can fix. Building without
    // the flag tests the real configuration instead of a stricter one that no
    // user will ever run.
    // -nodeReuse:false belongs on the command line as well as in the
    // environment: MSBuild reads them in different places, and a node that
    // outlives this run holds file locks on task assemblies in the NuGet
    // package folder. See TemplateFixture.ConfigureEnvironment.
    private const string BuildArguments = "build -nodeReuse:false";

    [Fact]
    public void Identity_combo_builds_cleanly()
    {
        var exitCode = fixture.RunForExitCode("dotnet", BuildArguments, fixture.IdentityOutput, out var output);
        Assert.True(exitCode == 0, Explain("--auth identity", output));
    }

    [Fact]
    public void Plain_combo_builds_cleanly()
    {
        var exitCode = fixture.RunForExitCode("dotnet", BuildArguments, fixture.PlainOutput, out var output);
        Assert.True(exitCode == 0, Explain("--auth none", output));
    }

    /// <summary>
    /// Adds a hint for failures that are about the machine rather than the
    /// template, so they are not mistaken for template defects.
    /// </summary>
    private static string Explain(string combo, string output)
    {
        var message = $"The {combo} combo failed to build:\n\n{output}";

        if (output.Contains("is denied", StringComparison.OrdinalIgnoreCase)
            && output.Contains(".Tasks.dll", StringComparison.OrdinalIgnoreCase))
        {
            message += "\n\nThis is a file lock, not a template problem: an MSBuild node left over "
                     + "from an earlier build still has the task assembly loaded. Run "
                     + "`dotnet build-server shutdown` and try again.";
        }

        return message;
    }

    /// <summary>
    /// A compiler warning must still fail the build. This asserts the mechanism
    /// itself is intact, so that removing TreatWarningsAsErrors from the
    /// generated project does not silently go unnoticed.
    /// </summary>
    [Fact]
    public void Generated_project_treats_compiler_warnings_as_errors()
    {
        var props = Path.Combine(fixture.IdentityOutput, "Directory.Build.props");
        var text = File.ReadAllText(props);

        Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", text);
        Assert.Contains("NU1903", text);   // audit codes stay warnings
    }
}
