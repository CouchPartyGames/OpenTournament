package app_test

import (
	"context"
	"encoding/json"
	"net/http"
	"os"
	"strings"
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
	u := &unstructured.Unstructured{Object: map[string]any{"spec": mustJSONObject(t, spec)}}
	u.SetAPIVersion("opentournament.io/v1alpha1")
	u.SetKind("Tournament")
	u.SetNamespace(namespace)
	u.SetName(name)
	u.SetGeneration(3)
	if _, err := h.FakeKubernetes.Resource(manifests.Resource).Namespace(namespace).Create(context.Background(), u, metav1.CreateOptions{}); err != nil {
		t.Fatalf("create manifest %s/%s: %v", namespace, name, err)
	}
}

// mustEditManifest changes a Manifest's spec, as syncing a new commit from
// git would, which gives the Manifest its next generation.
func mustEditManifest(t *testing.T, h *apptest.Harness, namespace, name string, edit func(spec map[string]any)) {
	t.Helper()
	client := h.FakeKubernetes.Resource(manifests.Resource).Namespace(namespace)
	u, err := client.Get(context.Background(), name, metav1.GetOptions{})
	if err != nil {
		t.Fatalf("get manifest %s/%s: %v", namespace, name, err)
	}
	spec, _ := u.Object["spec"].(map[string]any)
	edit(spec)
	u.Object["spec"] = mustJSONObject(t, spec)
	u.SetGeneration(u.GetGeneration() + 1)
	if _, err := client.Update(context.Background(), u, metav1.UpdateOptions{}); err != nil {
		t.Fatalf("update manifest %s/%s: %v", namespace, name, err)
	}
}

// mustJSONObject round-trips v through JSON, so it holds only JSON values,
// as a spec read from the API server would.
func mustJSONObject(t *testing.T, v any) map[string]any {
	t.Helper()
	b, err := json.Marshal(v)
	if err != nil {
		t.Fatalf("encode %v: %v", v, err)
	}
	var object map[string]any
	if err := json.Unmarshal(b, &object); err != nil {
		t.Fatalf("decode %s: %v", b, err)
	}
	return object
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
		Type    string `json:"type"`
		Status  string `json:"status"`
		Reason  string `json:"reason"`
		Message string `json:"message"`
	} `json:"conditions"`
}

// synced is the Synced condition of a Manifest's status, as status/reason.
func (s manifestStatus) synced() string {
	for _, c := range s.Conditions {
		if c.Type == "Synced" {
			return c.Status + "/" + c.Reason
		}
	}
	return ""
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
		t.Fatalf("encode status of manifest %s/%s: %v", namespace, name, err)
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
		t.Errorf("GET /api/v1/tournaments lists %+v, want a draft organized by client:arena-backend", ts[0])
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
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want generation 3, Tournament %s, draft", s, ts[0].ID)
	}
	if len(s.Conditions) != 1 || s.Conditions[0].Type != "Synced" || s.Conditions[0].Status != "True" || s.Conditions[0].Reason != "Created" {
		t.Errorf("mustReadStatus(games/friday-cup).Conditions = %+v, want only Synced=True because Created", s.Conditions)
	}
}

func TestTheExampleManifestDeclaresATournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	b, err := os.ReadFile("../../examples/tournament.yaml")
	if err != nil {
		t.Fatalf("read the example: %v", err)
	}
	var example struct {
		Metadata struct{ Name, Namespace string }
		Spec     map[string]any
	}
	var dates struct {
		Spec struct{ StartsAt, RegistrationOpensAt time.Time }
	}
	if err := yaml.Unmarshal(b, &example); err != nil {
		t.Fatalf("parse the example: %v", err)
	}
	if err := yaml.Unmarshal(b, &dates); err != nil {
		t.Fatalf("parse the example's dates: %v", err)
	}
	// The example's dates are in the fake clock's past; keep their spacing.
	starts := h.FakeClock.Now().Add(24 * time.Hour)
	example.Spec["startsAt"] = starts
	example.Spec["registrationOpensAt"] = starts.Add(-dates.Spec.StartsAt.Sub(dates.Spec.RegistrationOpensAt))

	mustApply(t, h, example.Metadata.Namespace, example.Metadata.Name, example.Spec)
	h.Settle()

	if s := mustReadStatus(t, h, example.Metadata.Namespace, example.Metadata.Name); s.TournamentStatus != "draft" {
		t.Errorf("mustReadStatus(example) = %+v, want a draft Tournament", s)
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
		t.Errorf("GET /api/v1/tournaments/%s = %+v, want organized by user:organizer with one Stage of one Group", id, tv)
	}
}

