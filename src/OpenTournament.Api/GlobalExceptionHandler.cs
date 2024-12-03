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
        _logger.LogError(
            exception, "Exception occurred: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext()
        {
            HttpContext = httpContext, 
            Exception = exception, 
        });
        return true;
    }
}
