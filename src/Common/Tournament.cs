namespace OpenTournament.Common;

public sealed record TournamentId(Guid Value);

public sealed class Tournament
{
    //public TournamentId Id { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; }   
}