namespace CustomerCampaign.WebApi.Rewards;

public sealed class CampaignOptions
{
    public const string SectionName = "Campaign";

    public DateOnly StartDate { get; set; }

    public int DurationDays { get; set; } = 7;

    public int DailyLimitPerAgent { get; set; } = 5;

    public DateOnly EndDateExclusive => StartDate.AddDays(DurationDays);

    public bool Contains(DateOnly date) => date >= StartDate && date < EndDateExclusive;
}
