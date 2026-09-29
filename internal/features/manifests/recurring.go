package manifests

import (
	"context"
	"errors"
	"fmt"
	"slices"
	"strings"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/recurrence"
	"github.com/couchpartygames/opentournament/internal/tournament"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	"k8s.io/apimachinery/pkg/api/meta"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/apis/meta/v1/unstructured"
	"k8s.io/apimachinery/pkg/runtime/schema"
	"k8s.io/client-go/tools/cache"
)

// RecurringResource is the Recurring Tournament's Kubernetes resource.
var RecurringResource = schema.GroupVersionResource{Group: "opentournament.io", Version: "v1alpha1", Resource: "recurringtournaments"}

// RecurringSpec is a Recurring Tournament's desired configuration: a schedule
// in a time zone, and the Tournament each of its Occurrences declares.
type RecurringSpec struct {
	// Schedule is a five-field cron expression: when each Occurrence starts.
	Schedule string `json:"schedule"`
	// TimeZone is the IANA time zone the Schedule is read in.
	TimeZone string `json:"timeZone"`
	// Lookahead is how far ahead Occurrences are declared, as a Go duration
	// such as 48h. Empty means recurrence.DefaultLookahead.
	Lookahead string `json:"lookahead,omitempty"`
	// Suspend stops declaring Occurrences, like a suspended CronJob, and no
	// Occurrence is wanted: the Tournaments of those that haven't started are
	// removed.
	Suspend  bool     `json:"suspend,omitempty"`
	Template Template `json:"template"`
}

// Template is the Tournament each Occurrence declares. Its name gets the
// Occurrence's start appended, and the Occurrence sets both of its instants:
// StartsAt and RegistrationOpensAt are ignored.
type Template struct {
	// Organizer is a Principal ID: user:<keycloak subject> or client:<keycloak client id>.
	Organizer string `json:"organizer"`
	// RegistrationOpensBefore is how long before each Occurrence starts its
	// Registration Window opens, as a Go duration such as 30m.
	RegistrationOpensBefore string `json:"registrationOpensBefore"`
	tournaments.NewTournament
}

// RecurringStatus reports the upcoming Occurrences of a Recurring Tournament.
type RecurringStatus struct {
	// ObservedGeneration is the latest generation whose Occurrences are
	// declared. While the Recurring Tournament isn't Synced, it stays the
	// last one that was.
	ObservedGeneration int64 `json:"observedGeneration,omitempty"`
	// LastScheduleTime is the start of the latest Occurrence declared.
	LastScheduleTime *metav1.Time `json:"lastScheduleTime,omitempty"`
	// Upcoming are the declared Occurrences within the lookahead window that
	// haven't started, earliest first.
	Upcoming   []Occurrence       `json:"upcoming,omitempty"`
	Conditions []metav1.Condition `json:"conditions,omitempty"`
}

// Occurrence is an upcoming Occurrence and the Tournament it declared.
type Occurrence struct {
	StartsAt         metav1.Time                `json:"startsAt"`
	TournamentID     string                     `json:"tournamentId"`
	TournamentStatus lifecycle.TournamentStatus `json:"tournamentStatus"`
}

// Reasons of a Recurring Tournament's Synced condition, besides those it
// shares with a Tournament Manifest for a template that can't be applied.
const (
	ReasonScheduled = "Scheduled"
	ReasonSuspended = "Suspended"

	ReasonInvalidSchedule  = "InvalidSchedule"
	ReasonUnknownTimeZone  = "UnknownTimeZone"
	ReasonInvalidLookahead = "InvalidLookahead"
)

// unscheduled are the reasons a schedule can't be read, by the error
// recurrence.Parse returns.
var unscheduled = map[error]string{
	recurrence.ErrInvalidSchedule:  ReasonInvalidSchedule,
	recurrence.ErrUnknownTimeZone:  ReasonUnknownTimeZone,
	recurrence.ErrInvalidLookahead: ReasonInvalidLookahead,
}

