namespace OpenTournament.Mediator.Behaviours;

using FluentValidation;

public sealed class ValidationPipelineBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull, IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
        Console.WriteLine(validators);
        Console.WriteLine(validators.Any());
    }


    public ValueTask<TResponse> Handle(TRequest message, CancellationToken cancellationToken, MessageHandlerDelegate<TRequest, TResponse> next)
    {
        if (!_validators.Any())
        {
            //Console.WriteLine("no validation necessary " + message);
            return next(message, cancellationToken);
        }
        
        //Console.WriteLine("validation");
        return next(message, cancellationToken);
    }
}
