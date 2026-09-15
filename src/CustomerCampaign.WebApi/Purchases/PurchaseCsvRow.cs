using CsvHelper.Configuration.Attributes;

namespace CustomerCampaign.WebApi.Purchases;

public sealed class PurchaseCsvRow
{
    [Name("CustomerId")]
    public int CustomerId { get; set; }

    [Name("CustomerName")]
    public string CustomerName { get; set; } = string.Empty;

    [Name("OrderReference")]
    public string OrderReference { get; set; } = string.Empty;

    [Name("PurchaseDate")]
    public DateOnly PurchaseDate { get; set; }

    [Name("Amount")]
    public decimal Amount { get; set; }
}
