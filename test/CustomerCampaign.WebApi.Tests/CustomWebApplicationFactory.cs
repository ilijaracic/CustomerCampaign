using CustomerCampaign.WebApi.Auth;
using CustomerCampaign.WebApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CustomerCampaign.WebApi.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ValidUserName = "agent1";
    public const string ValidPassword = "correct-horse-battery-staple";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agents:Agents:0:UserName"] = ValidUserName,
                ["Agents:Agents:0:Password"] = ValidPassword,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CampaignDbContext>>();
            services.AddDbContext<CampaignDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
    });
}
