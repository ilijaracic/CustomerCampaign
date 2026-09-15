using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Customers;

namespace CustomerCampaign.WebApi.Tests.Fakes;

internal sealed class FakeCustomerDirectory : ICustomerDirectory
{
    private readonly Dictionary<int, string> _known;

    public FakeCustomerDirectory(params int[] knownCustomerIds)
    {
        _known = knownCustomerIds.ToDictionary(id => id, id => $"Customer {id}");
    }

    public bool SimulateOutage { get; set; }

    public Task<CustomerLookupResult?> FindCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        if (SimulateOutage)
        {
            throw new ExternalServiceException(ErrorCodes.CustomerDirectoryUnavailable, "The customer directory service is unavailable.");
        }

        if (!_known.TryGetValue(customerId, out var name))
        {
            return Task.FromResult<CustomerLookupResult?>(null);
        }

        return Task.FromResult<CustomerLookupResult?>(new CustomerLookupResult(customerId, name, new DateOnly(1990, 1, 1), "Beograd", "11000"));
    }
}
