namespace CustomerCampaign.WebApi.Common;

public interface IDateTimeProvider
{
    DateOnly Today { get; }

    DateTimeOffset UtcNow { get; }
}

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
