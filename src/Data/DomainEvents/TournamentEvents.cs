﻿using OpenTournament.Common.Draw.Layout;
using OpenTournament.Data.Models;

namespace OpenTournament.Data.DomainEvents;

public record TournamentStartedEvent(TournamentId TournamentId, DrawSize DrawSize) : IDomainEvent;
public record TournamentStarted(TournamentId TournamentId);

public record TournamentCompletedEvent(TournamentId TournamentId) : IDomainEvent;
public record TournamentCompleted(TournamentId TournamentId);

public record TournamentCreated(TournamentId TournamentId);

public record TournamentUpdated(TournamentId TournamentId);

public record TournamentDrawFinalized(TournamentId TournamentId);

public record TournamentReady(TournamentId TournamentId);


public record TournamentCancelled(TournamentId TournamentId, string Reason);
