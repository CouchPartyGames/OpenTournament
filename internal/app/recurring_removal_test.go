package app_test

import (
	"context"
	"fmt"
	"slices"
	"strings"
	"sync/atomic"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime"
	"k8s.io/apimachinery/pkg/runtime/schema"
	ktesting "k8s.io/client-go/testing"
)

func mustDeleteRecurring(t *testing.T, h *apptest.Harness, namespace, name string) {
	t.Helper()
	if err := h.FakeKubernetes.Resource(manifests.RecurringResource).Namespace(namespace).Delete(context.Background(), name, metav1.DeleteOptions{}); err != nil {
		t.Fatalf("delete recurring tournament %s/%s: %v", namespace, name, err)
	}
}

// mustFindOccurrence finds the Tournament of the Occurrence at start.
func mustFindOccurrence(t *testing.T, h *apptest.Harness, start time.Time) *scenario {
	t.Helper()
	for _, o := range listOccurrences(h) {
		if o.StartsAt.Equal(start) {
			return &scenario{Harness: h, t: t, ID: o.ID, Organizer: h.Client("arena-backend"), Tokens: map[string]string{}, Names: map[string]string{}}
		}
	}
	t.Fatalf("no Tournament starts at %v", start)
	return nil
}

func TestDeletingARecurringTournamentRemovesItsUnstartedOccurrences(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()
	// Tuesday's Occurrence is played to completion.
	h.AdvanceTo(tuesdayAt20.Add(-30 * time.Minute))
	tuesday := mustFindOccurrence(t, h, tuesdayAt20)
	tuesday.mustRegister("anna", "bert")
	h.AdvanceTo(tuesdayAt20)
	tuesday.playAll(tuesday.byName)
	// Wednesday's Occurrence starts, and is still running.
	h.AdvanceTo(wednesdayAt20.Add(-30 * time.Minute))
	wednesday := mustFindOccurrence(t, h, wednesdayAt20)
	wednesday.mustRegister("anna", "bert")
	h.AdvanceTo(wednesdayAt20)
	// Thursday's registration opens, and Friday's Occurrence is a Draft.
	h.AdvanceTo(thursdayAt20.Add(-30 * time.Minute))
	thursday := mustFindOccurrence(t, h, thursdayAt20)
	thursday.mustRegister("anna")
	friday := mustFindOccurrence(t, h, fridayAt20)
	for tt, want := range map[*scenario]string{tuesday: "completed", wednesday: "running", thursday: "registration-open", friday: "draft"} {
		if got := tt.get().Status; got != want {
			t.Fatalf("GET Tournament %s = %s before deletion, want %s", tt.ID, got, want)
		}
	}

	mustDeleteRecurring(t, h, "games", "weeknight-cup")
	mustRunManifests(t, h)

	if err := awaitTournament(h, thursday.ID, "cancelled"); err != nil {
		t.Error(err)
	}
	if err := awaitTournament(h, friday.ID, ""); err != nil {
		t.Error(err)
	}
	if got := tuesday.get().Status; got != "completed" {
		t.Errorf("GET Tuesday's Occurrence = %s after deletion, want completed", got)
	}
	if got := wednesday.get().Status; got != "running" {
		t.Errorf("GET Wednesday's Occurrence = %s after deletion, want running", got)
	}
}

func TestTheMoreThanHalfGuardProtectsOccurrences(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	before := listTournaments(h)
	// Removing both Occurrences would remove 2 of the 3 declared Tournaments.
	mustDeleteRecurring(t, h, "games", "weeknight-cup")
	logs := mustCaptureManifestLogs(t, h)
	mustRunManifests(t, h)

	if err := awaitManifestLog(logs, "ERROR"); err != nil {
		t.Fatal(err)
	}
	if got := logs(); !strings.Contains(got, `"removals":2`) || !strings.Contains(got, `"declared":3`) {
		t.Errorf("manifest removal log = %s, want 2 removals of 3 declared", got)
	}
	if got := listTournaments(h); !slices.Equal(got, before) {
		t.Errorf("GET Tournaments after guarded sweep = %+v, want unchanged %+v", got, before)
	}
}

