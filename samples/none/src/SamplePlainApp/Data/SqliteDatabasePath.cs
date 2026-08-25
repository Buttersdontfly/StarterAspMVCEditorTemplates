using Microsoft.Data.Sqlite;

namespace SamplePlainApp.Data;

/// <summary>
/// SEAM: database provider.
///
/// SQLite-specific. Delete this file when you swap providers -- a server-based provider needs none of it.
/// </summary>
public static class SqliteDatabasePath
{
    /// <summary>
    /// Rewrites a SQLite connection string so its Data Source is an absolute
    /// path under <paramref name="contentRootPath"/>, and creates the containing
    /// directory. In-memory databases are returned unchanged.
    /// </summary>
    public static string Resolve(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        // ":memory:" and shared-cache in-memory names have no file on disk.
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || builder.DataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || builder.Mode == SqliteOpenMode.Memory)
        {
            return connectionString;
        }

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
