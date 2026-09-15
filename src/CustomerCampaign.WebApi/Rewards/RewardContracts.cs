using System.ComponentModel.DataAnnotations;

namespace CustomerCampaign.WebApi.Rewards;

public sealed class CreateRewardRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "customerId must be a positive number.")]
    public int CustomerId { get; set; }

    [MaxLength(500, ErrorMessage = "notes must be 500 characters or fewer.")]
    public string? Notes { get; set; }
}

public sealed record RewardResponse(
    Guid Id,
    string AgentUsername,
    int CustomerId,
    string CustomerName,
    DateOnly RewardDate,
    string? Notes,
    DateTimeOffset CreatedAtUtc);
