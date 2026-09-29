package app_test

import (
	"context"
	"encoding/json"
	"net/http"
	"os"
	"slices"
	"strings"
	"sync"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/apis/meta/v1/unstructured"
	"k8s.io/apimachinery/pkg/runtime"
	"k8s.io/apimachinery/pkg/runtime/schema"
	ktesting "k8s.io/client-go/testing"
	"k8s.io/client-go/tools/cache"
	"sigs.k8s.io/yaml"
)

// The fake clock starts on Tuesday 2030-01-01 at 12:00 UTC, 13:00 in Berlin.
// Weeknights at 20:00 Berlin time start at 19:00 UTC in winter.
var (
	tuesdayAt20   = time.Date(2030, time.January, 1, 19, 0, 0, 0, time.UTC)
	wednesdayAt20 = tuesdayAt20.Add(24 * time.Hour)
	thursdayAt20  = wednesdayAt20.Add(24 * time.Hour)
)

// recurringSpec is a valid RecurringTournament spec for the arena Game: every
// weeknight at 20:00 Berlin time, looking 48 hours ahead.
func recurringSpec() map[string]any {
	return map[string]any{
		"schedule":  "0 20 * * 1-5",
		"timeZone":  "Europe/Berlin",
		"lookahead": "48h",
		"template": map[string]any{
			"organizer":               "client:arena-backend",
			"gameId":                  "arena",
			"name":                    "Weeknight Cup",
			"registrationOpensBefore": "30m",
			"capacity":                16,
			"minimumParticipants":     2,
			"stages":                  []map[string]any{stage("single-elimination", nil)},
		},
	}
}

// mustApplyRecurring puts a RecurringTournament into the fake cluster, as
// Argo CD or Flux would after syncing it from git.
func mustApplyRecurring(t *testing.T, h *apptest.Harness, namespace, name string, spec map[string]any) {
	t.Helper()
	u := &unstructured.Unstructured{Object: map[string]any{"spec": mustJSONObject(t, spec)}}
	u.SetAPIVersion("opentournament.io/v1alpha1")
	u.SetKind("RecurringTournament")
	u.SetNamespace(namespace)
	u.SetName(name)
	u.SetGeneration(1)
	if _, err := h.FakeKubernetes.Resource(manifests.RecurringResource).Namespace(namespace).Create(context.Background(), u, metav1.CreateOptions{}); err != nil {
		t.Fatalf("create recurring tournament %s/%s: %v", namespace, name, err)
	}
}

// occurrenceView is a Tournament as the API shows it, with its schedule.
type occurrenceView struct {
	ID                  string    `json:"id"`
	Name                string    `json:"name"`
	Status              string    `json:"status"`
	Organizer           string    `json:"organizer"`
	StartsAt            time.Time `json:"startsAt"`
	RegistrationOpensAt time.Time `json:"registrationOpensAt"`
}

// listOccurrences lists every Tournament, earliest start first.
func listOccurrences(h *apptest.Harness) []occurrenceView {
	var out struct{ Tournaments []occurrenceView }
	h.Do(http.MethodGet, "/api/v1/tournaments", "", nil).Expect(http.StatusOK).Decode(&out)
	slices.SortFunc(out.Tournaments, func(a, b occurrenceView) int { return a.StartsAt.Compare(b.StartsAt) })
	return out.Tournaments
}

// startTimes are the starts of Tournaments, in UTC.
func startTimes(ts []occurrenceView) []time.Time {
	var out []time.Time
	for _, t := range ts {
		out = append(out, t.StartsAt.UTC())
	}
	return out
}