type declaredSettings struct {
	Name     string
	Capacity int
	Status   string
}

// mustGetDeclared reads the Tournament a Manifest's status names.
func mustGetDeclared(t *testing.T, h *apptest.Harness, namespace, name string) declaredSettings {
	t.Helper()
	id := mustReadStatus(t, h, namespace, name).TournamentID
	if id == "" {
		t.Fatalf("manifest %s/%s names no Tournament", namespace, name)
	}
	var tv declaredSettings
	h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil).Expect(http.StatusOK).Decode(&tv)
	return tv
}

func TestEditingTheManifestOfADraftUpdatesTheTournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) {
		spec["name"] = "Friday Night Cup"
		spec["capacity"] = 4
	})
	h.Settle()

	if tv := mustGetDeclared(t, h, "games", "friday-cup"); tv.Name != "Friday Night Cup" || tv.Capacity != 4 {
		t.Errorf("the declared Tournament is %+v, want Friday Night Cup with capacity 4", tv)
	}
	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.ObservedGeneration != 4 || s.synced() != "True/Updated" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want generation 4, Synced=True because Updated", s)
	}

	// Later reconciles find nothing to change, and keep saying why it synced.
	h.Settle()
	if s := mustReadStatus(t, h, "games", "friday-cup"); s.ObservedGeneration != 4 || s.synced() != "True/Updated" {
		t.Errorf("after another reconcile, mustReadStatus(games/friday-cup) = %+v, want generation 4, Synced=True because Updated", s)
	}
}

func TestEditingTheManifestAfterRegistrationOpensChangesNothing(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Advance(opensIn)

	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) { spec["capacity"] = 4 })
	h.Settle()

	if tv := mustGetDeclared(t, h, "games", "friday-cup"); tv.Capacity != 16 {
		t.Errorf("the declared Tournament has capacity %d, want 16, its frozen setting", tv.Capacity)
	}
	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.ObservedGeneration != 3 || s.TournamentStatus != "registration-open" || s.synced() != "False/SettingsFrozen" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want generation 3, registration-open, Synced=False because SettingsFrozen", s)
	}
}

func TestRevertingAFrozenManifestSyncsItAgain(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Advance(opensIn)
	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) { spec["capacity"] = 4 })
	h.Settle()

	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) { spec["capacity"] = 16 })
	h.Settle()

	if s := mustReadStatus(t, h, "games", "friday-cup"); s.ObservedGeneration != 5 || s.synced() != "True/Unchanged" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want generation 5, Synced=True because Unchanged", s)
	}
}

// invalidSettings breaks the issue's two examples: its first Stage's single
// Group must advance more Participants than it may hold, and as a
// free-for-all it may hold more than the arena Game's Maximum Match Size.
func invalidSettings(h *apptest.Harness) map[string]any {
	return settings(h,
		stage("free-for-all", map[string]any{"groups": 1, "bouts": 1, "advancement": 3}),
		stage("single-elimination", map[string]any{"groups": 1}))
}

// mustRefuse posts settings to the API, and returns the field messages of
// its refusal as a Manifest would locate them.
func mustRefuse(t *testing.T, h *apptest.Harness, settings map[string]any) []string {
	t.Helper()
	r := h.Do(http.MethodPost, "/api/v1/tournaments", h.Client("arena-backend"), settings).Expect(http.StatusUnprocessableEntity)
	var p problemDetails
	r.Decode(&p)
	var out []string
	for _, e := range p.Errors {
		out = append(out, "spec."+strings.TrimPrefix(e.Location, "body.")+": "+e.Message)
	}
	return out
}

func TestAnInvalidManifestReportsEveryFieldMessageAndCreatesNothing(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := invalidSettings(h)
	want := mustRefuse(t, h, spec)
	spec["organizer"] = "client:arena-backend"

	mustApply(t, h, "games", "friday-cup", spec)
	h.Settle()

	if ts := listTournaments(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %d Tournaments, want none", len(ts))
	}
	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.TournamentID != "" || s.synced() != "False/ValidationFailed" {
		t.Fatalf("mustReadStatus(games/friday-cup) = %+v, want no Tournament, Synced=False because ValidationFailed", s)
	}
	if len(want) != 2 {
		t.Errorf("the API refuses the settings with %q, want both of the issue's examples", want)
	}
	for _, m := range want {
		if !strings.Contains(s.Conditions[0].Message, m) {
			t.Errorf("Synced condition message = %q, want it to contain %q", s.Conditions[0].Message, m)
		}
	}
}

