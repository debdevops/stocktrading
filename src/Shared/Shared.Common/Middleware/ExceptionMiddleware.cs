using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Common.Models;
using System.Net;

namespace Shared.Common.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>();
        
        switch (exception)
        {
            case UnauthorizedAccessException:
                response.Message = "Unauthorized access";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
            case ArgumentException:
                response.Message = exception.Message;
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;
            case KeyNotFoundException:
                response.Message = "Resource not found";
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
            default:
                response.Message = "An internal server error occurred";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        response.Success = false;
        response.Errors.Add(exception.Message);

        var jsonResponse = JsonConvert.SerializeObject(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}
