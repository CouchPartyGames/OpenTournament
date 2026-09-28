// Package recurrence computes the Occurrences of a Recurring Tournament: the
// start times its schedule produces within a lookahead window.
//
// Like the Format engine, it is pure: it has no clock of its own and touches
// nothing but the time zone database. The caller passes "now", and the same
// inputs always produce the same outputs.
//
// A schedule is a standard five-field cron expression in its time zone.
// Daylight-saving transitions neither drop nor repeat an Occurrence; like
// Vixie cron, a schedule is read one of two ways:
//
//   - A schedule that matches every hour, such as every 15 minutes, follows
//     real time. It has no Occurrences in the hour the clocks skip, since that
//     hour never happens, and has them in both passes of an hour they repeat.
//   - Any other schedule names times on the wall clock, and each wall-clock
//     time it matches is one Occurrence. When the clocks skip a matching time
//     (02:30 when they spring forward from 02:00 to 03:00), its Occurrence
//     starts at the instant they jump. When they read a matching time twice
//     (02:30 when they fall back from 03:00 to 02:00), only the first counts.
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

// everyHour is the hour field of a schedule that matches every hour.
const everyHour = 1<<24 - 1

// Schedule is a Recurring Tournament's schedule: which Occurrences fall within
// its lookahead window at any given time.
type Schedule struct {
	// next returns the first Occurrence strictly after an instant, in UTC,
	// or zero if there is none.
	next      func(after time.Time) time.Time
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
	spec, ok := parsed.(*cron.SpecSchedule)
	if !ok {
		return Schedule{}, fmt.Errorf("%w %q: @every is not supported", ErrInvalidSchedule, expr)
	}

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

	s := Schedule{lookahead: lookahead}
	if spec.Hour&everyHour == everyHour {
		// robfig/cron steps through hours as they happen, which is real time.
		spec.Location = loc
		s.next = func(after time.Time) time.Time { return spec.Next(after).UTC() }
	} else {
		// spec matches wall-clock times, held as UTC times whose fields read
		// as the wall clock in loc.
		spec.Location = time.UTC
		s.next = func(after time.Time) time.Time { return nextOnWallClock(spec, loc, after) }
	}
	return s, nil
}

// Occurrences returns the Occurrences within [now, now+lookahead], in order and
// in UTC. Both ends of the window are included. At most MaxOccurrences are
// returned: the earliest ones.
func (s Schedule) Occurrences(now time.Time) []time.Time {
	occurrences, _ := s.scan(now)
	return occurrences
}

// NextEntry returns the earliest time after now at which Occurrences returns an
// Occurrence that it doesn't return at now, in UTC. That is usually when the
// next Occurrence after the window enters it. When the window already holds
// MaxOccurrences, it can be later: just after the first returned Occurrence
// has started, which makes room for the next.
//
// It reports false if the schedule never produces another Occurrence.
func (s Schedule) NextEntry(now time.Time) (time.Time, bool) {
	occurrences, following := s.scan(now)
	if following.IsZero() {
		return time.Time{}, false
	}
	entry := following.Add(-s.lookahead)
	if len(occurrences) == MaxOccurrences {
		if roomAt := occurrences[0].Add(time.Nanosecond); entry.Before(roomAt) {
			entry = roomAt
		}
	}
	return entry, true
}

// scan returns what Occurrences returns at now, and the Occurrence that
// follows them, or zero if there is none.
func (s Schedule) scan(now time.Time) (occurrences []time.Time, following time.Time) {
	end := now.Add(s.lookahead)
	// Starting just before now includes an Occurrence exactly at now.
	o := now.Add(-time.Nanosecond)
	for {
		o = s.next(o)
		if o.IsZero() || o.After(end) || len(occurrences) == MaxOccurrences {
			return occurrences, o
		}
		occurrences = append(occurrences, o)
	}
}

// nextOnWallClock returns, in UTC, the first Occurrence strictly after an
// instant for a schedule read on the wall clock in loc, or zero if there is
// none. wallClock matches wall-clock times held as UTC times.
func nextOnWallClock(wallClock *cron.SpecSchedule, loc *time.Location, after time.Time) time.Time {
	// instant never decreases as the wall clock advances, so no time on or
	// before after's own reading can have an Occurrence after it.
	wall := wallTime(after.In(loc))
	for {
		// robfig/cron gives up after five years without a match, as for
		// 30 February.
		wall = wallClock.Next(wall)
		if wall.IsZero() {
			return time.Time{}
		}
		// A later wall-clock time can still have an Occurrence that isn't
		// later: in an hour the clocks repeat, or when it is skipped and shares
		// the instant of the jump.
		if o := instant(wall, loc); o.After(after) {
			return o
		}
	}
}

// instant returns, in UTC, the first instant at which the clocks in loc read
// wall. If they skip wall, it returns the instant they jump. The result never
// decreases as wall increases, which nextOnWallClock relies on.
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