// ReconcileRecurring declares a Tournament for each Occurrence of one
// Recurring Tournament within its lookahead window, unless it is suspended,
// edits those still Drafts to match its template, and removes those of
// Occurrences still ahead that it no longer wants. It reports its upcoming
// Occurrences in its status, and the Occurrences whose settings froze before
// the template changed. A Recurring Tournament that can't be applied is
// reported rather than retried, and changes nothing. Recurring Tournaments
// outside the watched namespaces are ignored.
//
// It returns when the next Occurrence enters the window, which is when to
// reconcile again, or zero if nothing is due.
func (c *Controller) ReconcileRecurring(ctx context.Context, name cache.ObjectName) (time.Time, error) {
	if !slices.Contains(c.namespaces, name.Namespace) {
		return time.Time{}, nil
	}
	client := c.client.Resource(RecurringResource).Namespace(name.Namespace)
	u, err := client.Get(ctx, name.Name, metav1.GetOptions{})
	if apierrors.IsNotFound(err) {
		return time.Time{}, nil
	} else if err != nil {
		return time.Time{}, fmt.Errorf("get recurring tournament %v: %w", name, err)
	}
	var spec RecurringSpec
	if err := convert(u.Object["spec"], &spec); err != nil {
		return time.Time{}, fmt.Errorf("read recurring tournament %v: %w", name, err)
	}
	var status, want RecurringStatus
	if err := convert(u.Object["status"], &status); err != nil {
		return time.Time{}, fmt.Errorf("read status of recurring tournament %v: %w", name, err)
	}
	if err := convert(status, &want); err != nil {
		return time.Time{}, err
	}

	s, err := c.schedule(ctx, u, spec)
	if err != nil {
		return time.Time{}, err
	}
	if s.readable {
		want.Upcoming = s.upcoming
	}
	if n := len(want.Upcoming); n > 0 {
		latest := want.Upcoming[n-1].StartsAt
		if want.LastScheduleTime == nil || want.LastScheduleTime.Before(&latest) {
			want.LastScheduleTime = &latest
		}
	}
	cond := metav1.Condition{Type: ConditionSynced, ObservedGeneration: u.GetGeneration(), LastTransitionTime: metav1.NewTime(c.svc.Clock.Now())}
	switch {
	case s.reason != "":
		cond.Status, cond.Reason, cond.Message = metav1.ConditionFalse, s.reason, s.message
	case spec.Suspend:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, ReasonSuspended, "No Occurrences are declared while suspended"
	default:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, ReasonScheduled, "Every Occurrence in the lookahead window is declared"
	}
	if cond.Status == metav1.ConditionTrue {
		want.ObservedGeneration = u.GetGeneration()
	}
	meta.SetStatusCondition(&want.Conditions, cond)
	if err := c.updateStatus(ctx, RecurringResource, u, status, want, s.changed); err != nil {
		return time.Time{}, err
	}
	return s.next, nil
}

// scheduled is what scheduling a Recurring Tournament found.
type scheduled struct {
	// readable is false when the schedule can't be read, which leaves the
	// upcoming Occurrences unknown.
	readable bool
	// upcoming are the Occurrences in the window with a Tournament.
	upcoming []Occurrence
	// changed is true when a Tournament was created, edited or removed.
	changed bool
	// next is when to schedule again, or zero if nothing is due.
	next time.Time
	// reason and message say why the Recurring Tournament, or some of its
	// Occurrences, can't be applied, and are empty when all can.
	reason, message string
}

