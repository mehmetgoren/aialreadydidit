namespace AiAlreadyDidIt.Api.Contracts.Common;

public static class ErrorCodes
{
    public const int BadRequest = 400;
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int PayloadTooLarge = 413;
    public const int UnprocessableEntity = 422;
    public const int TooManyRequests = 429;
    public const int InternalError = 500;
    public const int ServiceUnavailable = 503;
}

public static class MessageGroups
{
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string UnprocessableEntity = "UNPROCESSABLE_ENTITY";
    public const string RateLimit = "RATE_LIMIT_EXCEEDED";
    public const string InternalError = "INTERNAL_SERVER_ERROR";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}

public static class ApiErrors
{
    public static ApiError BadRequest(string message) => new(ErrorCodes.BadRequest, message, MessageGroups.BadRequest);
    public static ApiError Unauthorized(string message) => new(ErrorCodes.Unauthorized, message, MessageGroups.Unauthorized);
    public static ApiError Forbidden(string message) => new(ErrorCodes.Forbidden, message, MessageGroups.Forbidden);
    public static ApiError NotFound(string message) => new(ErrorCodes.NotFound, message, MessageGroups.NotFound);
    public static ApiError Conflict(string message) => new(ErrorCodes.Conflict, message, MessageGroups.Conflict);
    public static ApiError Unprocessable(string message, string? field = null) => new(ErrorCodes.UnprocessableEntity, message, MessageGroups.UnprocessableEntity, field);
    public static ApiError RateLimit(int windowSeconds) => new(ErrorCodes.TooManyRequests, $"Rate limit exceeded. Try again in {windowSeconds} seconds or use an API key for a higher quota.", MessageGroups.RateLimit);
    public static ApiError Unavailable(string message) => new(ErrorCodes.ServiceUnavailable, message, MessageGroups.ServiceUnavailable);
}
