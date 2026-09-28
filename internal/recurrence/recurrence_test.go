package recurrence_test

import (
	"errors"
	"slices"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/recurrence"
)

// 2026-09-28 is a Monday. Europe/Berlin springs forward at 02:00 on
// 2026-03-29 and falls back at 03:00 on 2026-10-25.

func utc(year int, month time.Month, day, hour, min int) time.Time {
	return time.Date(year, month, day, hour, min, 0, 0, time.UTC)
}

func mustParse(t *testing.T, expr, timeZone string, lookahead time.Duration) recurrence.Schedule {
	t.Helper()
	s, err := recurrence.Parse(expr, timeZone, lookahead)
	if err != nil {
		t.Fatalf("Parse(%q, %q, %v) = %v", expr, timeZone, lookahead, err)
	}
	return s
}

func TestOccurrencesAreComputedInTheTimeZoneAndReturnedInUTC(t *testing.T) {
	weeknights := mustParse(t, "0 20 * * 1-5", "Europe/Berlin", 48*time.Hour)

	for _, tc := range []struct {
		name string
		now  time.Time
		want []time.Time
	}{
		{
			name: "Thursday and Friday",
			now:  utc(2026, time.October, 1, 10, 0),
			want: []time.Time{utc(2026, time.October, 1, 18, 0), utc(2026, time.October, 2, 18, 0)},
		},
		{
			name: "weekend",
			now:  utc(2026, time.October, 2, 19, 0),
			want: nil,
		},
		{
			name: "winter time",
			now:  utc(2026, time.December, 1, 10, 0),
			want: []time.Time{utc(2026, time.December, 1, 19, 0), utc(2026, time.December, 2, 19, 0)},
		},
	} {
		t.Run(tc.name, func(t *testing.T) {
			got := weeknights.Occurrences(tc.now)
			if !slices.EqualFunc(got, tc.want, time.Time.Equal) {
				t.Fatalf("Occurrences(%v) = %v, want %v", tc.now, got, tc.want)
			}
			for _, o := range got {
				if o.Location() != time.UTC {
					t.Errorf("Occurrence %v is in %v, want UTC", o, o.Location())
				}
			}
		})
	}
}

func TestTheWindowIncludesBothOfItsEnds(t *testing.T) {
	daily := mustParse(t, "0 20 * * *", "UTC", 24*time.Hour)

	now := utc(2026, time.October, 1, 20, 0)
	want := []time.Time{now, now.Add(24 * time.Hour)}
	if got := daily.Occurrences(now); !slices.EqualFunc(got, want, time.Time.Equal) {
		t.Errorf("Occurrences(%v) = %v, want %v", now, got, want)
	}

	justAfter := now.Add(time.Nanosecond)
	want = []time.Time{now.Add(24 * time.Hour)}
	if got := daily.Occurrences(justAfter); !slices.EqualFunc(got, want, time.Time.Equal) {
		t.Errorf("Occurrences(%v) = %v, want %v", justAfter, got, want)
	}
}

func TestTheLookaheadDefaultsTo24Hours(t *testing.T) {
	daily := mustParse(t, "0 20 * * *", "UTC", 0)

	now := utc(2026, time.October, 1, 20, 0)
	want := []time.Time{now, now.Add(24 * time.Hour)}
	if got := daily.Occurrences(now); !slices.EqualFunc(got, want, time.Time.Equal) {
		t.Errorf("Occurrences(%v) = %v, want %v", now, got, want)
	}
}

func TestDaylightSavingTransitionsNeitherDropNorRepeatAnOccurrence(t *testing.T) {
	for _, tc := range []struct {
		name      string
		expr      string
		now       time.Time
		lookahead time.Duration
		want      []time.Time
	}{
		{
			// 02:30 doesn't exist on 29 March, so that day's Occurrence
			// starts when the clocks jump, at 03:00 CEST.
			name:      "spring forward skips 02:30",
			expr:      "30 2 * * *",
			now:       utc(2026, time.March, 28, 12, 0),
			lookahead: 72 * time.Hour,
			want: []time.Time{
				utc(2026, time.March, 29, 1, 0),
				utc(2026, time.March, 30, 0, 30),
				utc(2026, time.March, 31, 0, 30),
			},
		},
		{
			name:      "the jump itself is now",
			expr:      "30 2 * * *",
			now:       utc(2026, time.March, 29, 1, 0),
			lookahead: 12 * time.Hour,
			want:      []time.Time{utc(2026, time.March, 29, 1, 0)},
		},
		{
			// 02:00 and 03:00 CEST are the same instant; it is one Occurrence.
			name:      "hourly across spring forward",
			expr:      "0 * * * *",
			now:       utc(2026, time.March, 29, 0, 0),
			lookahead: 2 * time.Hour,
			want: []time.Time{
				utc(2026, time.March, 29, 0, 0),
				utc(2026, time.March, 29, 1, 0),
				utc(2026, time.March, 29, 2, 0),
			},
		},
		{
			// 02:30 happens twice on 25 October; only the first counts.
			name:      "fall back repeats 02:30",
			expr:      "30 2 * * *",
			now:       utc(2026, time.October, 24, 12, 0),
			lookahead: 72 * time.Hour,
			want: []time.Time{
				utc(2026, time.October, 25, 0, 30),
				utc(2026, time.October, 26, 1, 30),
				utc(2026, time.October, 27, 1, 30),
			},
		},
		{
			// Inside the repeated hour, 02:30 has already happened once.
			name:      "now inside the repeated hour",
			expr:      "30 2 * * *",
			now:       utc(2026, time.October, 25, 1, 0),
			lookahead: 48*time.Hour + 30*time.Minute,
			want: []time.Time{
				utc(2026, time.October, 26, 1, 30),
				utc(2026, time.October, 27, 1, 30),
			},
		},
	} {
		t.Run(tc.name, func(t *testing.T) {
			s := mustParse(t, tc.expr, "Europe/Berlin", tc.lookahead)
			if got := s.Occurrences(tc.now); !slices.EqualFunc(got, tc.want, time.Time.Equal) {
				t.Errorf("Occurrences(%v) = %v, want %v", tc.now, got, tc.want)
			}
		})
	}
}

