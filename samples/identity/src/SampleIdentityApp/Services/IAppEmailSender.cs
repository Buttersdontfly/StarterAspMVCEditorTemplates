namespace SampleIdentityApp.Services;

/// <summary>
/// SEAM: email sender.
/// The app's own abstraction. Identity talks to this through
/// IdentityEmailSenderAdapter, so swapping in SMTP or a transactional provider
/// is one class plus one registration in Program.cs.
/// </summary>
public interface IAppEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);

    /// <summary>
    /// Messages sent so far this process. Backs the /dev/mailbox page and,
    /// more importantly, makes the password reset flow assertable in tests —
    /// scraping console output is not a testable interface.
    /// </summary>
    IReadOnlyList<SentEmail> Sent { get; }
}

public sealed record SentEmail(
    DateTimeOffset SentAt,
    string To,
    string Subject,
    string HtmlBody);
