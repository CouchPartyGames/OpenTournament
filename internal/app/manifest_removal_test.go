package app_test

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"slices"
	"strings"
	"sync"
	"sync/atomic"
	"testing"
	"time"

	"github.com/couchpartygames/opentournament/internal/app/apptest"
	"github.com/couchpartygames/opentournament/internal/features/manifests"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime"
	ktesting "k8s.io/client-go/testing"
)

func mustDeleteManifest(t *testing.T, h *apptest.Harness, namespace, name string) {
	t.Helper()
	if err := h.FakeKubernetes.Resource(manifests.Resource).Namespace(namespace).Delete(context.Background(), name, metav1.DeleteOptions{}); err != nil {
		t.Fatalf("delete manifest %s/%s: %v", namespace, name, err)
	}
}

// runManifests starts the real watches and sweeps, leaving the clock and
// scheduled Tournament jobs under the scenario's control.
func runManifests(t *testing.T, h *apptest.Harness) func() {
	t.Helper()
	h.App.Manifests.Resync = time.Second
	ctx, cancel := context.WithCancel(context.Background())
	done := make(chan struct{})
	go func() {
		defer close(done)
		h.App.Manifests.Run(ctx)
	}()
	stop := sync.OnceFunc(func() { cancel(); <-done })
	t.Cleanup(stop)
	return stop
}

// awaitTournament observes the HTTP seam; an empty status means absent.
func awaitTournament(h *apptest.Harness, id, status string) error {
	deadline := time.Now().Add(5 * time.Second)
	for {
		r := h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil)
		if status == "" && r.Status == http.StatusNotFound {
			return nil
		}
		if r.Status == http.StatusOK {
			var tv tournamentView
			r.Decode(&tv)
			if tv.Status == status {
				return nil
			}
		}
		if time.Now().After(deadline) {
			return fmt.Errorf("GET Tournament %s = %s, want status %q (empty means absent)", id, r.Body, status)
		}
		time.Sleep(20 * time.Millisecond)
	}
}

func TestDeletingAManifestRemovesItsDraft(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	runManifests(t, h)
	// Observe a declaration made by the running controller, so deletion is
	// exercised after watch startup rather than only on the initial sweep.
	deadline := time.Now().Add(5 * time.Second)
	id := ""
	for id == "" {
		id = mustReadStatus(t, h, "games", "removed").TournamentID
		if time.Now().After(deadline) {
			t.Fatal("the running controller never declared the removed Manifest")
		}
		time.Sleep(20 * time.Millisecond)
	}

	mustDeleteManifest(t, h, "games", "removed")

	if err := awaitTournament(h, id, ""); err != nil {
		t.Error(err)
	}
	h.Advance(startsIn)
	h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil).Expect(http.StatusNotFound)
}

func TestDeletingAManifestCancelsRegistrationAndAnnouncesItLive(t *testing.T) {
	for _, checkIn := range []bool{false, true} {
		t.Run(fmt.Sprintf("check-in=%t", checkIn), func(t *testing.T) {
			h := apptest.MustStart(t, apptest.WatchManifests("games"))
			spec := manifestSpec(h)
			if checkIn {
				spec["checkIn"] = map[string]any{"enabled": true, "windowSeconds": 1800}
			}
			mustApply(t, h, "games", "removed", spec)
			mustApply(t, h, "games", "retained", manifestSpec(h))
			h.Settle()
			id := mustReadStatus(t, h, "games", "removed").TournamentID
			tt := &scenario{Harness: h, t: t, ID: id, Tokens: map[string]string{}, Names: map[string]string{}}
			tt.openRegistration()
			tt.mustRegister("anna", "bert")
			want := "registration-open"
			if checkIn {
				want = "check-in"
			}
			if got := tt.get().Status; got != want {
				t.Fatalf("GET Tournament status = %s, want %s before deletion", got, want)
			}
			ws := connect(t, h)
			ws.subscribe(id)
			runManifests(t, h)

			mustDeleteManifest(t, h, "games", "removed")

			if err := awaitTournament(h, id, "cancelled"); err != nil {
				t.Fatal(err)
			}
			ev := ws.next(isEvent("tournament.status-changed"))
			var data struct{ Status string }
			if err := json.Unmarshal(ev.Data, &data); err != nil {
				t.Fatalf("decode cancellation event: %v", err)
			}
			if data.Status != "cancelled" {
				t.Errorf("live status = %q, want cancelled", data.Status)
			}
			tt.start()
			if got := tt.get().Status; got != "cancelled" {
				t.Errorf("GET Tournament at start time = %s, want cancelled", got)
			}
			if got := h.FakeServers.Running(); got != 0 {
				t.Errorf("Game Servers after cancellation = %d, want 0", got)
			}
		})
	}
}

