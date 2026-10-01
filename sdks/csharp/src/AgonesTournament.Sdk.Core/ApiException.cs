using System.Net;
using System.Text.Json;

namespace AgonesTournament.Sdk.Core;

/// <summary>RFC 9457 problem details, including the API's stable code and validation fields.</summary>
public sealed record ProblemDetails
{
    public string? Type { get; init; }
    public required string Title { get; init; }
    public required long Status { get; init; }
    public string? Detail { get; init; }
    public required string Code { get; init; }
    public IReadOnlyList<FieldError>? Errors { get; init; }
}

/// <summary>An invalid input field; Value preserves arbitrary JSON supplied by the API.</summary>
public sealed record FieldError
{
    public string? Location { get; init; }
    public required string Message { get; init; }
    public JsonElement? Value { get; init; }
}

/// <summary>An API failure. Unknown problem codes retain their code and detail in this base exception.</summary>
public class ApiException(HttpStatusCode statusCode, ProblemDetails problem)
    : Exception(problem.Code + ": " + (problem.Detail ?? problem.Title))
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public ProblemDetails Problem { get; } = problem;
    public string Code => Problem.Code;
    public string? Detail => Problem.Detail;

    internal static ApiException From(HttpStatusCode status, ProblemDetails problem) => problem.Code switch
    {
        "match-token-invalid" => new MatchTokenInvalidException(status, problem),
        "bout-conflict" => new BoutConflictException(status, problem),
        "bout-already-forfeited" => new BoutAlreadyForfeitedException(status, problem),
        "bout-out-of-order" => new BoutOutOfOrderException(status, problem),
        "validation-failed" or "bad-request" => new ValidationException(status, problem),
        _ => new ApiException(status, problem)
    };
}

/// <summary>The Match token is invalid, expired, or belongs to an earlier allocation.</summary>
public sealed class MatchTokenInvalidException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
/// <summary>A different result is already recorded for the Bout.</summary>
public sealed class BoutConflictException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
/// <summary>The Bout has already been forfeited.</summary>
public sealed class BoutAlreadyForfeitedException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
/// <summary>The report is for a Bout other than the next one expected.</summary>
public sealed class BoutOutOfOrderException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
/// <summary>The request is invalid; Problem.Errors describes invalid fields when supplied by the API.</summary>
public sealed class ValidationException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
