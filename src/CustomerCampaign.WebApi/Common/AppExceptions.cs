namespace CustomerCampaign.WebApi.Common;

public abstract class AppException : Exception
{
    protected AppException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string code, string message) : base(StatusCodes.Status400BadRequest, code, message)
    {
    }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string code, string message) : base(StatusCodes.Status404NotFound, code, message)
    {
    }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string code, string message) : base(StatusCodes.Status409Conflict, code, message)
    {
    }
}

public sealed class ExternalServiceException : AppException
{
    public ExternalServiceException(string code, string message) : base(StatusCodes.Status502BadGateway, code, message)
    {
    }
}

public static class ErrorCodes
{
    public const string InvalidCustomerId = "invalid_customer_id";
    public const string CampaignNotActive = "campaign_not_active";
    public const string DailyLimitReached = "daily_limit_reached";
    public const string CustomerAlreadyRewarded = "customer_already_rewarded";
    public const string CustomerNotFound = "customer_not_found";
    public const string CustomerDirectoryUnavailable = "customer_directory_unavailable";
    public const string RewardNotFound = "reward_not_found";
    public const string HttpsRequired = "https_required";
    public const string InvalidCsvFile = "invalid_csv_file";
}