func TestManifestRemovalLeavesStartedAndFinishedTournamentsUntouched(t *testing.T) {
	for _, status := range []string{"running", "completed", "cancelled"} {
		t.Run(status, func(t *testing.T) {
			h := apptest.MustStart(t, apptest.WatchManifests("games"))
			mustApply(t, h, "games", "target", manifestSpec(h))
			h.Settle()
			id := mustReadStatus(t, h, "games", "target").TournamentID
			tt := &scenario{Harness: h, t: t, ID: id, Organizer: h.Client("arena-backend"), Tokens: map[string]string{}, Names: map[string]string{}}
			tt.openRegistration()
			tt.mustRegister("anna", "bert")
			tt.start()
			switch status {
			case "completed":
				tt.playAll(tt.byName)
			case "cancelled":
				tt.Do(http.MethodPost, tt.path("cancel"), tt.Organizer, nil).Expect(http.StatusOK)
			}
			before := tt.get()
			servers := h.FakeServers.Running()
			mustApply(t, h, "games", "probe", manifestSpec(h))
			mustApply(t, h, "games", "retained", manifestSpec(h))
			h.Settle()
			probeID := mustReadStatus(t, h, "games", "probe").TournamentID
			mustDeleteManifest(t, h, "games", "target")
			mustDeleteManifest(t, h, "games", "probe")

			runManifests(t, h)

			// The removed Draft proves that the startup sweep completed.
			if err := awaitTournament(h, probeID, ""); err != nil {
				t.Fatal(err)
			}
			if got := tt.get(); got != before || got.Status != status {
				t.Errorf("GET Tournament = %+v, want unchanged %+v in %s", got, before, status)
			}
			if got := h.FakeServers.Running(); got != servers {
				t.Errorf("Game Servers = %d, want unchanged %d", got, servers)
			}
		})
	}
}

func TestManifestDeletedWhileStoppedIsRemovedOnStartup(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games"))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "removed").TournamentID
	mustDeleteManifest(t, h, "games", "removed")

	// A fresh controller has never seen the removed Manifest or its event.
	h.App.Manifests = manifests.New(h.FakeKubernetes, h.App.Service, []string{"games"})
	runManifests(t, h)

	if err := awaitTournament(h, id, ""); err != nil {
		t.Error(err)
	}
}

func TestRemovingAWatchedNamespaceLeavesItsTournamentsUntouched(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games", "elsewhere"))
	mustApply(t, h, "elsewhere", "unwatched", manifestSpec(h))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	unwatchedID := mustReadStatus(t, h, "elsewhere", "unwatched").TournamentID
	removedID := mustReadStatus(t, h, "games", "removed").TournamentID
	mustDeleteManifest(t, h, "elsewhere", "unwatched")
	mustDeleteManifest(t, h, "games", "removed")

	h.App.Manifests = manifests.New(h.FakeKubernetes, h.App.Service, []string{"games"})
	runManifests(t, h)

	if err := awaitTournament(h, removedID, ""); err != nil {
		t.Fatal(err)
	}
	var tv tournamentView
	h.Do(http.MethodGet, "/api/v1/tournaments/"+unwatchedID, "", nil).Expect(http.StatusOK).Decode(&tv)
	if tv.Status != "draft" {
		t.Errorf("GET unwatched Tournament status = %s, want draft", tv.Status)
	}
}

func TestManifestRemovalWaitsForEveryWatchToSync(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games", "slow"))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "removed").TournamentID
	mustDeleteManifest(t, h, "games", "removed")
	listing := make(chan struct{})
	listed := sync.OnceFunc(func() { close(listing) })
	release := make(chan struct{})
	unblock := sync.OnceFunc(func() { close(release) })
	h.FakeKubernetes.PrependReactor("list", "tournaments", func(action ktesting.Action) (bool, runtime.Object, error) {
		if action.GetNamespace() == "slow" {
			listed()
			<-release
		}
		return false, nil, nil
	})
	runManifests(t, h)
	t.Cleanup(unblock)
	select {
	case <-listing:
	case <-time.After(5 * time.Second):
		t.Fatal("the slow namespace never began its initial list")
	}
	// Hold back one namespace beyond the sweep interval. Another namespace
	// having synced must never be enough to authorize removal.
	time.Sleep(1200 * time.Millisecond)
	var tv tournamentView
	h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil).Expect(http.StatusOK).Decode(&tv)
	if tv.Status != "draft" {
		t.Errorf("GET Tournament before every watch synced = %s, want draft", tv.Status)
	}
	unblock()
	if err := awaitTournament(h, id, ""); err != nil {
		t.Error(err)
	}
}

