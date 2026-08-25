namespace SampleIdentityApp.Data;

public sealed partial class DevelopmentDataSeeder
{
    private static partial class Log
    {
        [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "The development user was created concurrently.")]
        public static partial void ConcurrentDevUserCreation(ILogger logger);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "The development user was already in the {Role} role.")]
        public static partial void DevUserAlreadyInRole(ILogger logger, string role);

        [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Seeded development user {Email} with password {Password}")]
        public static partial void CreatedDevUser(ILogger logger, string email, string password);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Error, Message = "Could not seed the development user: {Errors}")]
        public static partial void FailedDevUserCreation(ILogger logger, string errors);
    }
}
