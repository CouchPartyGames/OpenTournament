using Microsoft.AspNetCore.Diagnostics;

namespace OpenTournament.WebApi.Middleware.Exceptions;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        GlobalExceptionHandlerLog.LogError(logger, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext()
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