// mustCaptureManifestLogs captures only this controller's operational logs.
func mustCaptureManifestLogs(t *testing.T, h *apptest.Harness) func() string {
	t.Helper()
	f, err := os.CreateTemp(t.TempDir(), "manifest-logs")
	if err != nil {
		t.Fatalf("create log file: %v", err)
	}
	t.Cleanup(func() {
		if err := f.Close(); err != nil {
			t.Errorf("close log file: %v", err)
		}
	})
	h.App.Manifests.Logger = slog.New(slog.NewJSONHandler(f, nil))
	return func() string {
		b, err := os.ReadFile(f.Name())
		if err != nil {
			t.Fatalf("read manifest logs: %v", err)
		}
		return string(b)
	}
}

func TestManifestRemovalSkipsAndLogsMoreThanHalf(t *testing.T) {
	for _, count := range []int{1, 3} {
		t.Run(fmt.Sprintf("declared=%d", count), func(t *testing.T) {
			h := apptest.MustStart(t, apptest.WatchManifests("games", "elsewhere"))
			for i := range count {
				mustApply(t, h, "games", fmt.Sprintf("cup-%d", i), manifestSpec(h))
			}
			// Neither API-created nor unwatched Tournaments may inflate the
			// denominator and bypass the removal guard.
			for i := range 3 {
				mustApply(t, h, "elsewhere", fmt.Sprintf("cup-%d", i), manifestSpec(h))
				mustCreate(t, h, settings(h))
			}
			h.Settle()
			before := listTournaments(h)
			for i := range count/2 + 1 {
				mustDeleteManifest(t, h, "games", fmt.Sprintf("cup-%d", i))
			}
			h.App.Manifests = manifests.New(h.FakeKubernetes, h.App.Service, []string{"games"})
			logs := mustCaptureManifestLogs(t, h)
			runManifests(t, h)

			deadline := time.Now().Add(5 * time.Second)
			for !strings.Contains(logs(), `"level":"ERROR"`) {
				if time.Now().After(deadline) {
					t.Fatalf("manifest removal produced no error log: %s", logs())
				}
				time.Sleep(20 * time.Millisecond)
			}
			if got := logs(); !strings.Contains(got, fmt.Sprintf(`"removals":%d`, count/2+1)) || !strings.Contains(got, fmt.Sprintf(`"declared":%d`, count)) {
				t.Errorf("manifest removal log = %s, want removals and declared counts", got)
			}
			if got := listTournaments(h); !slices.Equal(got, before) {
				t.Errorf("GET Tournaments after guarded sweep = %+v, want unchanged %+v", got, before)
			}
		})
	}
}

func TestManifestRemovalRetriesAFailedNamespaceListWithoutRemovingAnything(t *testing.T) {
	h := apptest.MustStart(t, apptest.WatchManifests("games", "unavailable"))
	mustApply(t, h, "games", "removed", manifestSpec(h))
	mustApply(t, h, "games", "retained", manifestSpec(h))
	h.Settle()
	id := mustReadStatus(t, h, "games", "removed").TournamentID
	mustDeleteManifest(t, h, "games", "removed")
	var failed atomic.Bool
	failed.Store(true)
	lists := 0
	h.FakeKubernetes.PrependReactor("list", "tournaments", func(action ktesting.Action) (bool, runtime.Object, error) {
		if action.GetNamespace() == "unavailable" {
			lists++
			// Allow the watch's initial list, then make removal sweeps fail.
			if lists > 1 && failed.Load() {
				return true, nil, fmt.Errorf("namespace temporarily unavailable")
			}
		}
		return false, nil, nil
	})
	logs := mustCaptureManifestLogs(t, h)
	runManifests(t, h)
	deadline := time.Now().Add(5 * time.Second)
	for !strings.Contains(logs(), `"level":"WARN"`) {
		if time.Now().After(deadline) {
			t.Fatalf("the unavailable namespace produced no retry log: %s", logs())
		}
		time.Sleep(20 * time.Millisecond)
	}
	var tv tournamentView
	h.Do(http.MethodGet, "/api/v1/tournaments/"+id, "", nil).Expect(http.StatusOK).Decode(&tv)
	if tv.Status != "draft" {
		t.Errorf("GET Tournament after a failed namespace list = %s, want draft", tv.Status)
	}
	failed.Store(false)
	if err := awaitTournament(h, id, ""); err != nil {
		t.Error(err)
	}
}
