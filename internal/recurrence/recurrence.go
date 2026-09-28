// Package recurrence computes the Occurrences of a Recurring Tournament: the
// start times its schedule produces within a lookahead window.
//
// Like the Format engine, it is pure: it has no clock of its own and touches
// nothing but the time zone database. The caller passes "now", and the same
// inputs always produce the same outputs.
//
// A schedule is a standard five-field cron expression read on the wall clock
// of its time zone. Each wall-clock time the expression matches is at most one
// Occurrence, so daylight-saving transitions neither drop nor repeat one:
//
//   - When the clocks skip a matching time (02:30 when they spring forward from
//     02:00 to 03:00), its Occurrence starts at the instant they jump.
//   - When the clocks read a matching time twice (02:30 when they fall back
//     from 03:00 to 02:00), only the first counts.
//
// An hourly schedule therefore has no Occurrence in the hour the clocks
// repeat, since its wall-clock times have already been used.
package recurrence

import (
	"errors"
	"fmt"
	"strings"
	"time"

	"github.com/robfig/cron/v3"
)

// DefaultLookahead is the window used when Parse is given no lookahead.
const DefaultLookahead = 24 * time.Hour

// MaxOccurrences is the most Occurrences returned for one window. It protects
// the service from a runaway schedule, such as every minute with a long window.
const MaxOccurrences = 100

var (
	// ErrInvalidSchedule means the cron expression can't be parsed.
	ErrInvalidSchedule = errors.New("invalid schedule")
	// ErrUnknownTimeZone means the time zone isn't in the time zone database.
	ErrUnknownTimeZone = errors.New("unknown time zone")
	// ErrInvalidLookahead means the lookahead window is negative.
	ErrInvalidLookahead = errors.New("invalid lookahead")
)

// parser reads standard five-field cron expressions and descriptors such as
// @daily.
var parser = cron.NewParser(cron.Minute | cron.Hour | cron.Dom | cron.Month | cron.Dow | cron.Descriptor)

// Schedule is a Recurring Tournament's schedule: which Occurrences fall within
// its lookahead window at any given time.
type Schedule struct {
	// wallClock matches wall-clock times, held as UTC times whose fields
	// read as the wall clock in loc.
	wallClock *cron.SpecSchedule
	loc       *time.Location
	lookahead time.Duration
}

// Parse returns the Schedule for a cron expression read in timeZone, an IANA
// name such as "Europe/Berlin". A zero lookahead means DefaultLookahead.
//
// The error wraps ErrInvalidSchedule, ErrUnknownTimeZone or
// ErrInvalidLookahead.
func Parse(expr, timeZone string, lookahead time.Duration) (Schedule, error) {
	// robfig/cron accepts a time zone prefix, which would compete with timeZone.
	if strings.HasPrefix(expr, "TZ=") || strings.HasPrefix(expr, "CRON_TZ=") {
		return Schedule{}, fmt.Errorf("%w %q: set the time zone separately, not in the schedule", ErrInvalidSchedule, expr)
	}
	parsed, err := parser.Parse(expr)
	if err != nil {
		return Schedule{}, fmt.Errorf("%w %q: %v", ErrInvalidSchedule, expr, err)
	}
	// @every counts from whenever it is asked, so its Occurrences would move
	// with now.
	wallClock, ok := parsed.(*cron.SpecSchedule)
	if !ok {
		return Schedule{}, fmt.Errorf("%w %q: @every is not supported", ErrInvalidSchedule, expr)
	}
	wallClock.Location = time.UTC

	// "" and "Local" are valid for time.LoadLocation, but they depend on the
	// host rather than on the Recurring Tournament.
	if timeZone == "" || timeZone == "Local" {
		return Schedule{}, fmt.Errorf("%w %q: name an IANA time zone such as UTC or Europe/Berlin", ErrUnknownTimeZone, timeZone)
	}
	loc, err := time.LoadLocation(timeZone)
	if err != nil {
		return Schedule{}, fmt.Errorf("%w %q: %v", ErrUnknownTimeZone, timeZone, err)
	}

	if lookahead < 0 {
		return Schedule{}, fmt.Errorf("%w %v: must not be negative", ErrInvalidLookahead, lookahead)
	}
	if lookahead == 0 {
		lookahead = DefaultLookahead
	}
	return Schedule{wallClock: wallClock, loc: loc, lookahead: lookahead}, nil
}

