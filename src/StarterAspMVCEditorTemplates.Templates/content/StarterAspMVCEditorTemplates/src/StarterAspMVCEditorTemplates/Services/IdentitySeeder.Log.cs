namespace StarterAspMVCEditorTemplates.Services;

internal sealed partial class IdentitySeeder
{
    private static partial class Log
    {
        [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Created new role: {Role}")]
        public static partial void CreatedNewRole(ILogger logger, string role);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Failed to create role: {Role}")]
        public static partial void FailedRoleCreation(ILogger logger, string role);

        [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Created new user: {User}")]
        public static partial void CreatedNewUser(ILogger logger, string user);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Error, Message = "Failed to create user: {User}")]
        public static partial void FailedUserCreation(ILogger logger, string user);
    }
}
