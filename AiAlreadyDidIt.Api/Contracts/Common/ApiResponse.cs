namespace AiAlreadyDidIt.Api.Contracts.Common;

/// <summary>
/// Response envelope. Every endpoint (success or failure) returns this shape:
/// <c>{ statusCode, statusMessage, result, errors }</c>.
/// </summary>
public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public string StatusMessage { get; set; } = "OK";
    public T? Result { get; set; }
    public List<ApiError>? Errors { get; set; }
}

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T result) => new() { StatusCode = 200, StatusMessage = "OK", Result = result };

    public static ApiResponse<object> Fail(int statusCode, string statusMessage, params IEnumerable<ApiError> errors) =>
        new() { StatusCode = statusCode, StatusMessage = statusMessage, Result = null, Errors = errors.ToList() };
}

public class ApiError
{
    public ApiError() { }

    public ApiError(int statusCode, string message, string messageGroup, string? field = null)
    {
        StatusCode = statusCode;
        Message = message;
        MessageGroup = messageGroup;
        Field = field;
    }

    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageGroup { get; set; } = string.Empty;
    /// <summary>Form field the error belongs to (validation errors), when known.</summary>
    public string? Field { get; set; }
}
