namespace OpenTournament.Features;

public static class ValidationErrors
{

    public static readonly Dictionary<string, string[]> TournamentIdFailure = new()
    {
        { "tournamentId", [ "invalid format for tournament id" ] }
    };
    
    public static readonly Dictionary<string, string[]> MatchIdFailure = new()
    {
        { "matchId", [ "invalid format for match id" ] }
    };
    
    public static readonly Dictionary<string, string[]> ParticipantIdFailure = new()
    {
        { "participantId", [ "invalid format for participant id" ] }
    };
}