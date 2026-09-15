namespace CustomerCampaign.WebApi.Customers;

public sealed record CustomerLookupResult(
    int ExternalId,
    string FullName,
    DateOnly? DateOfBirth,
    string? City,
    string? PostalCode);
