using CouchPartyGames.TournamentGenerator;
using CouchPartyGames.TournamentGenerator.Position;
using OpenTournament.Api.Data.Models;
using OpenTournament.Api.DomainEvents;

namespace OpenTournament.Api.Common.Extensions;

public static class TournamentUtility
{
    /*
    public static Tournament GetLocalTournament(TournamentStarted message)
    {
        
        var tournament = new SingleEliminationBuilder<Participant>("Temporary")
            .SetSize(DrawSize.NewRoundBase2(numOpponents).Value)
            .SetSeeding(TournamentSeeding.Ranked)
            .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
            .WithOpponents(oppList, GlobalConstants.ByeOpponent)
            .Build();
            
        var tournament = new DoubleEliminationBuilder<Participant>("Temporary")
            .SetSize(DrawSize.NewRoundBase2(numOpponents).Value)
            .SetSeeding(TournamentSeeding.Ranked)
            .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
            .WithOpponents(oppList, GlobalConstants.ByeOpponent)
            .Build();
        
            
        return tournament;
    }
    */
    
    
    public static Tournament<Participant> GetLocalDoubleTournament(TournamentStarted message, 
        List<Participant> oppList, 
        Participant bye) => 
        new DoubleEliminationBuilder<Participant>("Temporary")
            .SetSize(DrawSize.NewRoundBase2(message.DrawSize).Value)
            .SetSeeding(TournamentSeeding.Ranked)
            .WithOpponents(oppList, bye)
            .Build();
    
    public static Tournament<Participant> GetLocalSingleTournament(TournamentStarted message, 
        List<Participant> oppList,
        Participant bye) => 
        new SingleEliminationBuilder<Participant>("Temporary")
            .SetSize(DrawSize.NewRoundBase2(message.DrawSize).Value)
            .SetSeeding(TournamentSeeding.Ranked)
            .Set3rdPlace(Tournament3rdPlace.NoThirdPlace)
            .WithOpponents(oppList, bye)
            .Build();

    public static DrawSeeding GetDrawSeeding(this TournamentSeeding seed) => seed switch
    {
        TournamentSeeding.Random => DrawSeeding.Random,
        _ => DrawSeeding.Seeded
    };

    public static TournamentSeeding GetTournamentSeeding(this DrawSeeding seed) => seed switch
    {
        DrawSeeding.Random => TournamentSeeding.Random,
        _ => TournamentSeeding.Ranked
    };

    /*
    public static  Get3rdPlace(this Tournament3rdPlace place) => place switch
    {

    };*/

}