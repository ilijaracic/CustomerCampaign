namespace CustomerCampaign.WebApi.Customers;

public interface ICustomerDirectory
{
    Task<CustomerLookupResult?> FindCustomerAsync(int customerId, CancellationToken cancellationToken = default);
}
