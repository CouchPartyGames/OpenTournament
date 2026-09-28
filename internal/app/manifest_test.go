package app_test

import (
	"context"
	"encoding/json"
	"net/http"
	"os"
	"sync"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/apis/meta/v1/unstructured"
	"k8s.io/client-go/tools/cache"
	"sigs.k8s.io/yaml"
)

// mustApply puts a Tournament Manifest into the fake cluster, as Argo CD or
// Flux would after syncing it from git.
func mustApply(t *testing.T, h *apptest.Harness, namespace, name string, spec map[string]any) {
	t.Helper()
	// Round-trip through JSON so the spec holds only JSON values, as it would
	// when read from the API server.
	b, err := json.Marshal(spec)
	if err != nil {
		t.Fatal(err)
	}
	var object map[string]any
	if err := json.Unmarshal(b, &object); err != nil {
		t.Fatal(err)
	}
	u := &unstructured.Unstructured{Object: map[string]any{"spec": object}}
	u.SetAPIVersion("opentournament.io/v1alpha1")
	u.SetKind("Tournament")
	u.SetNamespace(namespace)
	u.SetName(name)
	u.SetGeneration(3)
	if _, err := h.FakeKubernetes.Resource(manifests.Resource).Namespace(namespace).Create(context.Background(), u, metav1.CreateOptions{}); err != nil {
		t.Fatalf("create manifest %s/%s: %v", namespace, name, err)
	}
}

// manifestSpec is a valid Tournament Manifest spec for the arena Game.
func manifestSpec(h *apptest.Harness) map[string]any {
	spec := settings(h)
	spec["organizer"] = "client:arena-backend"
	return spec
}

func listTournaments(h *apptest.Harness) []tournamentView {
	var out struct{ Tournaments []tournamentView }
	h.Do(http.MethodGet, "/api/v1/tournaments", "", nil).Expect(http.StatusOK).Decode(&out)
	return out.Tournaments
}

type manifestStatus struct {
	ObservedGeneration int64  `json:"observedGeneration"`
	TournamentID       string `json:"tournamentId"`
	TournamentStatus   string `json:"tournamentStatus"`
	Conditions         []struct {
		Type   string `json:"type"`
		Status string `json:"status"`
		Reason string `json:"reason"`
	} `json:"conditions"`
}

// mustReadStatus reads a Manifest's status, as kubectl would show it.
func mustReadStatus(t *testing.T, h *apptest.Harness, namespace, name string) manifestStatus {
	t.Helper()
	u, err := h.FakeKubernetes.Resource(manifests.Resource).Namespace(namespace).Get(context.Background(), name, metav1.GetOptions{})
	if err != nil {
		t.Fatalf("get manifest %s/%s: %v", namespace, name, err)
	}
	b, err := json.Marshal(u.Object["status"])
	if err != nil {
		t.Fatal(err)
	}
	var s manifestStatus
	if err := json.Unmarshal(b, &s); err != nil {
		t.Fatalf("decode status %s: %v", b, err)
	}
	return s
}

func TestApplyingAManifestDeclaresADraftTournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))

	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	ts := listTournaments(h)
	if len(ts) != 1 {
		t.Fatalf("GET /api/v1/tournaments lists %d Tournaments, want 1", len(ts))
	}
	if ts[0].Status != "draft" || ts[0].Organizer != "client:arena-backend" {
		t.Errorf("got %+v, want a draft organized by client:arena-backend", ts[0])
	}
}

func TestManifestStatusReportsTheDeclaredTournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))

	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	ts := listTournaments(h)
	if len(ts) != 1 {
		t.Fatalf("GET /api/v1/tournaments lists %d Tournaments, want 1", len(ts))
	}
	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.ObservedGeneration != 3 || s.TournamentID != ts[0].ID || s.TournamentStatus != "draft" {
		t.Errorf("status = %+v, want generation 3, Tournament %s, draft", s, ts[0].ID)
	}
	if len(s.Conditions) != 1 || s.Conditions[0].Type != "Synced" || s.Conditions[0].Status != "True" || s.Conditions[0].Reason != "Created" {
		t.Errorf("conditions = %+v, want only Synced=True because Created", s.Conditions)
	}
}

func TestTheExampleManifestDeclaresATournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	b, err := os.ReadFile("../../examples/tournament.yaml")
	if err != nil {
		t.Fatal(err)
	}
	var example struct {
		Metadata struct{ Name, Namespace string }
		Spec     map[string]any
	}
	if err := yaml.Unmarshal(b, &example); err != nil {
		t.Fatalf("parse the example: %v", err)
	}
	// The example's dates are in the fake clock's past; keep their spacing.
	starts, err := time.Parse(time.RFC3339, example.Spec["startsAt"].(string))
	if err != nil {
		t.Fatal(err)
	}
	opens, err := time.Parse(time.RFC3339, example.Spec["registrationOpensAt"].(string))
	if err != nil {
		t.Fatal(err)
	}
	example.Spec["startsAt"] = h.FakeClock.Now().Add(24 * time.Hour)
	example.Spec["registrationOpensAt"] = h.FakeClock.Now().Add(24*time.Hour - starts.Sub(opens))

	mustApply(t, h, example.Metadata.Namespace, example.Metadata.Name, example.Spec)
	h.Settle()

	if s := mustReadStatus(t, h, example.Metadata.Namespace, example.Metadata.Name); s.TournamentStatus != "draft" {
		t.Errorf("status = %+v, want a draft Tournament", s)
	}
}

func TestManifestGetsTheAPIsDefaults(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := manifestSpec(h) // Its Stage leaves out groups.
	spec["organizer"] = "user:organizer"

	mustApply(t, h, "games", "friday-cup", spec)
	h.Settle()

	id := mustReadStatus(t, h, "games", "friday-cup").TournamentID
	var tv struct {
		Organizer string
		Stages    []struct{ Groups int }
	}
	h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil).Expect(http.StatusOK).Decode(&tv)
	if tv.Organizer != "user:organizer" || len(tv.Stages) != 1 || tv.Stages[0].Groups != 1 {
		t.Errorf("got %+v, want organized by user:organizer with one Stage of one Group", tv)
	}
}

func TestManifestsOutsideWatchedNamespacesAreIgnored(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))

	mustApply(t, h, "elsewhere", "friday-cup", manifestSpec(h))
	h.Settle()

	if ts := listTournaments(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %d Tournaments, want none", len(ts))
	}
	if s := mustReadStatus(t, h, "elsewhere", "friday-cup"); len(s.Conditions) != 0 || s.TournamentID != "" {
		t.Errorf("status = %+v, want none", s)
	}
}

func TestWithoutWatchedNamespacesNoTournamentIsDeclared(t *testing.T) {
	h := apptest.MustStart(t)

	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	if ts := listTournaments(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %d Tournaments, want none", len(ts))
	}
}

func TestTheServiceWatchesManifestsAsTheyAreApplied(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	ctx, cancel := context.WithCancel(context.Background())
	stopped := make(chan struct{})
	go func() {
		defer close(stopped)
		h.App.Manifests.Run(ctx)
	}()
	t.Cleanup(func() {
		cancel()
		<-stopped
	})

	mustApply(t, h, "elsewhere", "friday-cup", manifestSpec(h))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))

	deadline := time.Now().Add(10 * time.Second)
	for mustReadStatus(t, h, "games", "friday-cup").TournamentID == "" {
		if time.Now().After(deadline) {
			t.Fatal("the watched Manifest's status names no Tournament after 10s")
		}
		time.Sleep(20 * time.Millisecond)
	}
	if ts := listTournaments(h); len(ts) != 1 {
		t.Errorf("GET /api/v1/tournaments lists %d Tournaments, want 1", len(ts))
	}
	if s := mustReadStatus(t, h, "elsewhere", "friday-cup"); s.TournamentID != "" {
		t.Errorf("the unwatched Manifest's status = %+v, want none", s)
	}
}

func TestReplicasReconcilingAManifestAtOnceDeclareOneTournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))

	// Every replica reconciles every Manifest, without leader election.
	name := cache.ObjectName{Namespace: "games", Name: "friday-cup"}
	var wg sync.WaitGroup
	for range 8 {
		wg.Go(func() {
			if err := h.App.Manifests.Reconcile(context.Background(), name); err != nil {
				t.Errorf("Reconcile(%v) = %v, want nil", name, err)
			}
		})
	}
	wg.Wait()
	h.Settle()
	h.Settle()

	ts := listTournaments(h)
	if len(ts) != 1 {
		t.Fatalf("GET /api/v1/tournaments lists %d Tournaments, want 1", len(ts))
	}
	if s := mustReadStatus(t, h, "games", "friday-cup"); s.TournamentID != ts[0].ID {
		t.Errorf("status names Tournament %s, want %s", s.TournamentID, ts[0].ID)
	}
}
