namespace CustomerCampaign.WebApi.Auth;

public sealed class AgentCredentialsOptions
{
    public const string SectionName = "Agents";

    public List<AgentCredential> Agents { get; set; } = new();
}

public sealed class AgentCredential
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