// schedule makes the Tournaments of a Recurring Tournament's Occurrences
// match spec, the spec of u: it removes those of Occurrences no longer wanted,
// then declares one for each Occurrence in the window, unless spec is
// suspended, and edits those still Drafts. A spec that can't be applied
// changes nothing.
func (c *Controller) schedule(ctx context.Context, u *unstructured.Unstructured, spec RecurringSpec) (scheduled, error) {
	name := cache.ObjectName{Namespace: u.GetNamespace(), Name: u.GetName()}
	var lookahead time.Duration
	if spec.Lookahead != "" {
		d, err := time.ParseDuration(spec.Lookahead)
		if err != nil {
			return scheduled{reason: ReasonInvalidLookahead, message: fmt.Sprintf("spec.lookahead: %q is not a duration such as 48h", spec.Lookahead)}, nil
		}
		lookahead = d
	}
	sched, err := recurrence.Parse(spec.Schedule, spec.TimeZone, lookahead)
	if err != nil {
		for sentinel, reason := range unscheduled {
			if errors.Is(err, sentinel) {
				return scheduled{reason: reason, message: err.Error()}, nil
			}
		}
		return scheduled{}, err
	}

	now := c.svc.Clock.Now()
	var starts []time.Time
	for _, o := range sched.Occurrences(now) {
		// A Tournament must start in the future.
		if o.After(now) {
			starts = append(starts, o)
		}
	}
	s := scheduled{readable: true}
	tmpl, err := readTemplate(spec.Template, sched.Location())
	if err == nil {
		// Only the instants of an Occurrence's Tournament depend on when it
		// starts, so any Occurrence in the window checks the template.
		windowEnd := now.Add(sched.Lookahead())
		err = tournaments.Validate(c.svc, tmpl.organizer, tmpl.occurrence(windowEnd))
	}
	if err != nil {
		if s.reason, s.message, err = refusal(err); err != nil {
			return scheduled{}, err
		}
	}
	refused := s.reason != ""
	declared, err := c.svc.Queries.ListOccurrenceTournaments(ctx, recurringManifest(name)+"/")
	if err != nil {
		return scheduled{}, fmt.Errorf("list occurrences of recurring tournament %v: %w", name, err)
	}
	if !refused {
		// wanted are the Occurrences spec wants at now, by manifest.
		wanted := func(now time.Time) map[string]bool {
			out := map[string]bool{}
			if !spec.Suspend {
				for _, o := range sched.Occurrences(now) {
					out[occurrenceManifest(name, o)] = true
				}
			}
			return out
		}
		if s.changed, err = c.removeUnwanted(ctx, u, declared, wanted); err != nil {
			return scheduled{}, err
		}
	}

	cancelled := map[string]bool{}
	for _, t := range declared {
		cancelled[*t.Manifest] = t.Status == lifecycle.Cancelled
	}
	// Some Occurrences may refuse spec, such as those whose settings froze.
	// The others are still declared.
	var frozen []string
	for _, start := range starts {
		manifest := occurrenceManifest(name, start)
		// A cancelled Occurrence stays as it is, like any other cancelled
		// Tournament, so it never drifts from the template.
		if !spec.Suspend && !refused && !cancelled[manifest] {
			tv, change, err := tournaments.Declare(ctx, c.svc, manifest, tmpl.organizer, tmpl.occurrence(start))
			reason, message, err := refusal(err)
			if err != nil {
				return scheduled{}, fmt.Errorf("declare occurrence %s of recurring tournament %v: %w", start.Format(time.RFC3339), name, err)
			}
			if reason == "" {
				s.changed = s.changed || change != tournaments.Unchanged
				s.upcoming = append(s.upcoming, upcoming(start, tv))
				continue
			}
			if reason == ReasonSettingsFrozen {
				frozen = append(frozen, start.In(sched.Location()).Format("2006-01-02 15:04"))
			} else if s.reason == "" {
				s.reason, s.message = reason, message
			}
		}
		// The Occurrence isn't declared as spec says, but its Tournament is
		// still upcoming if it has one.
		tv, err := tournaments.FindDeclared(ctx, c.svc.Queries, manifest)
		if errors.Is(err, tournament.ErrTournamentNotFound) {
			continue
		} else if err != nil {
			return scheduled{}, fmt.Errorf("find occurrence %s of recurring tournament %v: %w", start.Format(time.RFC3339), name, err)
		}
		s.upcoming = append(s.upcoming, upcoming(start, tv))
	}
	if s.reason == "" && len(frozen) > 0 {
		s.reason = ReasonSettingsFrozen
		s.message = "The Occurrences starting " + strings.Join(frozen, ", ") + " keep their settings, frozen once registration opened"
	}
	if !refused && !spec.Suspend {
		s.next, _ = sched.NextEntry(now)
	}
	return s, nil
}

