// Package clocktest provides a clock for tests that only moves when told to.
package clocktest

import (
	"sync"
	"time"
)

// Fake is a clock that only moves when told to.
type Fake struct {
	mu  sync.Mutex
	now time.Time
}

// NewFake returns a Fake clock set to now.
func NewFake(now time.Time) *Fake { return &Fake{now: now.UTC()} }

// Now returns the time the clock was last set or advanced to.
func (f *Fake) Now() time.Time {
	f.mu.Lock()
	defer f.mu.Unlock()
	return f.now
}

// Advance moves the clock forward by d.
func (f *Fake) Advance(d time.Duration) {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.now = f.now.Add(d)
}

// Set moves the clock to t.
func (f *Fake) Set(t time.Time) {
	f.mu.Lock()
	defer f.mu.Unlock()
	f.now = t.UTC()
}
