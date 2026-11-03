namespace UtilityData.Web.Services;

public sealed class ApiException : Exception
{
    public int StatusCode { get; }

    public string? Title { get; }

    public ApiException(
        int statusCode,
        string message,
        string? title = null)
        : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }
}