func TestAnInvalidEditChangesNothing(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) {
		spec["capacity"] = 4
		spec["stages"] = invalidSettings(h)["stages"]
	})
	h.Settle()

	if tv := mustGetDeclared(t, h, "games", "friday-cup"); tv.Capacity != 16 {
		t.Errorf("the declared Tournament has capacity %d, want 16, as declared", tv.Capacity)
	}
	if s := mustReadStatus(t, h, "games", "friday-cup"); s.ObservedGeneration != 3 || s.synced() != "False/ValidationFailed" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want generation 3, Synced=False because ValidationFailed", s)
	}
}

func TestAManifestWithAMalformedOrganizerFailsValidation(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := manifestSpec(h)
	spec["organizer"] = "arena-backend"

	mustApply(t, h, "games", "friday-cup", spec)
	h.Settle()

	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.synced() != "False/ValidationFailed" || !strings.Contains(s.Conditions[0].Message, "spec.organizer") {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want Synced=False because ValidationFailed at spec.organizer", s)
	}
}

func TestAManifestCantChangeItsGameOrOrganizer(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()

	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) {
		spec["gameId"] = "royale"
		spec["organizer"] = "client:royale-backend"
	})
	h.Settle()

	ts := listTournaments(h)
	if len(ts) != 1 || ts[0].Organizer != "client:arena-backend" {
		t.Errorf("GET /api/v1/tournaments lists %+v, want only the Tournament organized by client:arena-backend", ts)
	}
	s := mustReadStatus(t, h, "games", "friday-cup")
	if s.synced() != "False/ValidationFailed" {
		t.Fatalf("mustReadStatus(games/friday-cup) = %+v, want Synced=False because ValidationFailed", s)
	}
	for _, field := range []string{"spec.gameId", "spec.organizer"} {
		if !strings.Contains(s.Conditions[0].Message, field) {
			t.Errorf("Synced condition message = %q, want it to name %s", s.Conditions[0].Message, field)
		}
	}
}

func TestAnUntrustedClientOrganizerIsReported(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := manifestSpec(h)
	spec["organizer"] = "client:royale-backend" // Trusted by royale, not arena.

	mustApply(t, h, "games", "friday-cup", spec)
	h.Settle()

	if ts := listTournaments(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %d Tournaments, want none", len(ts))
	}
	if s := mustReadStatus(t, h, "games", "friday-cup"); s.synced() != "False/OrganizerNotTrusted" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want Synced=False because OrganizerNotTrusted", s)
	}
}

func TestTheAPIRefusesToEditADeclaredTournament(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "friday-cup").TournamentID
	edit := settings(h)
	delete(edit, "gameId")
	edit["capacity"] = 4

	r := h.Do(http.MethodPut, "/api/v1/tournaments/"+id, h.Client("arena-backend"), edit).Expect(http.StatusConflict)

	if r.Code() != "declared-in-git" || r.Header.Get("Content-Type") != "application/problem+json" {
		t.Errorf("PUT /api/v1/tournaments/%s = %s %s, want declared-in-git Problem Details", id, r.Header.Get("Content-Type"), r.Body)
	}
	if tv := mustGetDeclared(t, h, "games", "friday-cup"); tv.Capacity != 16 {
		t.Errorf("the declared Tournament has capacity %d, want 16, as declared", tv.Capacity)
	}

	// A Tournament created through the API is still edited through it.
	tt := mustCreate(t, h, settings(h))
	h.Do(http.MethodPut, tt.path(), tt.Organizer, edit).Expect(http.StatusOK)
}

func TestADeclaredTournamentCancelledThroughTheAPIStaysCancelled(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "friday-cup", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "friday-cup").TournamentID

	h.Do(http.MethodPost, "/api/v1/tournaments/"+id+"/cancel", h.Client("arena-backend"), nil).Expect(http.StatusOK)
	h.Settle()
	mustEditManifest(t, h, "games", "friday-cup", func(spec map[string]any) { spec["capacity"] = 4 })
	h.Settle()
	h.Settle()

	ts := listTournaments(h)
	if len(ts) != 1 || ts[0].ID != id || ts[0].Status != "cancelled" {
		t.Errorf("GET /api/v1/tournaments lists %+v, want only Tournament %s, cancelled", ts, id)
	}
	if s := mustReadStatus(t, h, "games", "friday-cup"); s.TournamentID != id || s.TournamentStatus != "cancelled" {
		t.Errorf("mustReadStatus(games/friday-cup) = %+v, want Tournament %s, cancelled", s, id)
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
		t.Errorf("mustReadStatus(elsewhere/friday-cup) = %+v, want none", s)
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
		t.Errorf("mustReadStatus(elsewhere/friday-cup) = %+v, want none", s)
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
