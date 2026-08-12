using System.Net;
using Xunit;

namespace SampleIdentityApp.Tests;

public class SmokeTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task Home_page_loads()
    {
        var response = await factory.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_page_loads()
    {
        var response = await factory.CreateClient().GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Change_password_requires_signing_in()
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Account/ChangePassword");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? "");
    }
}
