using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SampleIdentityApp.Services;

/// <summary>
/// SEAM: email sender.
///
/// Development stand-in. Writes each message three ways:
///   1. to the console, with any link pulled out so it is easy to click;
///   2. to an .eml file under App_Data/mail/, openable in a mail client;
///   3. to the in-memory list behind /dev/mailbox.
///
/// Not for production: it stores message bodies in memory and on disk in the
/// clear. Replace the registration in Program.cs before you ship.
/// </summary>
public sealed partial class DevConsoleEmailSender : IAppEmailSender
{
    private readonly ILogger<DevConsoleEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    private const int MaxRetained = 50;

    public DevConsoleEmailSender(ILogger<DevConsoleEmailSender> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public IReadOnlyList<SentEmail> Sent => _sent.ToArray();

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = new SentEmail(DateTimeOffset.Now, to, subject, htmlBody);

        _sent.Enqueue(message);
        while (_sent.Count > MaxRetained && _sent.TryDequeue(out _)) { }

        var link = FirstHref(htmlBody);

        _logger.LogWarning(
            "Email not sent — no email provider is configured.\n" +
            "  To:      {To}\n" +
            "  Subject: {Subject}\n" +
            "  Link:    {Link}\n" +
            "  All messages: /dev/mailbox",
            to, subject, link ?? "(none)");

        await WriteEmlAsync(message, cancellationToken);
    }

    private async Task WriteEmlAsync(SentEmail message, CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.Combine(_environment.ContentRootPath, "App_Data", "mail");
            Directory.CreateDirectory(directory);

            var fileName = $"{message.SentAt:yyyyMMdd-HHmmss-fff}-{Sanitise(message.To)}.eml";

            // A plain-text copy of the link goes in a header so it survives being
            // read as text rather than rendered as HTML, for the same reason the
            // console line is decoded.
            var link = FirstHref(message.HtmlBody);

            var builder = new StringBuilder()
                .AppendLine($"Date: {message.SentAt:R}")
                .AppendLine($"To: {message.To}")
                .AppendLine("From: no-reply@localhost")
                .AppendLine($"Subject: {message.Subject}")
                .AppendLine("MIME-Version: 1.0")
                .AppendLine($"X-Dev-Link: {link ?? "(none)"}")
                .AppendLine("Content-Type: text/html; charset=utf-8")
                .AppendLine()
                .AppendLine(message.HtmlBody);

            await File.WriteAllTextAsync(Path.Combine(directory, fileName), builder.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            // Writing the .eml is a convenience, not part of the flow — never
            // fail a registration because the disk was not writable.
            _logger.LogDebug(ex, "Could not write the .eml file.");
        }
    }

    private static string Sanitise(string value) =>
        string.Concat(value.Select(c => char.IsLetterOrDigit(c) ? c : '-'));

    /// <summary>
    /// Pulls the first link out of an HTML body for the console line.
    /// </summary>
    /// <remarks>
    /// HtmlDecode is essential, not cosmetic. The href in the body is HTML
    /// encoded, so an ampersand between query parameters is written &amp;amp;.
    /// A browser decodes that on click, which is why the link on /dev/mailbox
    /// works -- but text copied straight out of the console does not go through
    /// a browser, so the server receives a parameter literally named
    /// "amp;token", the real token binds as null, and the reset fails with
    /// "invalid token".
    /// </remarks>
    private static string? FirstHref(string html) =>
        HrefPattern().Match(html) is { Success: true } match
            ? WebUtility.HtmlDecode(match.Groups[1].Value)
            : null;

    [GeneratedRegex("href=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex HrefPattern();
}