func TestApplyingARecurringTournamentDeclaresADraftPerOccurrenceInTheWindow(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))

	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()

	ts := listOccurrences(h)
	want := []occurrenceView{
		{Name: "Weeknight Cup 2030-01-01 20:00", Status: "draft", Organizer: "client:arena-backend",
			StartsAt: tuesdayAt20, RegistrationOpensAt: tuesdayAt20.Add(-30 * time.Minute)},
		{Name: "Weeknight Cup 2030-01-02 20:00", Status: "draft", Organizer: "client:arena-backend",
			StartsAt: wednesdayAt20, RegistrationOpensAt: wednesdayAt20.Add(-30 * time.Minute)},
	}
	if len(ts) != len(want) {
		t.Fatalf("GET /api/v1/tournaments lists %+v, want %d Tournaments", ts, len(want))
	}
	for i, w := range want {
		got := ts[i]
		if got.Name != w.Name || got.Status != w.Status || got.Organizer != w.Organizer ||
			!got.StartsAt.Equal(w.StartsAt) || !got.RegistrationOpensAt.Equal(w.RegistrationOpensAt) {
			t.Errorf("Tournament %d = %+v, want %+v", i, got, w)
		}
	}
}

// mustEditRecurring changes a RecurringTournament's spec, as syncing a new
// commit from git would, which gives it its next generation.
func mustEditRecurring(t *testing.T, h *apptest.Harness, namespace, name string, edit func(spec map[string]any)) {
	t.Helper()
	client := h.FakeKubernetes.Resource(manifests.RecurringResource).Namespace(namespace)
	u, err := client.Get(context.Background(), name, metav1.GetOptions{})
	if err != nil {
		t.Fatalf("get recurring tournament %s/%s: %v", namespace, name, err)
	}
	spec, _ := u.Object["spec"].(map[string]any)
	edit(spec)
	u.Object["spec"] = mustJSONObject(t, spec)
	u.SetGeneration(u.GetGeneration() + 1)
	if _, err := client.Update(context.Background(), u, metav1.UpdateOptions{}); err != nil {
		t.Fatalf("update recurring tournament %s/%s: %v", namespace, name, err)
	}
}

type recurringStatus struct {
	ObservedGeneration int64      `json:"observedGeneration"`
	LastScheduleTime   *time.Time `json:"lastScheduleTime"`
	Upcoming           []struct {
		StartsAt         time.Time `json:"startsAt"`
		TournamentID     string    `json:"tournamentId"`
		TournamentStatus string    `json:"tournamentStatus"`
	} `json:"upcoming"`
	manifestStatus
}

// upcomingStarts are the starts of the upcoming Occurrences, in UTC.
func (s recurringStatus) upcomingStarts() []time.Time {
	var out []time.Time
	for _, o := range s.Upcoming {
		out = append(out, o.StartsAt.UTC())
	}
	return out
}

// mustReadRecurringStatus reads a RecurringTournament's status, as kubectl
// would show it.
func mustReadRecurringStatus(t *testing.T, h *apptest.Harness, namespace, name string) recurringStatus {
	t.Helper()
	u, err := h.FakeKubernetes.Resource(manifests.RecurringResource).Namespace(namespace).Get(context.Background(), name, metav1.GetOptions{})
	if err != nil {
		t.Fatalf("get recurring tournament %s/%s: %v", namespace, name, err)
	}
	b, err := json.Marshal(u.Object["status"])
	if err != nil {
		t.Fatalf("encode status of recurring tournament %s/%s: %v", namespace, name, err)
	}
	var s recurringStatus
	if err := json.Unmarshal(b, &s); err != nil {
		t.Fatalf("decode status %s: %v", b, err)
	}
	return s
}

// sameInstants reports whether a and b hold the same instants, in order.
func sameInstants(a, b []time.Time) bool { return slices.EqualFunc(a, b, time.Time.Equal) }

func TestTheNextOccurrenceIsDeclaredWhenItEntersTheWindow(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()

	// Thursday's Occurrence enters the 48-hour window as Tuesday's starts.
	h.AdvanceTo(tuesdayAt20.Add(-time.Second))
	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20}) {
		t.Errorf("a second before Thursday's Occurrence enters the window, Tournaments start at %v, want Tuesday and Wednesday", got)
	}
	h.AdvanceTo(tuesdayAt20)
	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20, thursdayAt20}) {
		t.Errorf("once Thursday's Occurrence enters the window, Tournaments start at %v, want Tuesday to Thursday", got)
	}
}

