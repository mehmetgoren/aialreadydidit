using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Infrastructure.Auth;

/// <summary>
/// Admin-panel authorisation: admins may call everything; other roles need a <c>role_actions</c> row for the
/// controller/action (or controller/"*"). API-key principals are never allowed into the admin area.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RoleActionAuthorizeAttribute : TypeFilterAttribute
{
    public RoleActionAuthorizeAttribute() : base(typeof(RoleActionAuthorizationFilter)) { }
}

/// <summary>Opts a single action out of <see cref="RoleActionAuthorizeAttribute"/> (still requires authentication).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class AllowAnyRoleAttribute : Attribute;

public sealed class RoleActionAuthorizationFilter(AadiDbContext db, ICurrentUser currentUser) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!currentUser.IsAuthenticated) return; // [Authorize] already rejected anonymous callers
        if (currentUser.IsApiKey)
        {
            context.Result = Forbidden("API keys cannot access the admin panel.");
            return;
        }
        if (currentUser.IsAdmin) return;
        if (context.ActionDescriptor is not ControllerActionDescriptor descriptor) return;
        if (descriptor.MethodInfo.IsDefined(typeof(AllowAnyRoleAttribute), inherit: true)) return;

        var roleName = currentUser.RoleName;
        var allowed = !string.IsNullOrEmpty(roleName) && await db.RoleActions.AsNoTracking().AnyAsync(a =>
                a.Role.Name == roleName && a.Controller == descriptor.ControllerName && (a.Action == descriptor.ActionName || a.Action == "*"),
            context.HttpContext.RequestAborted);
        if (!allowed) context.Result = Forbidden("You are not allowed to perform this action.");
    }

    private static ObjectResult Forbidden(string message) =>
        new(ApiResponse.Fail(ErrorCodes.Forbidden, MessageGroups.Forbidden, ApiErrors.Forbidden(message))) { StatusCode = StatusCodes.Status403Forbidden };
}