// removeUnwanted applies the removal policy to the declared Tournaments of
// u's Occurrences that are still ahead and not wanted, by manifest, at the
// time. It reports whether it removed any.
func (c *Controller) removeUnwanted(ctx context.Context, u *unstructured.Unstructured, declared []db.Tournament, wanted func(now time.Time) map[string]bool) (bool, error) {
	name := key{RecurringResource, cache.ObjectName{Namespace: u.GetNamespace(), Name: u.GetName()}}
	now := c.svc.Clock.Now()
	want := wanted(now)
	removed := false
	for _, t := range declared {
		if want[*t.Manifest] || !removable(t, now) {
			continue
		}
		err := c.svc.InTournament(ctx, t.ID, func(tx *tournament.Tx) error {
			// Only u, as it is when the lock is held, can make an Occurrence
			// unwanted: the window may have moved on, bringing in an
			// Occurrence another replica just declared, and that replica may
			// have applied a newer spec, or a Recurring Tournament recreated
			// under the same name. A deleted one is the sweep's.
			latest, err := c.get(ctx, name)
			if apierrors.IsNotFound(err) {
				return nil
			} else if err != nil {
				return fmt.Errorf("get recurring tournament %v: %w", name.name, err)
			}
			sameSpec := latest.GetUID() == u.GetUID() && latest.GetGeneration() == u.GetGeneration()
			if !sameSpec || wanted(tx.Now())[*tx.T.Manifest] || !removable(tx.T, tx.Now()) {
				return nil
			}
			removed = true
			return remove(tx)
		})
		if err != nil && !errors.Is(err, tournament.ErrTournamentNotFound) {
			return removed, fmt.Errorf("remove occurrence %s of recurring tournament %v: %w", t.StartsAt.Format(time.RFC3339), name.name, err)
		}
	}
	return removed, nil
}

// removable reports whether a change of schedule can remove an Occurrence's
// Tournament at now: it hasn't started, and its start is still ahead.
func removable(t db.Tournament, now time.Time) bool {
	return unstarted(t.Status) && t.StartsAt.After(now)
}

// recurringManifest is how the Tournaments of a Recurring Tournament's
// Occurrences record it, before their start.
func recurringManifest(name cache.ObjectName) string {
	return "recurring/" + name.String()
}

// occurrenceManifest is the manifest the Tournament of the Occurrence at
// start records: recurring/<namespace>/<name>/<start>.
func occurrenceManifest(name cache.ObjectName, start time.Time) string {
	return recurringManifest(name) + "/" + start.UTC().Format(time.RFC3339)
}

// upcoming is the Occurrence at start, and the Tournament it declared.
func upcoming(start time.Time, tv tournaments.TournamentView) Occurrence {
	return Occurrence{StartsAt: metav1.NewTime(start), TournamentID: tv.ID.String(), TournamentStatus: tv.Status}
}

// refusal returns the reason and message of the Synced condition when err
// says a template can't be applied, or err itself when it doesn't.
func refusal(err error) (reason, message string, _ error) {
	var p *problem.Error
	if errors.As(err, &p) && notSynced[p.Code] != "" {
		return notSynced[p.Code], describe(p, "The template", "spec.template."), nil
	}
	return "", "", err
}

// occurrenceTemplate is a Recurring Tournament's template, read.
type occurrenceTemplate struct {
	organizer               auth.Principal
	body                    tournaments.NewTournament
	registrationOpensBefore time.Duration
	loc                     *time.Location
}

// readTemplate reads a template whose Occurrences are named on the wall
// clock in loc. The error is the *problem.Error the API would return for its
// fields.
func readTemplate(t Template, loc *time.Location) (occurrenceTemplate, error) {
	var f problem.Fields
	organizer, err := auth.ParsePrincipal(t.Organizer)
	if err != nil {
		f.Add("body.organizer", "must be user:<subject> or client:<client id>", t.Organizer)
	}
	before, err := time.ParseDuration(t.RegistrationOpensBefore)
	if err != nil || before <= 0 {
		f.Add("body.registrationOpensBefore", "must be a positive duration such as 30m", t.RegistrationOpensBefore)
	}
	return occurrenceTemplate{organizer: organizer, body: t.NewTournament, registrationOpensBefore: before, loc: loc}, f.Err()
}

// occurrence returns the Tournament the template declares for the Occurrence
// at start. Its name ends with the start on the wall clock, e.g.
// "Weeknight Cup 2026-10-02 20:00".
func (t occurrenceTemplate) occurrence(start time.Time) tournaments.NewTournament {
	body := t.body
	body.Name = t.body.Name + " " + start.In(t.loc).Format("2006-01-02 15:04")
	body.StartsAt, body.RegistrationOpensAt = start, start.Add(-t.registrationOpensBefore)
	return body
}
