namespace OpenTournament.Api.Common;

public sealed record Action(string Name, string Uri, string Method);

public sealed record ActionList(List<Action> Actions);