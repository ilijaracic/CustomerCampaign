namespace CustomerCampaign.WebApi.Results;

public sealed record MergedResultResponse(
    Guid RewardId,
    string AgentUsername,
    int CustomerId,
    string CustomerName,
    DateOnly RewardDate,
    DateOnly? PurchaseDate,
    string? OrderReference,
    decimal? Amount);
