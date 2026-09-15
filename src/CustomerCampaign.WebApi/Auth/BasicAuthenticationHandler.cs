using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CustomerCampaign.WebApi.Auth;

public sealed class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Basic";

    private readonly AgentCredentialsOptions _credentials;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<AgentCredentialsOptions> credentials)
        : base(options, logger, encoder)
    {
        _credentials = credentials.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsHttps)
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "HTTPS is required: Basic Authentication sends credentials base64-encoded, not encrypted."));
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader) || authorizationHeader.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!TryParseBasicHeader(authorizationHeader.ToString(), out var userName, out var password))
        {
            return Task.FromResult(AuthenticateResult.Fail("The Authorization header is not a valid Basic credential."));
        }

        var agent = _credentials.Agents.FirstOrDefault(a =>
            string.Equals(a.UserName, userName, StringComparison.Ordinal));

        if (agent is null || !PasswordsMatch(agent.Password, password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, agent.UserName),
            new Claim(ClaimTypes.Name, agent.UserName),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Basic realm=\"CustomerCampaign\", charset=\"UTF-8\"";
        return base.HandleChallengeAsync(properties);
    }

    private static bool TryParseBasicHeader(string headerValue, out string userName, out string password)
    {
        userName = string.Empty;
        password = string.Empty;

        const string prefix = "Basic ";
        if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string decoded;
        try
        {
            var bytes = Convert.FromBase64String(headerValue[prefix.Length..].Trim());
            decoded = Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        userName = decoded[..separatorIndex];
        password = decoded[(separatorIndex + 1)..];
        return true;
    }

    private static bool PasswordsMatch(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);

        if (expectedBytes.Length != actualBytes.Length)
        {
            CryptographicOperations.FixedTimeEquals(expectedBytes, expectedBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
