package app_test

import (
	"fmt"
	"net/http"
	"slices"
	"testing"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
)

func TestSwissGroupsIntoSingleEliminationRunToFinalPlacements(t *testing.T) {
	tt := mustRun(t, 8,
		stage("swiss", map[string]any{"groups": 2, "advancement": 2}),
		stage("single-elimination", nil),
	)
	s := tt.structure()
	if len(s.Stages[0].Groups) != 2 || len(s.Stages[0].Groups[0].Participants) != 4 {
		t.Fatalf("want two Swiss groups of four, got %+v", s.Stages[0].Groups)
	}

	// Play the Swiss stage.
	for tt.structure().Stages[0].Status != "completed" {
		for _, m := range append(tt.matches("allocating"), tt.matches("in-progress")...) {
			tt.playBout(m, 1, tt.byName(m.Participants[0], m.Participants[1]))
		}
	}

	s = tt.structure()
	groupOf := map[string]int{}
	var advanced []string
	for gi, g := range s.Stages[0].Groups {
		for _, e := range g.Participants {
			groupOf[e.ParticipantID] = gi
			if slices.ContainsFunc(g.Standings[:2], func(st standingView) bool { return st.ParticipantID == e.ParticipantID }) {
				advanced = append(advanced, e.ParticipantID)
			}
		}
	}
	semis := s.Stages[1].Groups[0].Rounds[0].Matches
	if len(semis) != 2 {
		t.Fatalf("want two semifinals, got %+v", semis)
	}
	for _, m := range semis {
		if !slices.Contains(advanced, m.Participants[0]) || !slices.Contains(advanced, m.Participants[1]) {
			t.Fatalf("semifinal %v has someone who didn't advance", m.Participants)
		}
		if groupOf[m.Participants[0]] == groupOf[m.Participants[1]] {
			t.Fatalf("group mates meet in the semifinal: %v", m.Participants)
		}
	}
	var list struct{ Participants []participantView }
	tt.Do(http.MethodGet, tt.path("participants"), "", nil).Expect(http.StatusOK).Decode(&list)
	for _, p := range list.Participants {
		if want := map[bool]string{true: "active", false: "eliminated"}[slices.Contains(advanced, p.ID)]; p.Status != want {
			t.Fatalf("%s is %s, want %s", tt.Names[p.ID], p.Status, want)
		}
	}

	tt.playAll(tt.byName)

	if tt.get().Status != "completed" {
		t.Fatal("tournament should be complete")
	}
	var ranges []string
	for _, p := range tt.placements() {
		ranges = append(ranges, fmt.Sprintf("%d-%d", p.From, p.To))
	}
	want := []string{"1-1", "2-2", "3-4", "3-4", "5-6", "5-6", "7-8", "7-8"}
	if !slices.Equal(ranges, want) {
		t.Fatalf("placement ranges %v, want %v", ranges, want)
	}
	if got := tt.placementOf("p1"); got != [2]int{1, 1} {
		t.Fatalf("p1 placed %v, want 1st", got)
	}
}

func TestRoundRobinIntoDoubleEliminationWithBracketReset(t *testing.T) {
	tt := mustRun(t, 5,
		stage("round-robin", map[string]any{"advancement": 4}),
		stage("double-elimination", nil),
	)
	rounds := tt.structure().Stages[0].Groups[0].Rounds
	if len(rounds) != 5 {
		t.Fatalf("5 Participants need 5 round robin rounds, got %d", len(rounds))
	}
	for tt.structure().Stages[0].Status != "completed" {
		ms := append(tt.matches("allocating"), tt.matches("in-progress")...)
		if len(ms) == 0 {
			t.Fatal("round robin stuck")
		}
		for _, m := range ms {
			tt.playBout(m, 1, tt.byName(m.Participants[0], m.Participants[1]))
		}
	}
	// In the double-elimination stage the best Participant loses only the first grand final.
	best := ""
	for id, n := range tt.Names {
		if n == "p1" {
			best = id
		}
	}
	gf1Played := false
	for range 100 {
		ms := append(tt.matches("allocating"), tt.matches("in-progress")...)
		if len(ms) == 0 {
			break
		}
		for _, m := range ms {
			w := tt.byName(m.Participants[0], m.Participants[1])
			if m.Key == "GF-M1" {
				gf1Played = true
				w = m.Participants[0]
				if w == best {
					w = m.Participants[1]
				}
			}
			tt.playBout(m, 1, w)
		}
	}

	if !gf1Played || tt.get().Status != "completed" {
		t.Fatalf("tournament %s, grand final played %v", tt.get().Status, gf1Played)
	}
	reset := false
	for _, m := range tt.matches("") {
		reset = reset || m.Key == "GF-M2"
	}
	if !reset {
		t.Fatal("want a bracket reset")
	}
	if got := tt.placementOf("p1"); got != [2]int{1, 1} {
		t.Fatalf("p1 placed %v, want 1st after the reset", got)
	}
	if got := tt.placementOf("p5"); got != [2]int{5, 5} {
		t.Fatalf("p5 placed %v, want 5th: knocked out in the round robin", got)
	}
}

func TestFreeForAllGroupsIntoAFreeForAllFinal(t *testing.T) {
	h := apptest.MustStart(t)
	body := settings(h,
		stage("free-for-all", map[string]any{"bouts": 2, "groups": 2, "advancement": 2}),
		stage("free-for-all", map[string]any{"bouts": 1}),
	)
	body["gameId"] = "royale"
	body["minimumParticipants"] = 6
	tt := mustCreate(t, h, body)
	tt.openRegistration()
	tt.mustRegisterN(8)
	tt.start()

	for range 10 {
		ms := append(tt.matches("allocating"), tt.matches("in-progress")...)
		if len(ms) == 0 {
			break
		}
		for _, m := range ms {
			// Finishing order by name, every bout; points 10, 6, 3, 1.
			order := slices.Clone(m.Participants)
			slices.SortFunc(order, func(a, b string) int {
				if tt.Names[a] < tt.Names[b] {
					return -1
				}
				return 1
			})
			token := tt.server(m.ID)
			for bout := len(m.Bouts) + 1; bout <= 2; bout++ {
				var pls []map[string]any
				for i, p := range order {
					pls = append(pls, map[string]any{"participantId": p, "placement": i + 1, "points": []int{10, 6, 3, 1}[i]})
				}
				tt.reportBout(token, bout, map[string]any{"placements": pls}).Expect(http.StatusNoContent)
				if tt.match(m.ID).Status == "completed" {
					break
				}
			}
			tt.Settle()
		}
	}

	if tt.get().Status != "completed" {
		t.Fatalf("status = %s", tt.get().Status)
	}
	final := tt.structure().Stages[1].Groups[0]
	if len(final.Participants) != 4 || final.Standings[0].Points != 10 {
		t.Fatalf("final = %+v", final)
	}
	if got := tt.placementOf("p1"); got != [2]int{1, 1} {
		t.Fatalf("p1 placed %v", got)
	}
	// 3rd of each group share 5th–6th.
	if got := tt.placementOf("p8"); got != [2]int{7, 8} {
		t.Fatalf("p8 placed %v, want 7th–8th", got)
	}
}
