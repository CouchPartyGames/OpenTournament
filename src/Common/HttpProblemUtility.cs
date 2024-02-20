namespace OpenTournament.Common;

public sealed class HttpProblemUtility
{
    public ProblemHttpResult NotFoundToProblemDetails()
    {
        return TypedResults.Problem("Resource not found", 
            statusCode: StatusCodes.Status404NotFound , 
            title: "Not Found");
    }
    
    public ProblemHttpResult AuthenticationToProblemDetails()
    {
        return TypedResults.Problem("Unauthorized access", 
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Forbidden");
    }
    
    public ProblemHttpResult ForbiddenToProblemDetails()
    {
        return TypedResults.Problem("Forbidden access", 
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden");
    }
    
    public ProblemHttpResult InternalErrorToProblemDetails()
    {
        return TypedResults.Problem("Internal Error", 
            statusCode: 500, 
            title: "Forbidden");
    }

    public ProblemHttpResult ValidationErrorToProblemDetails(List<string> errors)
    {
        return TypedResults.Problem("Input Validation Failure", 
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Input Validation Failure");
    }
}