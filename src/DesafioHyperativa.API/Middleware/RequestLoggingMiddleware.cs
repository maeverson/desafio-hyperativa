using System.Diagnostics;

namespace DesafioHyperativa.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        _logger.LogInformation(
            "Requisição recebida. TraceId: {TraceId} | Método: {Method} | Path: {Path} | IP: {IP}",
            requestId,
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "Resposta enviada. TraceId: {TraceId} | Status: {Status} | Duração: {ElapsedMs}ms",
                requestId,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
