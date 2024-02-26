using Grpc.Net.Client.Configuration;
using OpenTournament.Data.Models;

namespace OpenTournament.Common.Draw.Layout;

public sealed record MatchesByRound(int Round, List<DrawMatch> Matches);

/*
public sealed class TournamentToJson
{
    public TournamentToJson(LocalMatches localMatches, List<Match> matches)
    {
        
    }

    public void Create(LocalMatches localMatches)
    {
        localMatches.Matches
            .GroupBy(by => by.Round)
            .Select(grp => {
                return new MatchesByRound(grp.Key, grp.ToList());
            });

    }
}*/