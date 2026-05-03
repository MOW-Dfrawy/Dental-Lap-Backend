using DentalLab.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DentalLab.API.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    public string[] AllowedRoles { get; }

    public RequireRoleAttribute(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.Request.Headers["X-Role"].ToString();

        if (string.IsNullOrWhiteSpace(role))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                statusCode = 401,
                error = "Missing X-Role header. Provide one of: Admin, Technician, Reception."
            })
            { StatusCode = 401 };
            return;
        }

        if (AllowedRoles.Length > 0 && !AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                statusCode = 403,
                error = $"Role '{role}' is not allowed for this action. Required: {string.Join(", ", AllowedRoles)}"
            })
            { StatusCode = 403 };
        }
    }
}

public static class HttpContextExtensions
{
    public static string? GetUserName(this HttpContext context)
    {
        var user = context.Request.Headers["X-User"].ToString();
        return string.IsNullOrWhiteSpace(user) ? context.Request.Headers["X-Role"].ToString() : user;
    }

    public static string? GetUserRole(this HttpContext context)
        => context.Request.Headers["X-Role"].ToString();
}
