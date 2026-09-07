using System.Text.Json;
using AiAlreadyDidIt.Api.Contracts.Common;

namespace AiAlreadyDidIt.Api.Infrastructure;

public static class HttpResponseExtensions
{
    public static Task WriteEnvelopeAsync(this HttpResponse response, int httpStatus, ApiResponse<object> body, CancellationToken ct = default)
    {
        response.StatusCode = httpStatus;
        response.ContentType = "application/json; charset=utf-8";
        return response.WriteAsync(JsonSerializer.Serialize(body, AadiJson.Options), ct);
    }
}

/// <summary>Converts <see cref="ApiException"/> and unexpected errors into the envelope.</summary>
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex) when (!context.Response.HasStarted)
        {
            await context.Response.WriteEnvelopeAsync(ex.HttpStatusCode,
                ApiResponse.Fail(ex.HttpStatusCode, ex.StatusMessage, ex.Errors), context.RequestAborted);
        }
        catch (BadHttpRequestException ex) when (!context.Response.HasStarted && ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            await context.Response.WriteEnvelopeAsync(413,
                ApiResponse.Fail(413, MessageGroups.PayloadTooLarge, new ApiError(413, "The uploaded file is larger than the allowed limit.", MessageGroups.PayloadTooLarge)),
                context.RequestAborted);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client went away; nothing to write.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            var message = env.IsDevelopment() ? ex.Message : "An unexpected error occurred. Please try again later.";
            await context.Response.WriteEnvelopeAsync(500,
                ApiResponse.Fail(500, MessageGroups.InternalError, new ApiError(500, message, MessageGroups.InternalError)),
                context.RequestAborted);
        }
    }
}

/// <summary>Adds the security headers nginx does not add for API responses.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            return Task.CompletedTask;
        });
        return next(context);
    }
}
