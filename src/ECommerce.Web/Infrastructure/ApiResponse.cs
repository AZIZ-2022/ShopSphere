namespace ECommerce.Web.Infrastructure;

/// <summary>Standard JSON envelope for the <c>/api/*</c> endpoints.</summary>
public sealed class ApiResponse
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public static ApiResponse Ok(object? data = null, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
