using System.Text;
using CustomerCampaign.WebApi.Common;
using Microsoft.Extensions.Options;

namespace CustomerCampaign.WebApi.Customers;

public sealed class SoapCustomerDirectory : ICustomerDirectory
{
    private const string SoapAction = "http://tempuri.org/FindPerson";

    private readonly HttpClient _httpClient;
    private readonly ILogger<SoapCustomerDirectory> _logger;

    public SoapCustomerDirectory(HttpClient httpClient, IOptions<SoapOptions> options, ILogger<SoapCustomerDirectory> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        _logger = logger;
    }

    public async Task<CustomerLookupResult?> FindCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = new StringContent(BuildEnvelope(customerId), Encoding.UTF8, "text/xml"),
        };
        request.Headers.Add("SOAPAction", SoapAction);

        HttpResponseMessage response;
        string body;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "The customer directory service could not be reached for customer {CustomerId}.", customerId);
            throw new ExternalServiceException(ErrorCodes.CustomerDirectoryUnavailable, "The customer directory service is unavailable.");
        }

        var fault = SoapPersonParser.TryReadFault(body);
        if (fault is not null)
        {
            if (SoapPersonParser.LooksLikeNotFound(fault))
            {
                return null;
            }

            _logger.LogWarning("The customer directory service returned a fault for customer {CustomerId}: {Fault}", customerId, fault);
            throw new ExternalServiceException(ErrorCodes.CustomerDirectoryUnavailable, "The customer directory service returned an error.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "The customer directory service returned {StatusCode} for customer {CustomerId}.", (int)response.StatusCode, customerId);
            throw new ExternalServiceException(ErrorCodes.CustomerDirectoryUnavailable, "The customer directory service is unavailable.");
        }

        return SoapPersonParser.Parse(customerId, body);
    }

    private static string BuildEnvelope(int customerId)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <FindPerson xmlns="http://tempuri.org">
                  <id>{customerId}</id>
                </FindPerson>
              </soap:Body>
            </soap:Envelope>
            """;
    }
}
