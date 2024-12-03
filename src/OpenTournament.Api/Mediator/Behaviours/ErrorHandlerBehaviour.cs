namespace OpenTournament.Api.Mediator.Behaviours;


public sealed class ErrorHandlerBehaviour<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage // Constrained to IMessage, or constrain to IBaseCommand or any custom interface you've implemented
{
    private readonly ILogger<ErrorHandlerBehaviour<TMessage, TResponse>> _logger;

    public ErrorHandlerBehaviour(ILogger<ErrorHandlerBehaviour<TMessage, TResponse>> logger, IMediator mediator)
    {
        _logger = logger;
    }

    /*
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            var response = await next(message, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
            //await _mediator.Publish(new ErrorMessage(ex));
            throw;
        }
    }*/

    public ValueTask<TResponse> Handle(TMessage message, CancellationToken cancellationToken, MessageHandlerDelegate<TMessage, TResponse> next)
    {
        throw new NotImplementedException();
    }
}