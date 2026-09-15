using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Data;
using CustomerCampaign.WebApi.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerCampaign.WebApi.Tests;

internal static class TestSupport
{
    public static CampaignDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<CampaignDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CampaignDbContext(options);
    }

    public static IOptions<CampaignOptions> MakeCampaignOptions(DateOnly start, int durationDays = 7, int dailyLimit = 5)
        => Options.Create(new CustomerCampaign.WebApi.Rewards.CampaignOptions
        {
            StartDate = start,
            DurationDays = durationDays,
            DailyLimitPerAgent = dailyLimit,
        });
}

internal sealed class FixedClock : IDateTimeProvider
{
    public FixedClock(DateOnly today) => Today = today;

    public DateOnly Today { get; }

    public DateTimeOffset UtcNow => Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
