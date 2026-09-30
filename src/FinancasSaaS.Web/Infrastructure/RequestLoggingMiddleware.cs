using System.Diagnostics;

namespace FinancasSaaS.Web.Infrastructure;

public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startedAt);
            LogRequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds,
                correlationId);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return Guid.TryParse(context.Request.Headers["X-Correlation-ID"], out var correlationId)
            ? correlationId.ToString("D")
            : Guid.NewGuid().ToString("D");
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Requisição concluída. Método: {Method}; Caminho: {Path}; Status: {StatusCode}; DuraçãoMs: {DurationMs}; CorrelationId: {CorrelationId}")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string method,
        string? path,
        int statusCode,
        double durationMs,
        string correlationId);
}