func TestReconcilingARecurringTournamentRepeatedlyOrAtOnceNeverDuplicatesAnOccurrence(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())

	// Every replica reconciles every RecurringTournament, without leader election.
	name := cache.ObjectName{Namespace: "games", Name: "weeknight-cup"}
	var wg sync.WaitGroup
	for range 8 {
		wg.Go(func() {
			if _, err := h.App.Manifests.ReconcileRecurring(context.Background(), name); err != nil {
				t.Errorf("ReconcileRecurring(%v) = %v, want nil", name, err)
			}
		})
	}
	wg.Wait()
	h.Settle()
	h.Settle()

	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20}) {
		t.Errorf("Tournaments start at %v, want one on Tuesday and one on Wednesday", got)
	}
}

func TestRecurringTournamentStatusListsTheUpcomingOccurrences(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()
	// Tuesday's registration opens, and the status follows at the next resync.
	h.AdvanceTo(tuesdayAt20.Add(-30 * time.Minute))
	h.Settle()

	ts := listOccurrences(h)
	if len(ts) != 2 {
		t.Fatalf("GET /api/v1/tournaments lists %+v, want 2 Tournaments", ts)
	}
	s := mustReadRecurringStatus(t, h, "games", "weeknight-cup")
	if s.ObservedGeneration != 1 || s.synced() != "True/Scheduled" {
		t.Errorf("status = %+v, want generation 1, Synced=True because Scheduled", s)
	}
	if s.LastScheduleTime == nil || !s.LastScheduleTime.Equal(wednesdayAt20) {
		t.Errorf("status.lastScheduleTime = %v, want %v", s.LastScheduleTime, wednesdayAt20)
	}
	if len(s.Upcoming) != 2 {
		t.Fatalf("status.upcoming = %+v, want 2 Occurrences", s.Upcoming)
	}
	for i, want := range []struct {
		startsAt time.Time
		status   string
	}{{tuesdayAt20, "registration-open"}, {wednesdayAt20, "draft"}} {
		got := s.Upcoming[i]
		if !got.StartsAt.Equal(want.startsAt) || got.TournamentID != ts[i].ID || got.TournamentStatus != want.status {
			t.Errorf("status.upcoming[%d] = %+v, want %v, Tournament %s, %s", i, got, want.startsAt, ts[i].ID, want.status)
		}
	}

	// Tuesday's Occurrence starts, and so is no longer upcoming.
	h.AdvanceTo(tuesdayAt20)
	s = mustReadRecurringStatus(t, h, "games", "weeknight-cup")
	if got := s.upcomingStarts(); !sameInstants(got, []time.Time{wednesdayAt20, thursdayAt20}) {
		t.Errorf("status.upcoming starts at %v once Tuesday's has started, want Wednesday and Thursday", got)
	}
	if s.LastScheduleTime == nil || !s.LastScheduleTime.Equal(thursdayAt20) {
		t.Errorf("status.lastScheduleTime = %v, want %v", s.LastScheduleTime, thursdayAt20)
	}
}

func TestASuspendedRecurringTournamentDeclaresNothing(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := recurringSpec()
	spec["suspend"] = true

	mustApplyRecurring(t, h, "games", "weeknight-cup", spec)
	h.Settle()

	if ts := listOccurrences(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %+v, want none", ts)
	}
	if s := mustReadRecurringStatus(t, h, "games", "weeknight-cup"); s.synced() != "True/Suspended" || len(s.Upcoming) != 0 {
		t.Errorf("status = %+v, want no upcoming Occurrence, Synced=True because Suspended", s)
	}
}

func TestSuspendingARecurringTournamentStopsNewOccurrencesAndKeepsExistingOnes(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()
	before := listOccurrences(h)

	mustEditRecurring(t, h, "games", "weeknight-cup", func(spec map[string]any) { spec["suspend"] = true })
	h.Settle()
	// Thursday's Occurrence enters the window while Tuesday's registration
	// opens and it starts as usual.
	h.AdvanceTo(tuesdayAt20)

	after := listOccurrences(h)
	if got := startTimes(after); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20}) {
		t.Errorf("Tournaments start at %v while suspended, want only the Tuesday and Wednesday declared before", got)
	}
	for i := range min(len(before), len(after)) {
		if after[i].ID != before[i].ID || after[i].Name != before[i].Name {
			t.Errorf("Tournament %d = %+v while suspended, want %+v", i, after[i], before[i])
		}
	}
	s := mustReadRecurringStatus(t, h, "games", "weeknight-cup")
	if s.ObservedGeneration != 2 || s.synced() != "True/Suspended" {
		t.Errorf("status = %+v, want generation 2, Synced=True because Suspended", s)
	}
	if len(s.Upcoming) != 1 || s.Upcoming[0].TournamentID != before[1].ID {
		t.Errorf("status.upcoming = %+v, want only Wednesday's Tournament %s", s.Upcoming, before[1].ID)
	}

	mustEditRecurring(t, h, "games", "weeknight-cup", func(spec map[string]any) { spec["suspend"] = false })
	h.Settle()
	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20, thursdayAt20}) {
		t.Errorf("Tournaments start at %v once resumed, want Tuesday to Thursday", got)
	}
}

