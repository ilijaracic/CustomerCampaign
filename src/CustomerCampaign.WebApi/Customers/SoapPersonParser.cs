using System.Globalization;
using System.Xml.Linq;

namespace CustomerCampaign.WebApi.Customers;

public static class SoapPersonParser
{
    public static CustomerLookupResult? Parse(int requestedId, string xml)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception)
        {
            return null;
        }

        var result = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "FindPersonResult");
        if (result is null)
        {
            return null;
        }

        var rawName = Local(result, "Name");
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return null;
        }

        var home = result.Elements().FirstOrDefault(e => e.Name.LocalName == "Home");
        var office = result.Elements().FirstOrDefault(e => e.Name.LocalName == "Office");
        var address = home ?? office;

        return new CustomerLookupResult(
            ExternalId: requestedId,
            FullName: HumanizeName(rawName),
            DateOfBirth: TryParseDate(Local(result, "DOB")),
            City: address is null ? null : Local(address, "City"),
            PostalCode: address is null ? null : Local(address, "Zip"));
    }

    public static string? TryReadFault(string xml)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (Exception)
        {
            return null;
        }

        var fault = document.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault is null)
        {
            return null;
        }

        return Local(fault, "faultstring") ?? fault.Value;
    }

    public static bool LooksLikeNotFound(string faultString)
        => faultString.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static string? Local(XElement parent, string localName)
        => parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();

    private static DateOnly? TryParseDate(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    private static string HumanizeName(string rawName)
    {
        var parts = rawName.Split(',', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? $"{parts[1]} {parts[0]}" : rawName;
    }
}
