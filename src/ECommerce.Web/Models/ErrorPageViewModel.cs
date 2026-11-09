namespace ECommerce.Web.Models;

public sealed class ErrorPageViewModel
{
    public int StatusCode { get; init; }
    public string Title { get; init; } = "Error";
    public string Message { get; init; } = string.Empty;
    public string? RequestId { get; init; }
}