func TestAnInvalidRecurringTournamentIsReportedAndDeclaresNothing(t *testing.T) {
	for _, tc := range []struct {
		name    string
		edit    func(spec, template map[string]any)
		reason  string
		message string
	}{
		{
			name:    "schedule",
			edit:    func(spec, _ map[string]any) { spec["schedule"] = "every weeknight" },
			reason:  "InvalidSchedule",
			message: `invalid schedule "every weeknight"`,
		},
		{
			name:    "time zone",
			edit:    func(spec, _ map[string]any) { spec["timeZone"] = "Mars/Olympus_Mons" },
			reason:  "UnknownTimeZone",
			message: `unknown time zone "Mars/Olympus_Mons"`,
		},
		{
			name:    "lookahead",
			edit:    func(spec, _ map[string]any) { spec["lookahead"] = "2 days" },
			reason:  "InvalidLookahead",
			message: "spec.lookahead",
		},
		{
			name: "template",
			edit: func(_, template map[string]any) {
				template["stages"] = []map[string]any{stage("single-elimination", map[string]any{"advancement": 2})}
			},
			reason:  "ValidationFailed",
			message: "spec.template.stages[0].advancement: the last stage has no advancement",
		},
		{
			name:    "registration opening",
			edit:    func(_, template map[string]any) { template["registrationOpensBefore"] = "0s" },
			reason:  "ValidationFailed",
			message: "spec.template.registrationOpensBefore: must be a positive duration",
		},
		{
			name:    "organizer",
			edit:    func(_, template map[string]any) { template["organizer"] = "arena-backend" },
			reason:  "ValidationFailed",
			message: "spec.template.organizer: must be user:<subject> or client:<client id>",
		},
		{
			name:    "untrusted organizer",
			edit:    func(_, template map[string]any) { template["organizer"] = "client:royale-backend" },
			reason:  "OrganizerNotTrusted",
			message: `client "royale-backend" is not trusted to act for game "arena"`,
		},
	} {
		t.Run(tc.name, func(t *testing.T) {
			h := apptest.MustStart(t, apptest.WatchManifests("games"))
			spec := recurringSpec()
			tc.edit(spec, spec["template"].(map[string]any))

			mustApplyRecurring(t, h, "games", "weeknight-cup", spec)
			h.Settle()

			if ts := listOccurrences(h); len(ts) != 0 {
				t.Errorf("GET /api/v1/tournaments lists %+v, want none", ts)
			}
			s := mustReadRecurringStatus(t, h, "games", "weeknight-cup")
			if s.synced() != "False/"+tc.reason || !strings.Contains(s.syncedMessage(), tc.message) {
				t.Errorf("status = %+v, want Synced=False because %s, with a message containing %q", s, tc.reason, tc.message)
			}
			if s.ObservedGeneration != 0 {
				t.Errorf("status.observedGeneration = %d, want none synced", s.ObservedGeneration)
			}
		})
	}
}

func TestFixingAnInvalidRecurringTournamentDeclaresItsOccurrences(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	spec := recurringSpec()
	spec["timeZone"] = "Europe/Berln"
	mustApplyRecurring(t, h, "games", "weeknight-cup", spec)
	h.Settle()

	mustEditRecurring(t, h, "games", "weeknight-cup", func(spec map[string]any) { spec["timeZone"] = "Europe/Berlin" })
	h.Settle()

	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20}) {
		t.Errorf("Tournaments start at %v, want Tuesday and Wednesday", got)
	}
	if s := mustReadRecurringStatus(t, h, "games", "weeknight-cup"); s.ObservedGeneration != 2 || s.synced() != "True/Scheduled" {
		t.Errorf("status = %+v, want generation 2, Synced=True because Scheduled", s)
	}
}

