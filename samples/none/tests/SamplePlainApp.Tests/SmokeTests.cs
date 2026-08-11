using System.Net;
using Xunit;

namespace SamplePlainApp.Tests;

public class SmokeTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    [Fact]
    public async Task Home_page_loads()
    {
        var response = await factory.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Editor_gallery_is_available_in_development()
    {
        var response = await factory.CreateClient().GetAsync("/dev/editors");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

}
