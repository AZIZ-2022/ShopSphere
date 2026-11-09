namespace ECommerce.Application.Common.Models;

/// <summary>Lightweight operation outcome without a payload.</summary>
public class Result
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    public static Result Success() => new() { Succeeded = true };
    public static Result Failure(string error) => new() { Succeeded = false, Error = error };
    public static Result Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new() { Succeeded = false, Error = "Validation failed.", ValidationErrors = errors };
}

/// <summary>Operation outcome carrying a payload on success.</summary>
public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static new Result<T> Failure(string error) => new() { Succeeded = false, Error = error };
}