// Occurrences returns the Occurrences within [now, now+lookahead], in order and
// in UTC. Both ends of the window are included. At most MaxOccurrences are
// returned: the earliest ones.
func (s Schedule) Occurrences(now time.Time) []time.Time {
	occurrences, _ := s.scan(now)
	return occurrences[:min(len(occurrences), MaxOccurrences)]
}

// NextEntry returns the earliest time after now at which Occurrences returns an
// Occurrence that it doesn't return at now, in UTC. That is usually when the
// next Occurrence after the window enters it. When the window holds more than
// MaxOccurrences, it is instead just after the earliest returned Occurrence,
// when that one leaves the window and makes room for the next.
//
// It reports false if the schedule never produces another Occurrence.
func (s Schedule) NextEntry(now time.Time) (time.Time, bool) {
	occurrences, after := s.scan(now)
	switch {
	case len(occurrences) > MaxOccurrences:
		return occurrences[0].Add(time.Nanosecond), true
	case after.IsZero():
		return time.Time{}, false
	}
	return after.Add(-s.lookahead), true
}

// scan returns the Occurrences within the window at now, stopping after
// MaxOccurrences+1 so callers can tell the window was capped. If it wasn't,
// it also returns the first Occurrence after the window, or zero if none.
func (s Schedule) scan(now time.Time) (occurrences []time.Time, after time.Time) {
	end := now.Add(s.lookahead)
	// Every wall-clock time whose Occurrence is at or after now comes after
	// the wall-clock reading just before now. (Reading it at now would miss a
	// skipped time whose Occurrence is exactly now, the instant of the jump.)
	wall := wallTime(now.Add(-time.Nanosecond).In(s.loc))
	var last time.Time
	for len(occurrences) <= MaxOccurrences {
		// The next matching wall-clock time. robfig/cron gives up after five
		// years without a match, as for 30 February.
		wall = s.wallClock.Next(wall)
		if wall.IsZero() {
			return occurrences, time.Time{}
		}
		o := instant(wall, s.loc)
		// A wall-clock time just after now can still be before now when now
		// is in an hour the clocks repeat; and a skipped wall-clock time shares
		// its instant with the first time after the jump.
		if o.Before(now) || o.Equal(last) {
			continue
		}
		if o.After(end) {
			return occurrences, o
		}
		occurrences = append(occurrences, o)
		last = o
	}
	return occurrences, time.Time{}
}

// instant returns, in UTC, the first instant at which the clocks in loc read
// wall. If they skip wall, it returns the instant they jump. The result never
// decreases as wall increases, which scan relies on.
func instant(wall time.Time, loc *time.Location) time.Time {
	t := time.Date(wall.Year(), wall.Month(), wall.Day(), wall.Hour(), wall.Minute(), wall.Second(), 0, loc)
	// For a skipped wall-clock time, time.Date picks an instant on either side
	// of the jump, which reads differently.
	switch read := wallTime(t); {
	case read.After(wall):
		jump, _ := t.ZoneBounds()
		return jump.UTC()
	case read.Before(wall):
		_, jump := t.ZoneBounds()
		return jump.UTC()
	}
	// For a repeated wall-clock time, time.Date may pick the later instant.
	// If so, the clocks read wall in the previous zone too, and earlier.
	if start, _ := t.ZoneBounds(); !start.IsZero() {
		_, offset := t.Zone()
		_, previousOffset := start.Add(-time.Nanosecond).Zone()
		earlier := t.Add(time.Duration(offset-previousOffset) * time.Second)
		if earlier.Before(t) && wallTime(earlier.In(loc)).Equal(wall) {
			return earlier.UTC()
		}
	}
	return t.UTC()
}

// wallTime returns t's wall-clock reading as a UTC time with the same fields.
func wallTime(t time.Time) time.Time {
	return time.Date(t.Year(), t.Month(), t.Day(), t.Hour(), t.Minute(), t.Second(), t.Nanosecond(), time.UTC)
}
