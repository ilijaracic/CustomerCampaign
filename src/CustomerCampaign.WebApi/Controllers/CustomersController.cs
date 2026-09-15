using CustomerCampaign.WebApi.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCampaign.WebApi.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerDirectory _customerDirectory;

    public CustomersController(ICustomerDirectory customerDirectory)
    {
        _customerDirectory = customerDirectory;
    }

    [HttpGet("{customerId:int}")]
    [ProducesResponseType(typeof(CustomerLookupResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CustomerLookupResult>> Get(int customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerDirectory.FindCustomerAsync(customerId, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }
}
