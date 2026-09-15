namespace CustomerCampaign.WebApi.Data;

public sealed class RewardEntry
{
    public Guid Id { get; set; }

    public string AgentUsername { get; set; } = string.Empty;

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateOnly RewardDate { get; set; }

    public DateOnly CampaignStartDate { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
