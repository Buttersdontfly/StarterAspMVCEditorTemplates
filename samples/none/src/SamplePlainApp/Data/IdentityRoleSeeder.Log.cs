namespace SamplePlainApp.Data;

internal sealed partial class IdentityRoleSeeder
{
    private static partial class Log
    {
        [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Created new role: {Role}")]
        public static partial void CreatedNewRole(ILogger logger, string role);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "The {Role} role was created concurrently.")]
        public static partial void UniqueKeyViolation(ILogger logger, string role);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Failed to create role: {Role} - {Error}")]
        public static partial void FailedRoleCreation(ILogger logger, string role, string error);
    }
}