func TestOccurrencesInUnwatchedNamespacesAreNeverRemoved(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games", "elsewhere"))
	mustApplyRecurring(t, h, "elsewhere", "weeknight-cup", recurringSpec())
	mustApply(t, h, "games", "probe", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	unwatched := []string{mustFindOccurrence(t, h, tuesdayAt20).ID, mustFindOccurrence(t, h, wednesdayAt20).ID}
	probeID := mustReadStatus(t, h, "games", "probe").TournamentID
	mustDeleteRecurring(t, h, "elsewhere", "weeknight-cup")
	mustDeleteManifest(t, h, "games", "probe")

	h.App.Manifests = manifests.New(h.FakeKubernetes, h.App.Service, []string{"games"}, h.App.Manifests.Logger)
	mustRunManifests(t, h)

	// The removed Draft proves that a sweep completed.
	if err := awaitTournament(h, probeID, ""); err != nil {
		t.Fatal(err)
	}
	for _, id := range unwatched {
		if got := mustGetStatus(t, h, id); got != "draft" {
			t.Errorf("GET unwatched Occurrence %s = %q, want draft", id, got)
		}
	}
}

func TestOccurrencesAreRemovedOnlyOnceEveryRecurringTournamentWatchSynced(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games", "slow"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	for _, name := range []string{"probe", "retained-1", "retained-2"} {
		mustApply(t, h, "games", name, manifestSpec(h))
	}
	h.Settle()
	occurrences := []string{mustFindOccurrence(t, h, tuesdayAt20).ID, mustFindOccurrence(t, h, wednesdayAt20).ID}
	probeID := mustReadStatus(t, h, "games", "probe").TournamentID
	mustDeleteRecurring(t, h, "games", "weeknight-cup")
	mustDeleteManifest(t, h, "games", "probe")
	// The fake client serializes requests, so the slow namespace fails its
	// lists rather than blocking every other request.
	var slow atomic.Bool
	slow.Store(true)
	h.FakeKubernetes.PrependReactor("list", "recurringtournaments", func(action ktesting.Action) (bool, runtime.Object, error) {
		if action.GetNamespace() == "slow" && slow.Load() {
			return true, nil, fmt.Errorf("namespace temporarily unavailable")
		}
		return false, nil, nil
	})
	mustRunManifests(t, h)

	// Tournament Manifests are swept without waiting for the Recurring
	// Tournaments' watches.
	if err := awaitTournament(h, probeID, ""); err != nil {
		t.Fatal(err)
	}
	// Hold the slow namespace back beyond the sweep interval.
	time.Sleep(1200 * time.Millisecond)
	for _, id := range occurrences {
		if got := mustGetStatus(t, h, id); got != "draft" {
			t.Errorf("GET Occurrence %s before every watch synced = %q, want draft", id, got)
		}
	}
	slow.Store(false)
	// The watch retries its list with a backoff of up to several seconds.
	deadline := time.Now().Add(30 * time.Second)
	for _, id := range occurrences {
		for mustGetStatus(t, h, id) != "" {
			if time.Now().After(deadline) {
				t.Fatalf("Occurrence %s is still declared 30s after every watch could sync", id)
			}
			time.Sleep(50 * time.Millisecond)
		}
	}
}

func TestManifestRemovalWorksWithoutTheRecurringTournamentCRD(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "removed").TournamentID
	mustDeleteManifest(t, h, "games", "removed")
	// A cluster upgraded without applying the new CRD serves no such resource.
	h.FakeKubernetes.PrependReactor("*", "recurringtournaments", func(ktesting.Action) (bool, runtime.Object, error) {
		return true, nil, apierrors.NewNotFound(schema.GroupResource{Group: "opentournament.io", Resource: "recurringtournaments"}, "")
	})
	mustRunManifests(t, h)

	if err := awaitTournament(h, id, ""); err != nil {
		t.Error(err)
	}
}
