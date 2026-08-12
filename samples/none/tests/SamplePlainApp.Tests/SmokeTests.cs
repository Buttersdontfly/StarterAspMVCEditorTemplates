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

}
