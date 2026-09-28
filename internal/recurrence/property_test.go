package recurrence_test

import (
	"slices"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/recurrence"
	"pgregory.net/rapid"
)

// For any schedule, time zone, window and now, the Occurrences are ordered,
// distinct, in UTC and inside the window, and NextEntry is exactly the next
// time they change by gaining an Occurrence.
func TestOccurrencesAndNextEntryAgreeForAnyWindow(t *testing.T) {
	rapid.Check(t, func(rt *rapid.T) {
		expr := rapid.SampledFrom([]string{
			"0 20 * * 1-5", "30 2 * * *", "0 * * * *", "*/15 * * * *", "* * * * *", "0 0 1 * *", "@daily",
			// Irregular enough that a window can hold exactly MaxOccurrences.
			"0-49 10,12 * * *", "*/7 1-3,9-17 * * 1-5",
		}).Draw(rt, "schedule")
		timeZone := rapid.SampledFrom([]string{
			"UTC", "Europe/Berlin", "America/New_York", "Australia/Lord_Howe", "Asia/Kolkata",
		}).Draw(rt, "timeZone")
		lookahead := time.Duration(rapid.Int64Range(int64(time.Minute), int64(7*24*time.Hour)).Draw(rt, "lookahead"))
		// Any second in 2026 and 2027, which covers four transitions per DST zone.
		now := time.Unix(rapid.Int64Range(1767225600, 1830297600).Draw(rt, "now"), 0).UTC()

		s, err := recurrence.Parse(expr, timeZone, lookahead)
		if err != nil {
			rt.Fatalf("Parse(%q, %q, %v) = %v", expr, timeZone, lookahead, err)
		}

		occurrences := s.Occurrences(now)
		if len(occurrences) > recurrence.MaxOccurrences {
			rt.Fatalf("%d Occurrences, want at most %d", len(occurrences), recurrence.MaxOccurrences)
		}
		for i, o := range occurrences {
			if o.Location() != time.UTC {
				rt.Errorf("Occurrence %v is in %v, want UTC", o, o.Location())
			}
			if o.Before(now) || o.After(now.Add(lookahead)) {
				rt.Errorf("Occurrence %v is outside [%v, %v]", o, now, now.Add(lookahead))
			}
			if i > 0 && !o.After(occurrences[i-1]) {
				rt.Errorf("Occurrence %v follows %v", o, occurrences[i-1])
			}
		}

		next, ok := s.NextEntry(now)
		if !ok {
			rt.Fatalf("NextEntry(%v) = false for %q, which fires forever", now, expr)
		}
		if !next.After(now) {
			rt.Fatalf("NextEntry(%v) = %v, want after now", now, next)
		}
		gained := func(at time.Time) bool {
			for _, o := range s.Occurrences(at) {
				if !slices.ContainsFunc(occurrences, o.Equal) {
					return true
				}
			}
			return false
		}
		if !gained(next) {
			rt.Errorf("Occurrences(%v) gains nothing over Occurrences(%v)", next, now)
		}
		// When the window is capped, the next entry can be 1ns away, with no
		// time in between.
		if next.Sub(now) > 1 {
			between := now.Add(time.Duration(rapid.Int64Range(1, int64(next.Sub(now))-1).Draw(rt, "between")))
			if gained(between) {
				rt.Errorf("Occurrences(%v) gains an Occurrence before NextEntry = %v", between, next)
			}
		}
	})
}
