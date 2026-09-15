using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace CustomerCampaign.WebApi.Purchases;

public sealed class CsvPurchaseImporter
{
    private readonly CampaignDbContext _db;
    private readonly IDateTimeProvider _clock;

    public CsvPurchaseImporter(CampaignDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ImportSummaryResponse> ImportAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        var totalRows = 0;
        var imported = 0;
        var errors = new List<ImportRowError>();
        var existingOrderReferences = _db.PurchaseRecords.Select(p => p.OrderReference).ToHashSet();
        var seenInThisFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            throw new ValidationAppException(ErrorCodes.InvalidCsvFile, "The CSV file is empty or has no header row.");
        }

        while (await csv.ReadAsync())
        {
            totalRows++;
            var rowNumber = csv.Context.Parser?.Row ?? totalRows;

            PurchaseCsvRow row;
            try
            {
                row = csv.GetRecord<PurchaseCsvRow>();
            }
            catch (Exception ex)
            {
                errors.Add(new ImportRowError(rowNumber, Describe(ex)));
                continue;
            }

            if (row.CustomerId <= 0)
            {
                errors.Add(new ImportRowError(rowNumber, "customerId must be a positive number."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.OrderReference))
            {
                errors.Add(new ImportRowError(rowNumber, "orderReference is required."));
                continue;
            }

            if (row.Amount < 0)
            {
                errors.Add(new ImportRowError(rowNumber, "amount cannot be negative."));
                continue;
            }

            if (existingOrderReferences.Contains(row.OrderReference) || !seenInThisFile.Add(row.OrderReference))
            {
                errors.Add(new ImportRowError(rowNumber, $"order '{row.OrderReference}' was already imported; skipped."));
                continue;
            }

            _db.PurchaseRecords.Add(new PurchaseRecord
            {
                Id = Guid.NewGuid(),
                CustomerId = row.CustomerId,
                CustomerName = row.CustomerName,
                OrderReference = row.OrderReference,
                PurchaseDate = row.PurchaseDate,
                Amount = row.Amount,
                SourceFile = fileName,
                ImportedAtUtc = _clock.UtcNow,
            });
            imported++;
        }

        if (imported > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new ImportSummaryResponse(fileName, totalRows, imported, totalRows - imported, errors);
    }

    private static string Describe(Exception ex) => ex switch
    {
        CsvHelper.TypeConversion.TypeConverterException => "one or more fields could not be parsed (check for text where a number or date was expected).",
        CsvHelper.MissingFieldException => "the row is missing one or more expected columns.",
        _ => "the row could not be read.",
    };
}
