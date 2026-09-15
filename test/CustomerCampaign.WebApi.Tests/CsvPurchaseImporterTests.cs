using CustomerCampaign.WebApi.Data;
using CustomerCampaign.WebApi.Purchases;
using FluentAssertions;
using Xunit;

namespace CustomerCampaign.WebApi.Tests;

public sealed class CsvPurchaseImporterTests : IDisposable
{
    private readonly CampaignDbContext _db = TestSupport.NewDb();
    private readonly FixedClock _clock = new(new DateOnly(2026, 11, 1));
    private readonly string _samplePath = Path.Combine(AppContext.BaseDirectory, "samples", "purchases-2026-10.csv");

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Good_rows_are_imported_and_bad_rows_are_reported_without_aborting_the_file()
    {
        var importer = new CsvPurchaseImporter(_db, _clock);

        await using var stream = File.OpenRead(_samplePath);
        var summary = await importer.ImportAsync(stream, "purchases-2026-10.csv");

        summary.TotalRows.Should().Be(9);
        summary.Imported.Should().Be(6);
        summary.Skipped.Should().Be(3);
        summary.Errors.Should().HaveCount(3);
        _db.PurchaseRecords.Should().HaveCount(6);
    }

    [Fact]
    public async Task Reimporting_the_same_file_skips_every_row_as_a_duplicate()
    {
        var importer = new CsvPurchaseImporter(_db, _clock);
        await using (var first = File.OpenRead(_samplePath))
        {
            await importer.ImportAsync(first, "purchases-2026-10.csv");
        }

        await using var second = File.OpenRead(_samplePath);
        var summary = await importer.ImportAsync(second, "purchases-2026-10.csv");

        summary.Imported.Should().Be(0);
        _db.PurchaseRecords.Should().HaveCount(6);
    }

    [Fact]
    public async Task An_empty_file_is_rejected_as_a_validation_error()
    {
        var importer = new CsvPurchaseImporter(_db, _clock);
        using var empty = new MemoryStream();

        var act = () => importer.ImportAsync(empty, "empty.csv");

        await act.Should().ThrowAsync<CustomerCampaign.WebApi.Common.ValidationAppException>();
    }
}
