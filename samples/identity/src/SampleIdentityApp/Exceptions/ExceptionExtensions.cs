namespace SampleIdentityApp.Exceptions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Matched on the message rather than on a provider-specific exception type,
    /// so this keeps working even if/when the database provider is swapped. The text
    /// differs per provider, hence the several variants.
    /// </summary>
    /// <remarks>
    ///  Providers:
    /// - SQLite: "UNIQUE constraint failed"
    /// - PostgreSQL: "duplicate key"
    /// - SQL Server: "Violation of UNIQUE KEY"
    /// - MySQL / MariaDB: "Duplicate entry"
    /// </remarks>
    public static bool IsUniqueConstraintViolation(this Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;

            if (message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Violation of UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
