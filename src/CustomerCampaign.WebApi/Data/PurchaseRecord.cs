namespace CustomerCampaign.WebApi.Data;

public sealed class PurchaseRecord
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string OrderReference { get; set; } = string.Empty;

    public DateOnly PurchaseDate { get; set; }

    public decimal Amount { get; set; }

    public string SourceFile { get; set; } = string.Empty;

    public DateTimeOffset ImportedAtUtc { get; set; }
}
