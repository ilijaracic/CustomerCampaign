using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Customers;
using CustomerCampaign.WebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustomerCampaign.WebApi.Rewards;

public sealed class RewardService
{
    private readonly CampaignDbContext _db;
    private readonly ICustomerDirectory _customerDirectory;
    private readonly IDateTimeProvider _clock;
    private readonly CampaignOptions _campaign;

    public RewardService(
        CampaignDbContext db,
        ICustomerDirectory customerDirectory,
        IDateTimeProvider clock,
        IOptions<CampaignOptions> campaign)
    {
        _db = db;
        _customerDirectory = customerDirectory;
        _clock = clock;
        _campaign = campaign.Value;
    }

    public async Task<RewardResponse> GrantAsync(string agentUsername, CreateRewardRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId <= 0)
        {
            throw new ValidationAppException(ErrorCodes.InvalidCustomerId, "customerId must be a positive number.");
        }

        var today = _clock.Today;

        if (!_campaign.Contains(today))
        {
            throw new ValidationAppException(
                ErrorCodes.CampaignNotActive,
                $"The campaign runs from {_campaign.StartDate:yyyy-MM-dd} to {_campaign.EndDateExclusive.AddDays(-1):yyyy-MM-dd}; today is not within that window.");
        }

        var todaysCount = await _db.RewardEntries
            .CountAsync(r => r.AgentUsername == agentUsername && r.RewardDate == today, cancellationToken);

        if (todaysCount >= _campaign.DailyLimitPerAgent)
        {
            throw new ConflictAppException(
                ErrorCodes.DailyLimitReached,
                $"Agent '{agentUsername}' has already rewarded {_campaign.DailyLimitPerAgent} customers today.");
        }

        var alreadyRewarded = await _db.RewardEntries.AnyAsync(
            r => r.CustomerId == request.CustomerId && r.CampaignStartDate == _campaign.StartDate,
            cancellationToken);

        if (alreadyRewarded)
        {
            throw new ConflictAppException(
                ErrorCodes.CustomerAlreadyRewarded,
                $"Customer {request.CustomerId} has already been rewarded in the current campaign.");
        }

        var customer = await _customerDirectory.FindCustomerAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new NotFoundAppException(ErrorCodes.CustomerNotFound, $"No customer was found with id {request.CustomerId}.");
        }

        var entry = new RewardEntry
        {
            Id = Guid.NewGuid(),
            AgentUsername = agentUsername,
            CustomerId = customer.ExternalId,
            CustomerName = customer.FullName,
            RewardDate = today,
            CampaignStartDate = _campaign.StartDate,
            Notes = request.Notes,
            CreatedAtUtc = _clock.UtcNow,
        };

        _db.RewardEntries.Add(entry);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictAppException(
                ErrorCodes.CustomerAlreadyRewarded,
                $"Customer {request.CustomerId} has already been rewarded in the current campaign.");
        }

        return ToResponse(entry);
    }

    public async Task<IReadOnlyList<RewardResponse>> ListAsync(
        string? agentUsername, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = _db.RewardEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(agentUsername))
        {
            query = query.Where(r => r.AgentUsername == agentUsername);
        }

        if (from is not null)
        {
            query = query.Where(r => r.RewardDate >= from.Value);
        }

        if (to is not null)
        {
            query = query.Where(r => r.RewardDate <= to.Value);
        }

        var entries = (await query.ToListAsync(cancellationToken))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
        return entries.Select(ToResponse).ToList();
    }

    private static RewardResponse ToResponse(RewardEntry entry) => new(
        entry.Id, entry.AgentUsername, entry.CustomerId, entry.CustomerName, entry.RewardDate, entry.Notes, entry.CreatedAtUtc);
}
