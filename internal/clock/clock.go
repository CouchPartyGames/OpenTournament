// Package clock abstracts the current time so tests can move it forward.
package clock

import "time"

// Clock tells the time.
type Clock interface {
	Now() time.Time
}

// Real is the system clock.
type Real struct{}

// Now returns the current time in UTC.
func (Real) Now() time.Time { return time.Now().UTC() }
