namespace OpenTournament.Core.Features.Matches.Complete;

public sealed record CompleteMatchCommand(string MatchId, string WinnerId);