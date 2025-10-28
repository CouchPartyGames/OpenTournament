namespace OpenTournament.Api.Data.Models;

public sealed record UserId(Guid Id)
{
    public static UserId New() => new(Guid.CreateVersion7());
}