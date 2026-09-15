using CustomerCampaign.WebApi.Auth;
using CustomerCampaign.WebApi.Common;
using CustomerCampaign.WebApi.Customers;
using CustomerCampaign.WebApi.Data;
using CustomerCampaign.WebApi.Purchases;
using CustomerCampaign.WebApi.Results;
using CustomerCampaign.WebApi.Rewards;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AgentCredentialsOptions>()
    .Bind(builder.Configuration.GetSection(AgentCredentialsOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<SoapOptions>()
    .Bind(builder.Configuration.GetSection(SoapOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddOptions<CampaignOptions>()
    .Bind(builder.Configuration.GetSection(CampaignOptions.SectionName))
    .PostConfigure(options =>
    {
        if (options.StartDate == default)
        {
            options.StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
    })
    .ValidateOnStart();

var connectionString = builder.Configuration.GetConnectionString("Campaign") ?? "Data Source=customercampaign.db";
builder.Services.AddDbContext<CampaignDbContext>(options => options.UseSqlite(connectionString));

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<RewardService>();
builder.Services.AddScoped<CsvPurchaseImporter>();
builder.Services.AddScoped<ResultsService>();

builder.Services.AddHttpClient<ICustomerDirectory, SoapCustomerDirectory>((serviceProvider, client) =>
{
    var soapOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SoapOptions>>().Value;
    client.BaseAddress = new Uri(soapOptions.ServiceUrl);
});

builder.Services
    .AddAuthentication(BasicAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(BasicAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Customer Campaign API",
        Version = "v1",
        Description = "Reward-entry, SOAP customer lookup, purchase-report merging and results for the customer service campaign.",
    });

    var basicScheme = new OpenApiSecurityScheme
    {
        Scheme = "basic",
        Type = SecuritySchemeType.Http,
        Description = "HTTP Basic Authentication over HTTPS. Enter the agent username and password.",
    };
    options.AddSecurityDefinition("basic", basicScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" } }, Array.Empty<string>() },
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
    db.Database.EnsureCreated();

    var campaign = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CampaignOptions>>().Value;
    app.Logger.LogInformation(
        "Campaign window: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd} (exclusive), daily limit {DailyLimit} per agent.",
        campaign.StartDate, campaign.EndDateExclusive, campaign.DailyLimitPerAgent);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

public partial class Program
{
}
