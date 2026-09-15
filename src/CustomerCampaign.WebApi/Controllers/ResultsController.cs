using CustomerCampaign.WebApi.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCampaign.WebApi.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
[Produces("application/json")]
public sealed class ResultsController : ControllerBase
{
    private readonly ResultsService _resultsService;

    public ResultsController(ResultsService resultsService)
    {
        _resultsService = resultsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MergedResultResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MergedResultResponse>>> List(
        [FromQuery] string? agentUsername, CancellationToken cancellationToken)
    {
        var results = await _resultsService.GetMergedResultsAsync(agentUsername, cancellationToken);
        return Ok(results);
    }
}
