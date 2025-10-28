namespace OpenTournament.Core.Domain.ValueObjects;

public sealed record PlatformId(Guid Value)
{
    public static PlatformId New() => new(Guid.CreateVersion7());
}
