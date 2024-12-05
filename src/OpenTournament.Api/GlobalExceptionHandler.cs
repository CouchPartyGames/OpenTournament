using Microsoft.AspNetCore.Diagnostics;

namespace OpenTournament.Api;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        GlobalExceptionHandlerLog.LogError(_logger, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = httpContext, 
            Exception = exception, 
        });
        return true;
    }
}

public static partial class GlobalExceptionHandlerLog
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Exception occurred: `{Message}`")]
    public static partial void LogError(ILogger logger, string message);
}