func TestAtMost100OccurrencesAreReturned(t *testing.T) {
	everyMinute := mustParse(t, "* * * * *", "UTC", 48*time.Hour)

	now := utc(2026, time.October, 1, 10, 0)
	got := everyMinute.Occurrences(now)
	if len(got) != recurrence.MaxOccurrences {
		t.Fatalf("len(Occurrences(%v)) = %d, want %d", now, len(got), recurrence.MaxOccurrences)
	}
	if last, want := got[len(got)-1], now.Add(99*time.Minute); !last.Equal(want) {
		t.Errorf("last Occurrence = %v, want %v", last, want)
	}
}

func TestNextEntry(t *testing.T) {
	for _, tc := range []struct {
		name      string
		expr      string
		timeZone  string
		lookahead time.Duration
		now       time.Time
		want      time.Time
	}{
		{
			name:      "the Occurrence after the window",
			expr:      "0 20 * * *",
			timeZone:  "UTC",
			lookahead: 24 * time.Hour,
			now:       utc(2026, time.October, 1, 10, 0),
			want:      utc(2026, time.October, 1, 20, 0),
		},
		{
			name:      "none in the current window",
			expr:      "0 20 * * 1-5",
			timeZone:  "Europe/Berlin",
			lookahead: 24 * time.Hour,
			now:       utc(2026, time.October, 2, 19, 0),
			want:      utc(2026, time.October, 4, 18, 0),
		},
		{
			// The window already holds more than 100 Occurrences, so the
			// 101st is returned as soon as the first one has started.
			name:      "capped window",
			expr:      "* * * * *",
			timeZone:  "UTC",
			lookahead: 48 * time.Hour,
			now:       utc(2026, time.October, 1, 10, 0),
			want:      utc(2026, time.October, 1, 10, 0).Add(time.Nanosecond),
		},
	} {
		t.Run(tc.name, func(t *testing.T) {
			s := mustParse(t, tc.expr, tc.timeZone, tc.lookahead)
			got, ok := s.NextEntry(tc.now)
			if !ok || !got.Equal(tc.want) {
				t.Errorf("NextEntry(%v) = %v, %v, want %v, true", tc.now, got, ok, tc.want)
			}
			if got.Location() != time.UTC {
				t.Errorf("NextEntry(%v) is in %v, want UTC", tc.now, got.Location())
			}
		})
	}
}

func TestAScheduleThatNeverFiresHasNoOccurrences(t *testing.T) {
	february30 := mustParse(t, "0 20 30 2 *", "UTC", 0)

	now := utc(2026, time.October, 1, 10, 0)
	if got := february30.Occurrences(now); len(got) != 0 {
		t.Errorf("Occurrences(%v) = %v, want none", now, got)
	}
	if got, ok := february30.NextEntry(now); ok {
		t.Errorf("NextEntry(%v) = %v, true, want false", now, got)
	}
}

func TestParseRejectsInvalidSchedules(t *testing.T) {
	for _, tc := range []struct {
		expr      string
		timeZone  string
		lookahead time.Duration
		want      error
	}{
		{"", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"0 20 * *", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"0 20 * * * *", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"61 20 * * *", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"0 20 * * mon-xyz", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"@every 1h", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"CRON_TZ=Europe/Berlin 0 20 * * *", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"TZ=Europe/Berlin 0 20 * * *", "UTC", 0, recurrence.ErrInvalidSchedule},
		{"0 20 * * *", "Mars/Olympus_Mons", 0, recurrence.ErrUnknownTimeZone},
		{"0 20 * * *", "Local", 0, recurrence.ErrUnknownTimeZone},
		{"0 20 * * *", "", 0, recurrence.ErrUnknownTimeZone},
		{"0 20 * * *", "UTC", -time.Hour, recurrence.ErrInvalidLookahead},
	} {
		_, err := recurrence.Parse(tc.expr, tc.timeZone, tc.lookahead)
		if !errors.Is(err, tc.want) {
			t.Errorf("Parse(%q, %q, %v) = %v, want %v", tc.expr, tc.timeZone, tc.lookahead, err, tc.want)
		}
	}
}

func TestParseAcceptsDescriptors(t *testing.T) {
	daily := mustParse(t, "@daily", "Europe/Berlin", 0)

	now := utc(2026, time.October, 1, 10, 0)
	want := []time.Time{utc(2026, time.October, 1, 22, 0)}
	if got := daily.Occurrences(now); !slices.EqualFunc(got, want, time.Time.Equal) {
		t.Errorf("Occurrences(%v) = %v, want %v", now, got, want)
	}
}
