using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace StarterAspMVCEditorTemplates.Tests;

/// <summary>
/// Boots the app for integration tests against a throwaway SQLite file.
///
/// Two details matter and are easy to get wrong:
///
/// 1. The environment is forced to Development. Migrations, the seeded user and
///    the /dev pages are all Development-only, so a test run in any other
///    environment gets an empty database and 404s.
///
/// 2. Each factory gets its own database file. xUnit runs test classes in
///    parallel, so a shared file means several instances migrating and seeding
///    the same database at once -- which surfaces as
///    `UNIQUE constraint failed: AspNetRoles.NormalizedName` from whichever
///    instance loses. Tests would also interfere through the seeded user and any
///    accounts they create, and both failure modes read as flakiness rather than
///    as shared state.
///
///    The app itself is safe under concurrent startup regardless (see the mutex
///    in Program.cs and the idempotent seeder); a separate file per factory keeps
///    the tests independent as well as passing.
/// </summary>
public sealed class TestWebAppFactory : WebApplicationFactory<Program>, IDisposable
{
#if (UseSqlite)
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.db");
#else
    // Each run gets its own LocalDB database, for the same isolation reason as
    // the SQLite file: xUnit runs test classes in parallel.
    private readonly string _databaseName = $"StarterAspMVCEditorTemplatesTest_{Guid.NewGuid():N}";
#endif

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
#if (UseSqlite)
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}"
#else
                ["ConnectionStrings:DefaultConnection"] =
                    $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True"
#endif
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
#if (UseSqlite)
            // SQLite keeps the file handle until pooled connections are cleared.
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-shm", "-wal" })
            {
                var path = _databasePath + suffix;
                if (File.Exists(path))
                {
                    try { File.Delete(path); } catch (IOException) { /* best effort */ }
                }
            }
#else
            // LocalDB databases are left behind on purpose: dropping one needs a
            // connection to master and single-user mode, which is more failure
            // surface than it is worth in a test teardown. Clean up with
            // `sqllocaldb stop MSSQLLocalDB` and deleting %USERPROFILE%\*.mdf,
            // or drop the StarterAspMVCEditorTemplatesTest_* databases.
#endif
        }
    }
}
