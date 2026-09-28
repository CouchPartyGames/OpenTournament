package tournaments

import (
	"fmt"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/problem"
)

// NewTournament creates a Tournament for a Game.
type NewTournament struct {
	GameID string `json:"gameId" minLength:"1" doc:"The Game the Tournament is played in"`
	TournamentSettings
}

// TournamentSettings are everything an Organizer configures, frozen once registration opens.
type TournamentSettings struct {
	Name                string          `json:"name" minLength:"1" maxLength:"200"`
	StartsAt            time.Time       `json:"startsAt" doc:"When the Tournament starts, automatically"`
	RegistrationOpensAt time.Time       `json:"registrationOpensAt" doc:"When the Registration Window opens; it closes at the start"`
	Capacity            int32           `json:"capacity" minimum:"2" maximum:"100000" doc:"The most Participants accepted, first come, first served"`
	MinimumParticipants int32           `json:"minimumParticipants" minimum:"2" doc:"Below this many at the start, the Tournament is cancelled"`
	CheckIn             CheckInSettings `json:"checkIn,omitempty"`
	Stages              []StageSettings `json:"stages" minItems:"1" maxItems:"10" doc:"The Stages, played in order"`
}

// CheckInSettings configures the optional Check-in step.
type CheckInSettings struct {
	Enabled       bool  `json:"enabled" doc:"Participants must check in or are dropped at the start"`
	WindowSeconds int32 `json:"windowSeconds,omitempty" minimum:"0" doc:"Length of the Check-in Window, ending at the start"`
}

// StageSettings configures one Stage.
type StageSettings struct {
	Format                format.Kind `json:"format"`
	Groups                int32       `json:"groups,omitempty" minimum:"1" maximum:"1024" default:"1" doc:"Groups the Stage is split into"`
	Advancement           int32       `json:"advancement,omitempty" minimum:"0" doc:"Participants per Group who advance to the next Stage; 0 on the last Stage"`
	BestOf                int32       `json:"bestOf,omitempty" enum:"1,3" doc:"Head-to-head Formats: Bouts per Match"`
	Bouts                 int32       `json:"bouts,omitempty" minimum:"1" maximum:"100" doc:"Free-for-all: Bouts per Match"`
	SwissRounds           int32       `json:"swissRounds,omitempty" minimum:"0" maximum:"100" doc:"Swiss: overrides the default ⌈log₂ N⌉ Rounds"`
	ResultDeadlineSeconds int32       `json:"resultDeadlineSeconds" minimum:"60" maximum:"604800" doc:"Time a Match has to complete, from when it is Ready"`
}

// settings are the TournamentSettings the Tournament is configured with.
func (v TournamentView) settings() TournamentSettings {
	s := TournamentSettings{
		Name: v.Name, StartsAt: v.StartsAt, RegistrationOpensAt: v.RegistrationOpensAt, Capacity: v.Capacity,
		MinimumParticipants: v.MinimumParticipants, CheckIn: v.CheckIn, Stages: make([]StageSettings, len(v.Stages)),
	}
	for i, st := range v.Stages {
		s.Stages[i] = st.StageSettings
	}
	return s
}

// equal reports whether s and o configure a Tournament the same way. It
// compares every field, and instants rather than their time zones.
func (s TournamentSettings) equal(o TournamentSettings) bool {
	return s.Name == o.Name && s.StartsAt.Equal(o.StartsAt) && s.RegistrationOpensAt.Equal(o.RegistrationOpensAt) &&
		s.Capacity == o.Capacity && s.MinimumParticipants == o.MinimumParticipants && s.CheckIn == o.CheckIn &&
		slices.Equal(s.Stages, o.Stages)
}

// validate checks the rules that span several fields, and reports every
// violation at once.
func (s TournamentSettings) validate(game games.Game, now time.Time) error {
	var f problem.Fields
	if !s.StartsAt.After(now) {
		f.Add("body.startsAt", "must be in the future", s.StartsAt)
	}
	if !s.RegistrationOpensAt.Before(s.StartsAt) {
		f.Add("body.registrationOpensAt", "must be before startsAt", s.RegistrationOpensAt)
	}
	if s.MinimumParticipants > s.Capacity {
		f.Add("body.minimumParticipants", "must not exceed capacity", s.MinimumParticipants)
	}
	if s.CheckIn.Enabled {
		window := time.Duration(s.CheckIn.WindowSeconds) * time.Second
		if window < time.Minute {
			f.Add("body.checkIn.windowSeconds", "must be at least 60 when check-in is enabled", s.CheckIn.WindowSeconds)
		} else if window > s.StartsAt.Sub(s.RegistrationOpensAt) {
			f.Add("body.checkIn.windowSeconds", "must fit inside the Registration Window", s.CheckIn.WindowSeconds)
		}
	}

	// Participant counts entering each Stage: the first between Minimum
	// Participants and Capacity, later ones exactly Groups × Advancement.
	low, high := int(s.MinimumParticipants), int(s.Capacity)
	for i, st := range s.Stages {
		at := func(field string) string { return fmt.Sprintf("body.stages[%d].%s", i, field) }
		last := i == len(s.Stages)-1
		groups := max(int(st.Groups), 1)
		if st.Format.HeadToHead() && st.BestOf != 1 && st.BestOf != 3 {
			f.Add(at("bestOf"), "head-to-head formats need bestOf 1 or 3", st.BestOf)
		}
		if !st.Format.HeadToHead() && st.BestOf != 0 {
			f.Add(at("bestOf"), "only head-to-head formats have a best-of", st.BestOf)
		}
		if st.Format == format.FreeForAll && st.Bouts < 1 {
			f.Add(at("bouts"), "free-for-all needs at least one bout per match", st.Bouts)
		}
		if st.Format != format.FreeForAll && st.Bouts != 0 {
			f.Add(at("bouts"), "only free-for-all sets bouts", st.Bouts)
		}
		if st.Format != format.Swiss && st.SwissRounds != 0 {
			f.Add(at("swissRounds"), "only swiss sets swissRounds", st.SwissRounds)
		}
		if last && st.Advancement != 0 {
			f.Add(at("advancement"), "the last stage has no advancement", st.Advancement)
		}
		if !last && st.Advancement < 1 {
			f.Add(at("advancement"), "must be at least 1 when another stage follows", st.Advancement)
		}
		smallest := low / groups
		if need := max(int(st.Advancement)+1, 2); smallest < need {
			f.Add(at("groups"), fmt.Sprintf(
				"with only %d participants, a group could have %d, fewer than the %d it needs", low, smallest, need), st.Groups)
		}
		if largest := (high + groups - 1) / groups; st.Format == format.FreeForAll && largest > game.MaximumMatchSize {
			f.Add(at("groups"), fmt.Sprintf(
				"with %d participants, a free-for-all group could have %d, more than the game's maximum match size of %d",
				high, largest, game.MaximumMatchSize), st.Groups)
		}
		low = groups * int(st.Advancement)
		high = low
	}
	return f.Err()
}
