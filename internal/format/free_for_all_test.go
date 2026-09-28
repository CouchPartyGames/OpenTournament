package format_test

import (
	"reflect"
	"testing"

	"github.com/couchpartygames/opentournament/internal/format"
)

// placed records a free-for-all Bout: results in finishing order, with points.
func (gr *group) placed(order []format.ParticipantID, points []int, forfeited ...format.ParticipantID) {
	gr.t.Helper()
	m := gr.match("M1")
	var results []format.BoutResult
	for i, who := range order {
		results = append(results, format.BoutResult{Participant: who, Placement: i + 1, Points: points[i]})
	}
	for _, who := range forfeited {
		results = append(results, format.BoutResult{Participant: who, Forfeited: true})
	}
	m.Bouts = append(m.Bouts, format.Bout{Number: len(m.Bouts) + 1, Results: results})
}

func TestFreeForAllGroupPlaysAsOneMatch(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.FreeForAll, Bouts: 3}, 5)

	p := gr.plan()

	if len(p.Matches) != 1 || p.Matches[0].State != format.Playable || len(p.Matches[0].Participants) != 5 {
		t.Fatalf("matches = %+v, want one playable match with all 5", p.Matches)
	}
}

func TestFreeForAllSumsPointsAcrossBoutsThenBestPlacement(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.FreeForAll, Bouts: 2}, 3)
	gr.plan()
	gr.placed([]format.ParticipantID{"p1", "p2", "p3"}, []int{10, 6, 5})
	if p := gr.plan(); p.Complete {
		t.Fatal("complete after 1 of 2 bouts")
	}
	gr.placed([]format.ParticipantID{"p3", "p2", "p1"}, []int{10, 6, 5})

	p := gr.plan()

	// p1 and p3 both have 15 points and a best placement of 1st; the Lot
	// decides. p2 has 12.
	if want := []format.ParticipantID{"p1", "p3", "p2"}; !reflect.DeepEqual(ranking(p), want) {
		t.Fatalf("ranking = %v, want %v", ranking(p), want)
	}
	if !p.Complete || p.Matches[0].State != format.Decided {
		t.Fatal("group should be complete after the configured bouts")
	}
	if p.Standings[0].Points != 15 || p.Standings[0].BestPlacement != 1 {
		t.Fatalf("p1 standing = %+v", p.Standings[0])
	}
}

func TestFreeForAllBestPlacementBreaksEqualPoints(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.FreeForAll, Bouts: 2}, 3)
	gr.plan()
	gr.placed([]format.ParticipantID{"p2", "p1", "p3"}, []int{5, 5, 0})
	gr.placed([]format.ParticipantID{"p3", "p1", "p2"}, []int{5, 5, 5})

	p := gr.plan()

	// p1 and p2 both have 10 points. p2 was 1st once, p1 never, so p2 ranks
	// higher even though p1 has the better Lot.
	if want := []format.ParticipantID{"p2", "p1", "p3"}; !reflect.DeepEqual(ranking(p), want) {
		t.Fatalf("ranking = %v, want %v", ranking(p), want)
	}
}

func TestFreeForAllForfeitedBoutScoresLastPlacementAndNoPoints(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.FreeForAll, Bouts: 1}, 3)
	gr.plan()
	gr.placed([]format.ParticipantID{"p2", "p3"}, []int{5, 3}, "p1")

	p := gr.plan()

	last := p.Standings[2]
	if last.Participant != "p1" || last.Points != 0 || last.BestPlacement != 3 {
		t.Fatalf("last standing = %+v, want p1 with 0 points placed 3rd", last)
	}
}

func TestFreeForAllCompletesWhenOneParticipantIsLeft(t *testing.T) {
	gr := newGroup(t, format.Stage{Format: format.FreeForAll, Bouts: 3}, 3)
	gr.plan()
	gr.placed([]format.ParticipantID{"p1", "p2", "p3"}, []int{1, 2, 3})
	gr.g.Dropped = []format.ParticipantID{"p1", "p2"}

	p := gr.plan()

	if !p.Complete {
		t.Fatal("group should complete once only p3 hasn't dropped")
	}
}
