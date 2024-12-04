using CouchPartyGames.TournamentGenerator.Type;
using OpenTournament.Api.Data.Models;

namespace OpenTournament.Api.Common.Extensions;

public static class LocalMatchExtensions
{
    public static Progression GetProgression(this Match<Participant> localMatch)
    {
        return localMatch switch
        {
            { WinProgression: Progression.NoProgression, LoseProgression: Progression.NoProgression } => Progression.NewNoProgression(),
            { LoseProgression: Progression.NoProgression } => Progression.NewWinProgression(localMatch.WinProgression),
            _ => Progression.NewWinLoseProgression(localMatch.WinProgression, localMatch.LoseProgression)
        };
    }
}