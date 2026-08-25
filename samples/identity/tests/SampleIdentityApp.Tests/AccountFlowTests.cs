using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SampleIdentityApp.Data;
using SampleIdentityApp.Identity;
using SampleIdentityApp.Services;
using System.Net;
using Xunit;

namespace SampleIdentityApp.Tests;

/// <summary>
/// Walks the account flows end to end against a real HTTP pipeline.
///
/// The password reset test is the valuable one: it covers token generation, the
/// fake email sender, the link format, token round-tripping through the URL, and
/// the reset itself. That chain has several places where an encoding mistake
/// produces "invalid token" and no other clue.
/// </summary>
public class AccountFlowTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private HttpClient NewClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = true
    });

    private static string UniqueEmail() => $"testuser-{Guid.NewGuid():N}@example.com";
    private static string UniqueUsername() => $"testuser-{Guid.NewGuid():N}";

    private async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, Dictionary<string, string> fields)
    {
        var page = await HtmlPage.ReadAsync(await client.GetAsync(url));
        fields["__RequestVerificationToken"] = HtmlPage.AntiforgeryToken(page);
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    [Fact]
    public async Task Seeded_user_can_sign_in()
    {
        var client = NewClient();

        var response = await PostFormAsync(client, "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = DevelopmentDataSeeder.DevUserEmail,
            ["Password"] = DevelopmentDataSeeder.DevUserPassword,
            ["RememberMe"] = "false"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var protectedPage = await client.GetAsync("/Account/ChangePassword", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, protectedPage.StatusCode);
    }

    [Fact]
    public async Task Registration_creates_an_account_and_signs_in()
    {
        var client = NewClient();
        var email = UniqueEmail();

        var response = await PostFormAsync(client, "/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "123User!",
            ["ConfirmPassword"] = "123User!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var protectedPage = await client.GetAsync("/Account/ChangePassword", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, protectedPage.StatusCode);
    }

    [Fact]
    public async Task Registration_rejects_mismatched_passwords()
    {
        var client = NewClient();

        var response = await PostFormAsync(client, "/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = UniqueEmail(),
            ["Password"] = "123User!",
            ["ConfirmPassword"] = "SomethingElse1!"
        });

        var document = await HtmlPage.ReadAsync(response);
        Assert.Contains("do not match", document.Body!.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Password_can_be_reset_through_the_emailed_link()
    {
        var client = NewClient();
        var email = UniqueEmail();
        var username = AccountIdentityConventions.SignInWithEmail ? email : UniqueUsername();
        const string newPassword = "Replaced123!";

        await PostFormAsync(client, "/Account/Register", new Dictionary<string, string>
        {
            ["UserName"] = username,
            ["Email"] = email,
            ["Password"] = "123User!",
            ["ConfirmPassword"] = "123User!"
        });

        var anonymous = NewClient();

        await PostFormAsync(anonymous, "/Account/ForgotPassword", new Dictionary<string, string>
        {
            ["Email"] = email
        });

        var sender = factory.Services.GetRequiredService<IAppEmailSender>();
        var message = sender.Sent.LastOrDefault(m => m.To == email);
        Assert.NotNull(message);

        var link = ExtractLink(message!.HtmlBody);
        Assert.Contains("ResetPassword", link);

        var resetPage = await anonymous.GetAsync(link, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, resetPage.StatusCode);

        var document = await HtmlPage.ReadAsync(resetPage);
        var token = document.QuerySelector("input[name='Token']")?.GetAttribute("value");
        Assert.False(string.IsNullOrEmpty(token),
            "The reset page rendered no token, so the link did not carry one through.");

        var reset = await anonymous.PostAsync("/Account/ResetPassword", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = newPassword,
                ["ConfirmPassword"] = newPassword,
                ["Token"] = token!,
                ["__RequestVerificationToken"] = HtmlPage.AntiforgeryToken(document)
            }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // The new password works...
        var afterReset = NewClient();
        await PostFormAsync(afterReset, "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = newPassword,
            ["RememberMe"] = "false"
        });
        Assert.Equal(HttpStatusCode.OK,
            (await afterReset.GetAsync("/Account/ChangePassword", TestContext.Current.CancellationToken)).StatusCode);

        // ...and the old one does not.
        var withOldPassword = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        await PostFormAsync(withOldPassword, "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "123User!",
            ["RememberMe"] = "false"
        });
        var stillProtected = await withOldPassword.GetAsync("/Account/ChangePassword", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, stillProtected.StatusCode);

        CleanTestMail(factory, $"*{DevelopmentDataSeeder.DevUserEmail.Replace('@', '-')}*");
    }

    [Fact]
    public async Task Forgot_password_does_not_reveal_whether_an_account_exists()
    {
        var client = NewClient();

        var response = await PostFormAsync(client, "/Account/ForgotPassword", new Dictionary<string, string>
        {
            ["Email"] = "definitely-not-registered@example.com"
        });

        // Same confirmation page as a real address, and no email sent.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sender = factory.Services.GetRequiredService<IAppEmailSender>();
        Assert.DoesNotContain(sender.Sent, m => m.To == "definitely-not-registered@example.com");
    }

    [Fact]
    public async Task Mailbox_page_lists_sent_messages()
    {
        var client = NewClient();

        await PostFormAsync(client, "/Account/ForgotPassword", new Dictionary<string, string>
        {
            ["Email"] = DevelopmentDataSeeder.DevUserEmail
        });

        var mailbox = await client.GetAsync("/dev/mailbox, TestContext.Current.CancellationToken");
        Assert.Equal(HttpStatusCode.OK, mailbox.StatusCode);

        var document = await HtmlPage.ReadAsync(mailbox);
        Assert.Contains("Reset your password", document.Body!.TextContent);

        CleanTestMail(factory, $"*{DevelopmentDataSeeder.DevUserEmail.Replace('@', '-')}*");
    }

    private static string ExtractLink(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(html, "href=[\"']([^\"']+)[\"']");
        Assert.True(match.Success, "No link found in the email body.");
        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static void CleanTestMail(TestWebAppFactory factory, string pattern)
    {
        var mailDirectory = Path.Combine(factory.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().ContentRootPath, "App_Data", "mail");
        foreach (var file in Directory.EnumerateFiles(mailDirectory, pattern))
        {
            File.Delete(file);
        }
    }
}
