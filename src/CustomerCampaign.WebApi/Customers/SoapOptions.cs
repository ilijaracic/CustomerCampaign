namespace CustomerCampaign.WebApi.Customers;

public sealed class SoapOptions
{
    public const string SectionName = "Soap";

    public string ServiceUrl { get; set; } = "https://www.crcind.com/csp/samples/SOAP.Demo.cls";

    public int TimeoutSeconds { get; set; } = 15;
}
