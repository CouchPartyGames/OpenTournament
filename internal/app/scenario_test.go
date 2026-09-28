package app_test

import (
	"fmt"
	"net/http"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

// These helpers describe Tournaments the way the API does, in the
// CONTEXT.md vocabulary.

type identity struct {
	Kind  string `json:"kind"`
	Value string `json:"value"`
}

type stageView struct {
	ID       string      `json:"id"`
	Position int         `json:"position"`
	Format   string      `json:"format"`
	Status   string      `json:"status"`
	Groups   []groupView `json:"groups"`
}

type groupView struct {
	ID           string                 `json:"id"`
	Status       string                 `json:"status"`
	Participants []groupParticipantView `json:"participants"`
	Standings    []standingView         `json:"standings"`
	Rounds       []roundView            `json:"rounds"`
}

type groupParticipantView struct {
	ParticipantID string `json:"participantId"`
	Seed          int    `json:"seed"`
}

type standingView struct {
	ParticipantID string `json:"participantId"`
	Position      int    `json:"position"`
	Points        int    `json:"points"`
	Wins          int    `json:"wins"`
	Eliminated    bool   `json:"eliminated"`
	Dropped       bool   `json:"dropped"`
}

type roundView struct {
	Round   int         `json:"round"`
	Bracket string      `json:"bracket"`
	Matches []matchView `json:"matches"`
}

type boutView struct {
	Bout    int `json:"bout"`
	Results []struct {
		ParticipantID string `json:"participantId"`
		Won           bool   `json:"won"`
		Forfeited     bool   `json:"forfeited"`
		Placement     int    `json:"placement"`
		Points        int    `json:"points"`
	} `json:"results"`
}

type matchView struct {
	ID              string     `json:"id"`
	Key             string     `json:"key"`
	Round           int        `json:"round"`
	Status          string     `json:"status"`
	Result          string     `json:"result"`
	WinnerID        string     `json:"winnerId"`
	Participants    []string   `json:"participants"`
	Bouts           []boutView `json:"bouts"`
	ServerAllocated bool       `json:"serverAllocated"`
	Aborts          int        `json:"aborts"`
	ServerAddress   string     `json:"serverAddress"`
	ServerPort      int        `json:"serverPort"`
}

type structureView struct {
	Status string      `json:"status"`
	Stages []stageView `json:"stages"`
}

type placementView struct {
	ParticipantID string   `json:"participantId"`
	Identity      identity `json:"identity"`
	From          int      `json:"from"`
	To            int      `json:"to"`
}

type participantView struct {
	ID          string   `json:"id"`
	Identity    identity `json:"identity"`
	Status      string   `json:"status"`
	CheckedInAt *string  `json:"checkedInAt"`
}

type tournamentView struct {
	ID         string `json:"id"`
	Status     string `json:"status"`
	Organizer  string `json:"organizer"`
	Registered int    `json:"registered"`
	CheckedIn  int    `json:"checkedIn"`
}

// scenario is a Tournament under test.
type scenario struct {
	*apptest.Harness
	t         *testing.T
	ID        string
	Organizer string
	// Tokens maps a Participant ID to the Keycloak token of the person behind it.
	Tokens map[string]string
	// Names maps a Participant ID to the person's name.
	Names map[string]string
}

var (
	opensIn  = 30 * time.Minute
	startsIn = time.Hour
)

// stage builds a Stage definition.
func stage(format string, extra map[string]any) map[string]any {
	s := map[string]any{"format": format, "resultDeadlineSeconds": 600}
	if format != "free-for-all" {
		s["bestOf"] = 1
	}
	for k, v := range extra {
		s[k] = v
	}
	return s
}

// settings is a valid Tournament for the arena Game, opening registration
// in 30 minutes and starting in an hour.
func settings(h *apptest.Harness, stages ...map[string]any) map[string]any {
	if len(stages) == 0 {
		stages = []map[string]any{stage("single-elimination", nil)}
	}
	now := h.FakeClock.Now()
	return map[string]any{
		"gameId":              "arena",
		"name":                "Friday Cup",
		"startsAt":            now.Add(startsIn),
		"registrationOpensAt": now.Add(opensIn),
		"capacity":            16,
		"minimumParticipants": 2,
		"stages":              stages,
	}
}

func mustCreate(t *testing.T, h *apptest.Harness, body map[string]any) *scenario {
	t.Helper()
	organizer := h.User("organizer")
	var tv tournamentView
	h.Do(http.MethodPost, "/api/v1/tournaments", organizer, body).Expect(http.StatusCreated).Decode(&tv)
	return &scenario{Harness: h, t: t, ID: tv.ID, Organizer: organizer, Tokens: map[string]string{}, Names: map[string]string{}}
}

func (tt *scenario) path(parts ...string) string {
	return "/api/v1/tournaments/" + tt.ID + apptest.Path(append([]string{""}, parts...)...)
}

// mustRegister signs people up as Participants with their own Keycloak identity.
func (tt *scenario) mustRegister(names ...string) []string {
	tt.t.Helper()
	var out []string
	for _, n := range names {
		token := tt.User(n)
		var p participantView
		tt.Do(http.MethodPost, tt.path("participants"), token, map[string]any{}).Expect(http.StatusCreated).Decode(&p)
		tt.Tokens[p.ID] = token
		tt.Names[p.ID] = n
		out = append(out, p.ID)
	}
	return out
}

// mustRegisterN registers n Participants named p1..pn.
func (tt *scenario) mustRegisterN(n int) []string {
	names := make([]string, n)
	for i := range names {
		names[i] = fmt.Sprintf("p%d", i+1)
	}
	return tt.mustRegister(names...)
}

func (tt *scenario) openRegistration() { tt.Advance(opensIn) }

func (tt *scenario) start() { tt.AdvanceTo(apptest.Epoch.Add(startsIn)) }

func (tt *scenario) get() tournamentView {
	tt.t.Helper()
	var tv tournamentView
	tt.Do(http.MethodGet, tt.path(), "", nil).Expect(http.StatusOK).Decode(&tv)
	return tv
}

func (tt *scenario) structure() structureView {
	tt.t.Helper()
	var s structureView
	tt.Do(http.MethodGet, tt.path("structure"), "", nil).Expect(http.StatusOK).Decode(&s)
	return s
}

// matches lists every Match of the Tournament in a given status.
func (tt *scenario) matches(status string) []matchView {
	tt.t.Helper()
	var out []matchView
	for _, st := range tt.structure().Stages {
		for _, g := range st.Groups {
			for _, r := range g.Rounds {
				for _, m := range r.Matches {
					if status == "" || m.Status == status {
						out = append(out, m)
					}
				}
			}
		}
	}
	return out
}

func (tt *scenario) placements() []placementView {
	tt.t.Helper()
	var body struct{ Placements []placementView }
	tt.Do(http.MethodGet, tt.path("placements"), "", nil).Expect(http.StatusOK).Decode(&body)
	return body.Placements
}

// server acts as the Game Server of a Match.
func (tt *scenario) server(match string) string { return tt.ServerToken(match) }

// playBout reports a head-to-head Bout won by winner, starting the Match first.
func (tt *scenario) playBout(match matchView, bout int, winner string) {
	tt.t.Helper()
	token := tt.server(match.ID)
	tt.Do(http.MethodPost, "/api/v1/game-server/match/started", token, nil).Expect(http.StatusNoContent)
	tt.Do(http.MethodPut, fmt.Sprintf("/api/v1/game-server/match/bouts/%d", bout), token,
		map[string]any{"winner": winner}).Expect(http.StatusNoContent)
	tt.Settle()
}

// playAll plays every In Progress or allocated head-to-head Match with pick
// choosing its winner, until no Match is left to play.
func (tt *scenario) playAll(pick func(a, b string) string) {
	tt.t.Helper()
	for range 1000 {
		playable := tt.matches("allocating")
		playable = append(playable, tt.matches("in-progress")...)
		if len(playable) == 0 {
			return
		}
		for _, m := range playable {
			tt.playBout(m, len(m.Bouts)+1, pick(m.Participants[0], m.Participants[1]))
		}
	}
	tt.t.Fatal("tournament never ran out of matches")
}

// byName makes the Participant with the alphabetically lower name win.
func (tt *scenario) byName(a, b string) string {
	if tt.Names[a] < tt.Names[b] {
		return a
	}
	return b
}

func (tt *scenario) placementOf(name string) [2]int {
	tt.t.Helper()
	for _, p := range tt.placements() {
		if tt.Names[p.ParticipantID] == name {
			return [2]int{p.From, p.To}
		}
	}
	tt.t.Fatalf("%s has no placement", name)
	return [2]int{}
}

// mustRun starts a Tournament of n Participants with the given Stages.
func mustRun(t *testing.T, n int, stages ...map[string]any) *scenario {
	t.Helper()
	h := apptest.MustStart(t)
	body := settings(h, stages...)
	body["minimumParticipants"] = max(n, 2)
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	tt.mustRegisterN(n)
	tt.start()
	return tt
}

func (tt *scenario) reportBout(token string, bout int, body any) *apptest.Response {
	tt.t.Helper()
	return tt.Do(http.MethodPut, fmt.Sprintf("/api/v1/game-server/match/bouts/%d", bout), token, body)
}

func (tt *scenario) match(id string) matchView {
	tt.t.Helper()
	var m matchView
	tt.Do(http.MethodGet, "/api/v1/matches/"+id, tt.Organizer, nil).Expect(http.StatusOK).Decode(&m)
	return m
}
