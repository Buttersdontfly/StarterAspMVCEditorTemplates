using System.Diagnostics;
using Xunit;

namespace StarterAspMVCEditorTemplates.TemplateTests;

/// <summary>
/// Packs and installs the template once, then generates both combos.
///
/// The install is isolated with DOTNET_CLI_HOME pointed at a temp directory, so
/// running these tests does not disturb whatever version of the template the
/// developer has installed on their machine. Template installs are otherwise
/// machine-global, and a test run that clobbered the developer's install would
/// be a genuinely unpleasant surprise.
/// </summary>
public sealed class TemplateFixture : IDisposable
{
    public string RepoRoot { get; }
    public string IdentityOutput { get; }
    public string PlainOutput { get; }

    /// <summary>
    /// SQL Server combo, generated and built but never run. Building is enough to
    /// catch what actually breaks per provider -- a migration that does not
    /// compile, or a conditional that leaves the wrong provider wired up. Running
    /// it would need LocalDB, which is Windows only, and that cost buys very
    /// little: the application code is identical across providers.
    /// </summary>
    public string SqlServerOutput { get; }

    /// <summary>Identity plus a peppered password hasher.</summary>
    public string PepperOutput { get; }

    /// <summary>Identity, pepper, and encrypted personal data columns.</summary>
    public string ProtectedOutput { get; }

    private readonly string _workRoot;
    private readonly string _cliHome;

    public TemplateFixture()
    {
        RepoRoot = FindRepoRoot();
        _workRoot = Path.Combine(Path.GetTempPath(), $"tmpl-{Guid.NewGuid():N}");
        _cliHome = Path.Combine(_workRoot, "cli-home");
        Directory.CreateDirectory(_cliHome);

        // Any MSBuild node still running from a previous test run holds locks on
        // task assemblies in the NuGet package folder. Shut them down before
        // starting, or the first restore can fail with "access is denied".
        ShutDownBuildServers();

        var artifacts = Path.Combine(_workRoot, "artifacts");
        var version = $"0.0.0-test.{DateTime.UtcNow:yyyyMMddHHmmss}";

        Run("dotnet", $"pack src/StarterAspMVCEditorTemplates.Templates -o \"{artifacts}\" -p:MinVerVersionOverride={version}", RepoRoot);

        var nupkg = Directory.GetFiles(artifacts, "*.nupkg").Single();
        Run("dotnet", $"new install \"{nupkg}\"", RepoRoot);

        IdentityOutput = Path.Combine(_workRoot, "identity");
        PlainOutput = Path.Combine(_workRoot, "plain");

        Run("dotnet", $"new starterasp-mvc -n IdentityApp -o \"{IdentityOutput}\" --auth identity --no-restore", RepoRoot);
        Run("dotnet", $"new starterasp-mvc -n PlainApp -o \"{PlainOutput}\" --auth none --no-restore", RepoRoot);

        SqlServerOutput = Path.Combine(_workRoot, "sqlserver");
        Run("dotnet", $"new starterasp-mvc -n SqlServerApp -o \"{SqlServerOutput}\" --auth identity --database sqlserver --no-restore", RepoRoot);

        PepperOutput = Path.Combine(_workRoot, "pepper");
        Run("dotnet", $"new starterasp-mvc -n PepperApp -o \"{PepperOutput}\" --auth pepper --no-restore", RepoRoot);

        ProtectedOutput = Path.Combine(_workRoot, "protected");
        Run("dotnet", $"new starterasp-mvc -n ProtectedApp -o \"{ProtectedOutput}\" --auth protected --no-restore", RepoRoot);
    }

    /// <summary>Runs a command, returning stdout+stderr. Throws on failure.</summary>
    public string Run(string fileName, string arguments, string workingDirectory, bool throwOnError = true)
    {
        var info = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        ConfigureEnvironment(info);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException($"Could not start {fileName}.");

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"`{fileName} {arguments}` exited with {process.ExitCode}.\n\n{output}");
        }

        return output;
    }

    public int RunForExitCode(string fileName, string arguments, string workingDirectory, out string output)
    {
        var info = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        ConfigureEnvironment(info);

        using var process = Process.Start(info)!;
        output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>
    /// Environment for every child dotnet process.
    /// </summary>
    /// <remarks>
    /// Node reuse and the MSBuild server are switched off deliberately.
    ///
    /// MSBuild keeps worker nodes alive between builds, and those nodes hold
    /// task assemblies loaded. `Microsoft.AspNetCore.Mvc.Testing.Tasks.dll` is
    /// one such assembly, referenced by the test project inside generated
    /// output. A node left over from a previous run keeps a file lock on it in
    /// the NuGet package folder, and the next restore fails with
    /// "Access to the path ... is denied" -- an error that looks like a
    /// permissions problem and is really a stale process.
    ///
    /// Leaving nodes running between test runs also makes the failure
    /// intermittent, which is worse than a consistent one.
    /// </remarks>
    private void ShutDownBuildServers()
    {
        try
        {
            var info = new ProcessStartInfo("dotnet", "build-server shutdown")
            {
                WorkingDirectory = RepoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            info.Environment["DOTNET_CLI_HOME"] = _cliHome;

            using var process = Process.Start(info);
            process?.WaitForExit(30_000);
        }
        catch
        {
            // Best effort. If this fails the build may still succeed, and a
            // clearer error will come from the build itself.
        }
    }

    private void ConfigureEnvironment(ProcessStartInfo info)
    {
        info.Environment["DOTNET_CLI_HOME"] = _cliHome;
        info.Environment["DOTNET_NOLOGO"] = "true";
        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        info.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "StarterAspMVCEditorTemplates.Templates")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
    }

    public void Dispose()
    {
        try { Directory.Delete(_workRoot, recursive: true); }
        catch (IOException) { /* best effort */ }
    }
}

[CollectionDefinition("template")]
public sealed class TemplateCollection : ICollectionFixture<TemplateFixture>;
