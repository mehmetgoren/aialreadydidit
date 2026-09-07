namespace AiAlreadyDidIt.Api.Contracts.Common;

/// <summary>Thrown by services to short-circuit a request with an error envelope.</summary>
public class ApiException : Exception
{
    public ApiException(int httpStatusCode, string statusMessage, params IEnumerable<ApiError> errors)
        : base(errors.FirstOrDefault()?.Message ?? statusMessage)
    {
        HttpStatusCode = httpStatusCode;
        StatusMessage = statusMessage;
        Errors = errors.ToList();
    }

    public int HttpStatusCode { get; }
    public string StatusMessage { get; }
    public List<ApiError> Errors { get; }

    public static ApiException NotFound(string message) => new(404, MessageGroups.NotFound, ApiErrors.NotFound(message));
    public static ApiException Unprocessable(string message, string? field = null) => new(422, MessageGroups.UnprocessableEntity, ApiErrors.Unprocessable(message, field));
    public static ApiException Unprocessable(IEnumerable<ApiError> errors) => new(422, MessageGroups.UnprocessableEntity, errors);
    public static ApiException BadRequest(string message) => new(400, MessageGroups.BadRequest, ApiErrors.BadRequest(message));
    public static ApiException Unauthorized(string message) => new(401, MessageGroups.Unauthorized, ApiErrors.Unauthorized(message));
    public static ApiException Forbidden(string message) => new(403, MessageGroups.Forbidden, ApiErrors.Forbidden(message));
    public static ApiException Conflict(string message) => new(409, MessageGroups.Conflict, ApiErrors.Conflict(message));
    public static ApiException Unavailable(string message) => new(503, MessageGroups.ServiceUnavailable, ApiErrors.Unavailable(message));
}

/// <summary>Collects field-level validation errors and throws them all at once.</summary>
public sealed class ValidationBag
{
    private readonly List<ApiError> _errors = [];
    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<ApiError> Errors => _errors;

    public ValidationBag Add(string field, string message)
    {
        _errors.Add(ApiErrors.Unprocessable(message, field));
        return this;
    }

    public ValidationBag Require(bool condition, string field, string message)
    {
        if (!condition) Add(field, message);
        return this;
    }

    public void ThrowIfAny()
    {
        if (HasErrors) throw ApiException.Unprocessable(_errors);
    }
}
