using System.Net;
using System.Text.Json;

namespace AgonesTournament.Sdk.Core;

/// <summary>RFC 9457 problem details, including the API's stable code and validation fields.</summary>
public sealed record ProblemDetails
{
    /// <summary>The URI reference identifying the problem type.</summary>
    public string? Type { get; init; }
    /// <summary>The short summary of the problem.</summary>
    public required string Title { get; init; }
    /// <summary>The status supplied in the problem response.</summary>
    public required long Status { get; init; }
    /// <summary>The explanation of this occurrence, when supplied.</summary>
    public string? Detail { get; init; }
    /// <summary>The stable problem code, preserved for unknown codes as well.</summary>
    public required string Code { get; init; }
    /// <summary>Every invalid field, when supplied by the API; the collection may be null.</summary>
    public IReadOnlyList<FieldError>? Errors { get; init; }
}

/// <summary>An invalid input field; Value preserves arbitrary JSON supplied by the API.</summary>
public sealed record FieldError
{
    /// <summary>The input location, such as body.winner, when supplied.</summary>
    public string? Location { get; init; }
    /// <summary>What is wrong with the input field.</summary>
    public required string Message { get; init; }
    /// <summary>The offending value as arbitrary JSON, when supplied.</summary>
    public JsonElement? Value { get; init; }
}

/// <summary>An API failure. Unknown problem codes retain their code and detail in this base exception.</summary>
public class ApiException(HttpStatusCode statusCode, ProblemDetails problem)
    : Exception(problem.Code + ": " + (problem.Detail ?? problem.Title))
{
    /// <summary>The actual HTTP response status.</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;
    /// <summary>The complete problem details supplied by the API.</summary>
    public ProblemDetails Problem { get; } = problem;
    /// <summary>The stable problem code, preserved for unknown codes as well.</summary>
    public string Code => Problem.Code;
    /// <summary>The explanation of this occurrence, when supplied.</summary>
    public string? Detail => Problem.Detail;

    internal static ApiException From(HttpStatusCode status, ProblemDetails problem) => problem.Code switch
    {
        "match-not-stalled" => new MatchNotStalledException(status, problem),
        "not-organizer" => new NotOrganizerException(status, problem),
        "settings-frozen" => new SettingsFrozenException(status, problem),
        "declared-in-git" => new DeclaredInGitException(status, problem),
        "not-trusted-for-game" => new NotTrustedForGameException(status, problem),
        "registration-closed" => new RegistrationClosedException(status, problem),
        "tournament-full" => new TournamentFullException(status, problem),
        "already-registered" => new AlreadyRegisteredException(status, problem),
        "identity-kind-not-accepted" => new PlayerIdentityKindNotAcceptedException(status, problem),
        "identity-not-owned" => new PlayerIdentityNotOwnedException(status, problem),
        "check-in-closed" => new CheckInClosedException(status, problem),
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

/// <summary>The Registration Window is closed.</summary>
public sealed class RegistrationClosedException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The Tournament has reached Capacity.</summary>
public sealed class TournamentFullException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The Player Identity is already registered in this Tournament.</summary>
public sealed class AlreadyRegisteredException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The Game does not accept this Player Identity kind.</summary>
public sealed class PlayerIdentityKindNotAcceptedException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The caller does not own the requested Player Identity.</summary>
public sealed class PlayerIdentityNotOwnedException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The Check-in Window is closed.</summary>
public sealed class CheckInClosedException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>Tournament settings are frozen because registration has opened.</summary>
public sealed class SettingsFrozenException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>A Declared Tournament's settings must be edited in git through its Manifest.</summary>
public sealed class DeclaredInGitException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The Game does not trust this backend to create Tournaments or act for Player Identities.</summary>
public sealed class NotTrustedForGameException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>Only a Stalled Match can be manually resolved.</summary>
public sealed class MatchNotStalledException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);

/// <summary>The caller is not the Tournament's Organizer.</summary>
public sealed class NotOrganizerException(HttpStatusCode statusCode, ProblemDetails problem) : ApiException(statusCode, problem);
