namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record UserId(Guid Id)
{
    public static UserId New() => new(Guid.CreateVersion7());
}