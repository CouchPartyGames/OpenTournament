using Microsoft.AspNetCore.Components.Web;

namespace OpenTournament.Common;

public sealed class HttpProblemUtility
{
    public static ValidationProblem ValidationProblemTournamentId()
    {
        return TypedResults.ValidationProblem(
            new Dictionary<string, string[]>
            {
                { "tournament id", [ "invalid format" ] }
            });
    }
    
    public static ProblemHttpResult NotFoundToProblemDetails()
    {
        return TypedResults.Problem("Resource not found", 
            statusCode: StatusCodes.Status404NotFound , 
            title: "Not Found");
    }
    
    public static ProblemHttpResult AuthenticationToProblemDetails()
    {
        return TypedResults.Problem("Unauthorized access", 
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Forbidden");
    }
    
    public static ProblemHttpResult ForbiddenToProblemDetails()
    {
        return TypedResults.Problem("Forbidden access", 
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden");
    }
    
    public static ProblemHttpResult InternalErrorToProblemDetails()
    {
        return TypedResults.Problem("Internal Error", 
            statusCode: 500, 
            title: "Forbidden");
    }

    public static ProblemHttpResult ValidationErrorToProblemDetails(Dictionary<string, object> errors)
    {
        return TypedResults.Problem("Input Validation Failure", 
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Input Validation Failure",
            extensions: errors);
    }

    /*
    public static ProblemHttpResult ConflictProblemDetails(List<string> errors)
    {
        
        return TypedResults.Conflict()
    }*/
}