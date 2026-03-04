using Microsoft.AspNetCore.Diagnostics;
using SecureMedicalRecordSystem.Core.DTOs;
using System.Net;
using System.Text.Json;

namespace SecureMedicalRecordSystem.API.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = context.Response;
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred. Please try again later.";

        // Map exception types to status codes if needed
        // switch (exception) { ... }

        response.StatusCode = (int)statusCode;

        var result = ApiResponse.FailureResult(
            _env.IsDevelopment() ? exception.Message : message
        );

        if (_env.IsDevelopment())
        {
            // Add stack trace in development
            result.Data = exception.StackTrace ?? string.Empty;
        }

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        await response.WriteAsync(json);
    }
}
