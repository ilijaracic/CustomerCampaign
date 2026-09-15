using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Data;
using CustomerCampaign.WebApi.Rewards;
using CustomerCampaign.WebApi.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace CustomerCampaign.WebApi.Tests;

public sealed class RewardServiceTests : IDisposable
{
    private static readonly DateOnly CampaignStart = new(2026, 9, 1);
    private static readonly DateOnly Today = CampaignStart.AddDays(1);

    private readonly CampaignDbContext _db = TestSupport.NewDb();
    private readonly FakeCustomerDirectory _directory = new(101, 102, 103, 104, 105, 106, 201);
    private readonly FixedClock _clock = new(Today);

    public void Dispose() => _db.Dispose();

    private RewardService NewService(int dailyLimit = 5)
        => new(_db, _directory, _clock, TestSupport.MakeCampaignOptions(CampaignStart, dailyLimit: dailyLimit));

    [Fact]
    public async Task An_agent_can_reward_up_to_the_daily_limit()
    {
        var service = NewService(dailyLimit: 5);

        await GrantRangeAsync(service, "agent1", 101, 105);

        (await service.ListAsync("agent1", null, null)).Should().HaveCount(5);
    }

    [Fact]
    public async Task The_reward_past_the_daily_limit_is_refused_with_a_conflict()
    {
        var service = NewService(dailyLimit: 5);
        await GrantRangeAsync(service, "agent1", 101, 105);

        var act = () => service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 106 });

        (await act.Should().ThrowAsync<ConflictAppException>()).Which.Code.Should().Be(ErrorCodes.DailyLimitReached);
    }

    [Fact]
    public async Task Each_agent_has_their_own_independent_daily_quota()
    {
        var service = NewService(dailyLimit: 5);
        await GrantRangeAsync(service, "agent1", 101, 105);

        var act = () => service.GrantAsync("agent2", new CreateRewardRequest { CustomerId = 201 });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task A_customer_cannot_be_rewarded_twice_in_the_same_campaign()
    {
        var service = NewService();
        await service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 101 });

        var act = () => service.GrantAsync("agent2", new CreateRewardRequest { CustomerId = 101 });

        (await act.Should().ThrowAsync<ConflictAppException>()).Which.Code.Should().Be(ErrorCodes.CustomerAlreadyRewarded);
    }

    [Fact]
    public async Task An_unknown_customer_is_refused_and_nothing_is_written()
    {
        var service = NewService();

        var act = () => service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 999 });

        (await act.Should().ThrowAsync<NotFoundAppException>()).Which.Code.Should().Be(ErrorCodes.CustomerNotFound);
        _db.RewardEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task An_outage_of_the_customer_directory_surfaces_as_an_upstream_failure()
    {
        _directory.SimulateOutage = true;
        var service = NewService();

        var act = () => service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 101 });

        await act.Should().ThrowAsync<ExternalServiceException>();
        _db.RewardEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task A_non_positive_customer_id_is_rejected_as_a_validation_error()
    {
        var service = NewService();

        var act = () => service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 0 });

        (await act.Should().ThrowAsync<ValidationAppException>()).Which.Code.Should().Be(ErrorCodes.InvalidCustomerId);
    }

    [Fact]
    public async Task A_reward_outside_the_campaign_window_is_rejected()
    {
        var lateClock = new FixedClock(CampaignStart.AddDays(14));
        var service = new RewardService(_db, _directory, lateClock, TestSupport.MakeCampaignOptions(CampaignStart));

        var act = () => service.GrantAsync("agent1", new CreateRewardRequest { CustomerId = 101 });

        (await act.Should().ThrowAsync<ValidationAppException>()).Which.Code.Should().Be(ErrorCodes.CampaignNotActive);
    }

    private static async Task GrantRangeAsync(RewardService service, string agent, int firstCustomerId, int lastCustomerId)
    {
        for (var customerId = firstCustomerId; customerId <= lastCustomerId; customerId++)
        {
            await service.GrantAsync(agent, new CreateRewardRequest { CustomerId = customerId });
        }
    }
}
