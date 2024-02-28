namespace OpenTournament.Common.Draw.Layout;

public sealed class CreateMatchIds
{
    public sealed record MatchWithId(int Round, int MatchId, int Position1 = -1, int Position2 = -1)
    {
        public static MatchWithId Create(int round, int matchId, VersusMatch match) =>
            new(round, matchId, match.FirstParticipant, match.SecondParticipant);
    }
    
    private int _matchId = 1;
    
    private readonly FirstRoundPositions _positions;
    
    public List<MatchWithId> MatchByIds { get; init; } = new();

    public CreateMatchIds(FirstRoundPositions positions)
    {
        _positions = positions;
        Create();
    }


    void Create()
    {
        for (int round = 1; round <= _positions.DrawSize.ToTotalRounds(); round++)
        {
            var totalMatches = GetTotalMatchesInRound(round);
            for (int j = 0; j < totalMatches; j++)
            {
                CreateMatchId(round);
            }
        }
    }
    
    void CreateMatchId(int round)
    {
        if (round == 1)
        {
            var positionMatch = _positions.Matches[_matchId - 1];
            MatchByIds.Add(MatchWithId.Create(round, _matchId, positionMatch));
        }
        else
        {
            MatchByIds.Add(new MatchWithId(round, _matchId));
        }
        _matchId++;
    }
    
    int GetTotalMatchesInRound(int round) => (int)_positions.DrawSize.Value / (int)Math.Pow(2, round);
}