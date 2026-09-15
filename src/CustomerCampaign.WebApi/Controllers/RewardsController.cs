using System.Security.Claims;
using CustomerCampaign.WebApi.Rewards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCampaign.WebApi.Controllers;

[ApiController]
[Route("api/rewards")]
[Authorize]
[Produces("application/json")]
public sealed class RewardsController : ControllerBase
{
    private readonly RewardService _rewardService;

    public RewardsController(RewardService rewardService)
    {
        _rewardService = rewardService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RewardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<RewardResponse>> Create([FromBody] CreateRewardRequest request, CancellationToken cancellationToken)
    {
        var result = await _rewardService.GrantAsync(CurrentAgent, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RewardResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RewardResponse>>> List(
        [FromQuery] string? agentUsername, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var results = await _rewardService.ListAsync(agentUsername, from, to, cancellationToken);
        return Ok(results);
    }

    private string CurrentAgent => User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
