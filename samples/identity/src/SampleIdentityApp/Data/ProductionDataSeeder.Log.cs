namespace SampleIdentityApp.Data;

public sealed partial class ProductionDataSeeder
{
    private static partial class Log
    {
        [LoggerMessage(EventId = 1012, Level = LogLevel.Information, Message = "Created new user: {User} in {Roles}")]
        public static partial void CreatedNewUser(ILogger logger, string user, string roles);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Error, Message = "Failed to create user: {User}")]
        public static partial void FailedUserCreation(ILogger logger, string user);
    }
}
