namespace CustomerCampaign.WebApi.Purchases;

public sealed record ImportRowError(int Row, string Reason);

public sealed record ImportSummaryResponse(
    string FileName,
    int TotalRows,
    int Imported,
    int Skipped,
    IReadOnlyList<ImportRowError> Errors);