func TestRecurringTournamentsOutsideWatchedNamespacesAreIgnored(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))

	mustApplyRecurring(t, h, "elsewhere", "weeknight-cup", recurringSpec())
	h.Settle()

	if ts := listOccurrences(h); len(ts) != 0 {
		t.Errorf("GET /api/v1/tournaments lists %+v, want none", ts)
	}
	if s := mustReadRecurringStatus(t, h, "elsewhere", "weeknight-cup"); s.synced() != "" {
		t.Errorf("status of an unwatched RecurringTournament = %+v, want none", s)
	}
}

func TestTheAPIRefusesToEditAnOccurrence(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())
	h.Settle()
	id := listOccurrences(h)[0].ID

	body := settings(h)
	delete(body, "gameId")
	r := h.Do(http.MethodPut, "/api/v1/tournaments/"+id, h.Client("arena-backend"), body).Expect(http.StatusConflict)
	if code := r.Code(); code != "declared-in-git" {
		t.Errorf("PUT an Occurrence's Tournament = %s, want declared-in-git", code)
	}
}

func TestTheExampleRecurringTournamentDeclaresItsOccurrences(t *testing.T) {
	b, err := os.ReadFile("../../examples/recurring-tournament.yaml")
	if err != nil {
		t.Fatalf("read the example: %v", err)
	}
	var example struct {
		Metadata struct{ Name, Namespace string }
		Spec     map[string]any
	}
	if err := yaml.Unmarshal(b, &example); err != nil {
		t.Fatalf("parse the example: %v", err)
	}
	h := apptest.MustStart(t, apptest.WatchManifests(example.Metadata.Namespace))

	mustApplyRecurring(t, h, example.Metadata.Namespace, example.Metadata.Name, example.Spec)
	h.Settle()

	s := mustReadRecurringStatus(t, h, example.Metadata.Namespace, example.Metadata.Name)
	if s.synced() != "True/Scheduled" || len(s.Upcoming) == 0 {
		t.Errorf("status of the example = %+v, want upcoming Occurrences, Synced=True because Scheduled", s)
	}
}

func TestTheServiceWatchesRecurringTournamentsAsTheyAreApplied(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustRunManifests(t, h)

	mustApplyRecurring(t, h, "games", "weeknight-cup", recurringSpec())

	deadline := time.Now().Add(10 * time.Second)
	for len(mustReadRecurringStatus(t, h, "games", "weeknight-cup").Upcoming) != 2 {
		if time.Now().After(deadline) {
			t.Fatal("the RecurringTournament's status doesn't list 2 upcoming Occurrences after 10s")
		}
		time.Sleep(20 * time.Millisecond)
	}
	if got := startTimes(listOccurrences(h)); !sameInstants(got, []time.Time{tuesdayAt20, wednesdayAt20}) {
		t.Errorf("Tournaments start at %v, want Tuesday and Wednesday", got)
	}
}

func TestTournamentManifestsWorkWithoutTheRecurringTournamentCRD(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	// A cluster upgraded without applying the new CRD serves no such resource.
	h.FakeKubernetes.PrependReactor("*", "recurringtournaments", func(ktesting.Action) (bool, runtime.Object, error) {
		return true, nil, apierrors.NewNotFound(schema.GroupResource{Group: "opentournament.io", Resource: "recurringtournaments"}, "")
	})
	mustRunManifests(t, h)

	mustApply(t, h, "games", "friday-cup", manifestSpec(h))

	deadline := time.Now().Add(10 * time.Second)
	for mustReadStatus(t, h, "games", "friday-cup").TournamentID == "" {
		if time.Now().After(deadline) {
			t.Fatal("the Tournament Manifest's status names no Tournament after 10s")
		}
		time.Sleep(20 * time.Millisecond)
	}
}
