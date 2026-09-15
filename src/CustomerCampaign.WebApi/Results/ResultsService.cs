using CustomerCampaign.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerCampaign.WebApi.Results;

public sealed class ResultsService
{
    private readonly CampaignDbContext _db;

    public ResultsService(CampaignDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MergedResultResponse>> GetMergedResultsAsync(
        string? agentUsername, CancellationToken cancellationToken = default)
    {
        var rewards = await FilterByAgent(_db.RewardEntries.AsQueryable(), agentUsername)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (rewards.Count == 0)
        {
            return Array.Empty<MergedResultResponse>();
        }

        var customerIds = rewards.Select(r => r.CustomerId).Distinct().ToArray();
        var purchases = await _db.PurchaseRecords
            .Where(p => customerIds.Contains(p.CustomerId))
            .ToListAsync(cancellationToken);

        var results = rewards
            .Select(reward =>
            {
                var match = purchases
                    .Where(p => p.CustomerId == reward.CustomerId && p.PurchaseDate >= reward.RewardDate)
                    .OrderBy(p => p.PurchaseDate)
                    .FirstOrDefault();

                return new MergedResultResponse(
                    reward.Id,
                    reward.AgentUsername,
                    reward.CustomerId,
                    reward.CustomerName,
                    reward.RewardDate,
                    PurchaseDate: match?.PurchaseDate,
                    OrderReference: match?.OrderReference,
                    Amount: match?.Amount);
            })
            .ToList();

        return results;
    }

    private static IQueryable<RewardEntry> FilterByAgent(IQueryable<RewardEntry> query, string? agentUsername)
        => string.IsNullOrWhiteSpace(agentUsername) ? query : query.Where(r => r.AgentUsername == agentUsername);
}
