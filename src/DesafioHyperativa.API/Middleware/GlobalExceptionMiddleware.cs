using System.Net;
using System.Text.Json;
using FluentValidation;

namespace DesafioHyperativa.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            _logger.LogWarning("Erro de validação: {Errors}", ex.Errors.Select(e => e.ErrorMessage));
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, "Erro de validação.",
                ex.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Acesso não autorizado: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Recurso não encontrado: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Operação inválida: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno não tratado.");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno. Tente novamente.");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string message, string[]? details = null)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = (int)statusCode,
            message,
            details,
            traceId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
