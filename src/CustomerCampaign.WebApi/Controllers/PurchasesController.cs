using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Purchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerCampaign.WebApi.Controllers;

[ApiController]
[Route("api/purchases")]
[Authorize]
[Produces("application/json")]
public sealed class PurchasesController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private readonly CsvPurchaseImporter _importer;

    public PurchasesController(CsvPurchaseImporter importer)
    {
        _importer = importer;
    }

    [HttpPost("import")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(ImportSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImportSummaryResponse>> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new ValidationAppException(ErrorCodes.InvalidCsvFile, "The uploaded file is empty.");
        }

        await using var stream = file.OpenReadStream();
        var summary = await _importer.ImportAsync(stream, file.FileName, cancellationToken);
        return Ok(summary);
    }
}
