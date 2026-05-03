using System.Net;
using System.Text.Json;
using DentalLab.Application.Common;

namespace DentalLab.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, message) = ex switch
        {
            NotFoundException => ((int)HttpStatusCode.NotFound, ex.Message),
            ValidationException => ((int)HttpStatusCode.BadRequest, ex.Message),
            BusinessRuleException => ((int)HttpStatusCode.Conflict, ex.Message),
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, ex.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500)
            _logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
        else
            _logger.LogWarning("Request error {Status} on {Path}: {Message}", status, context.Request.Path, ex.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        var payload = new
        {
            success = false,
            statusCode = status,
            error = message,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
