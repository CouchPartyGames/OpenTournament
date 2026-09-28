// Package gameserver is the port through which the service coordinates the
// Game Servers Matches are played on (ADR-0001).
package gameserver

import (
	"context"
	"errors"

	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/ids"
)

// ErrNoCapacity is returned when no warm Game Server is available.
var ErrNoCapacity = errors.New("no game server available")

// AllocationRequest asks for one Game Server for one Match.
type AllocationRequest struct {
	Fleet        games.Fleet
	MatchID      ids.MatchID
	AllocationID ids.AllocationID
	// Token is the Match token handed to the Game Server.
	Token string
}

// State is what the service needs to know about a Game Server's health.
type State string

const (
	// Healthy servers are allocated and running.
	Healthy State = "healthy"
	// Failed servers are unhealthy, shutting down or in error.
	Failed State = "failed"
)

// Server is an allocated Game Server carrying the Open Tournament label.
type Server struct {
	Name         string
	Address      string
	Port         int
	MatchID      ids.MatchID
	AllocationID ids.AllocationID
	State        State
}

// Port allocates, watches, notifies and releases Game Servers.
type Port interface {
	// Allocate takes a warm Game Server from the Fleet for a Match.
	Allocate(ctx context.Context, req AllocationRequest) (Server, error)
	// Servers lists every Game Server allocated by the service.
	Servers(ctx context.Context) ([]Server, error)
	// Watch calls onChange whenever a Game Server changes, until ctx ends.
	Watch(ctx context.Context, onChange func()) error
	// NotifyForfeits tells a running Game Server which Participants forfeited.
	NotifyForfeits(ctx context.Context, server string, participants []ids.ParticipantID) error
	// Release gives a Game Server back to its Fleet. Releasing a server that
	// no longer exists is not an error.
	Release(ctx context.Context, server string) error
}
