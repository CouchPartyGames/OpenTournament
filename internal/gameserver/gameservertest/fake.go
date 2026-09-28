// Package gameservertest provides an in-memory Game Server port for tests. It records
// allocations, releases and Forfeit notices, and lets a test make
// allocation fail or servers fail, disappear or leak.
package gameservertest

import (
	"context"
	"fmt"
	"slices"
	"sync"

	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/ids"
)

// Fake is a fake Game Server port.
type Fake struct {
	mu        sync.Mutex
	servers   map[string]gameserver.Server
	tokens    map[string]string
	unhealthy map[string]bool
	unlisted  map[string]bool
	next      int
	failures  int
	watchers  []func()
	forfeits  map[string][]ids.ParticipantID
	released  []string
	allocated []gameserver.AllocationRequest
}

var _ gameserver.Port = (*Fake)(nil)

// NewFake returns a fake port with unlimited capacity.
func NewFake() *Fake {
	return &Fake{
		servers:   map[string]gameserver.Server{},
		tokens:    map[string]string{},
		unhealthy: map[string]bool{},
		unlisted:  map[string]bool{},
		forfeits:  map[string][]ids.ParticipantID{},
	}
}

// Allocate hands out a new server, or fails while FailAllocations is in effect.
func (p *Fake) Allocate(_ context.Context, req gameserver.AllocationRequest) (gameserver.Server, error) {
	p.mu.Lock()
	defer p.mu.Unlock()
	if p.failures > 0 {
		p.failures--
		return gameserver.Server{}, gameserver.ErrNoCapacity
	}
	p.next++
	s := gameserver.Server{
		Name:         fmt.Sprintf("gs-%d", p.next),
		Address:      fmt.Sprintf("10.0.0.%d", p.next%250+1),
		Port:         7000 + p.next,
		MatchID:      req.MatchID,
		AllocationID: req.AllocationID,
		State:        gameserver.Healthy,
	}
	p.servers[s.Name] = s
	p.tokens[s.Name] = req.Token
	p.allocated = append(p.allocated, req)
	return s, nil
}

// Servers lists the servers that exist, except those held back by DelayListing.
func (p *Fake) Servers(context.Context) ([]gameserver.Server, error) {
	p.mu.Lock()
	defer p.mu.Unlock()
	out := make([]gameserver.Server, 0, len(p.servers))
	for _, s := range p.servers {
		if !p.unlisted[s.Name] {
			out = append(out, s)
		}
	}
	slices.SortFunc(out, func(a, b gameserver.Server) int { return compare(a.Name, b.Name) })
	return out, nil
}

// Lookup finds a server, even one kept out of Servers by DelayListing.
func (p *Fake) Lookup(_ context.Context, server string) (gameserver.Server, bool, error) {
	p.mu.Lock()
	defer p.mu.Unlock()
	s, ok := p.servers[server]
	return s, ok, nil
}

// Watch registers onChange for MakeUnhealthy and Delete, until ctx ends.
func (p *Fake) Watch(ctx context.Context, onChange func()) error {
	p.mu.Lock()
	p.watchers = append(p.watchers, onChange)
	p.mu.Unlock()
	<-ctx.Done()
	return nil
}

// NotifyForfeits records the Participants a server was told forfeited.
func (p *Fake) NotifyForfeits(_ context.Context, server string, participants []ids.ParticipantID) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.forfeits[server] = slices.Clone(participants)
	return nil
}

// Release removes a server and records that it was released.
func (p *Fake) Release(_ context.Context, server string) error {
	p.mu.Lock()
	defer p.mu.Unlock()
	if _, ok := p.servers[server]; ok {
		delete(p.servers, server)
		p.released = append(p.released, server)
	}
	return nil
}

// FailAllocations makes the next n allocations fail for lack of capacity.
func (p *Fake) FailAllocations(n int) {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.failures = n
}

// ServerFor returns the running server of a Match.
func (p *Fake) ServerFor(match ids.MatchID) (gameserver.Server, string, bool) {
	p.mu.Lock()
	defer p.mu.Unlock()
	for _, s := range p.servers {
		if s.MatchID == match {
			return s, p.tokens[s.Name], true
		}
	}
	return gameserver.Server{}, "", false
}

// TokenOf returns the Match token a server was allocated with, even after
// it was released.
func (p *Fake) TokenOf(server string) string {
	p.mu.Lock()
	defer p.mu.Unlock()
	return p.tokens[server]
}

// MakeUnhealthy marks a server as failed and tells watchers.
func (p *Fake) MakeUnhealthy(server string) {
	p.mu.Lock()
	s := p.servers[server]
	s.State = gameserver.Failed
	p.servers[server] = s
	p.mu.Unlock()
	p.notify()
}

// Delete removes a server. With silently, watchers aren't told, as when a
// server disappears while the service is down.
func (p *Fake) Delete(server string, silently bool) {
	p.mu.Lock()
	delete(p.servers, server)
	p.mu.Unlock()
	if !silently {
		p.notify()
	}
}

// Leak adds an allocated server that no Match references.
func (p *Fake) Leak(match ids.MatchID) string {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.next++
	name := fmt.Sprintf("gs-leaked-%d", p.next)
	p.servers[name] = gameserver.Server{Name: name, MatchID: match, AllocationID: ids.New[ids.AllocationID](), State: gameserver.Healthy}
	return name
}

// DelayListing keeps a server out of Servers, the way a lagging informer
// cache hasn't seen a fresh allocation yet. Lookup still finds it.
func (p *Fake) DelayListing(server string) {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.unlisted[server] = true
}

// Released lists the released servers, in order.
func (p *Fake) Released() []string {
	p.mu.Lock()
	defer p.mu.Unlock()
	return slices.Clone(p.released)
}

// Allocations lists every allocation request that succeeded, in order.
func (p *Fake) Allocations() []gameserver.AllocationRequest {
	p.mu.Lock()
	defer p.mu.Unlock()
	return slices.Clone(p.allocated)
}

// Forfeits returns the Participants a server was last told had forfeited.
func (p *Fake) Forfeits(server string) []ids.ParticipantID {
	p.mu.Lock()
	defer p.mu.Unlock()
	return slices.Clone(p.forfeits[server])
}

// Running counts the servers that exist.
func (p *Fake) Running() int {
	p.mu.Lock()
	defer p.mu.Unlock()
	return len(p.servers)
}

func (p *Fake) notify() {
	p.mu.Lock()
	ws := slices.Clone(p.watchers)
	p.mu.Unlock()
	for _, w := range ws {
		w()
	}
}

func compare(a, b string) int {
	switch {
	case a < b:
		return -1
	case a > b:
		return 1
	}
	return 0
}
