package manifests

import (
	"cmp"
	"context"
	"errors"
	"fmt"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/recurrence"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"k8s.io/apimachinery/pkg/api/equality"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	"k8s.io/apimachinery/pkg/api/meta"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
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
	// Suspend stops declaring new Occurrences, like a suspended CronJob. The
	// Tournaments already declared are kept.
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
// and reports its upcoming Occurrences in its status. A Recurring Tournament
// that can't be applied is reported rather than retried, and declares
// nothing. Recurring Tournaments outside the watched namespaces are ignored.
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

	s, err := c.schedule(ctx, name, spec)
	if err != nil {
		return time.Time{}, err
	}
	if s.upcoming != nil {
		want.Upcoming = *s.upcoming
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
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, ReasonSuspended, "No new Occurrences are declared while suspended"
	default:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, ReasonScheduled, "Every Occurrence in the lookahead window is declared"
	}
	if cond.Status == metav1.ConditionTrue {
		want.ObservedGeneration = u.GetGeneration()
	}
	meta.SetStatusCondition(&want.Conditions, cond)
	// As for a Tournament Manifest, writing only on change keeps the status
	// update's own watch event from triggering another write, and a
	// declaration always writes so that a conflicting replica retries.
	if equality.Semantic.DeepEqual(status, want) && !s.declared {
		return s.next, nil
	}
	var object map[string]any
	if err := convert(want, &object); err != nil {
		return time.Time{}, err
	}
	u.Object["status"] = object
	if _, err := client.UpdateStatus(ctx, u, metav1.UpdateOptions{}); err != nil {
		return time.Time{}, fmt.Errorf("update status of recurring tournament %v: %w", name, err)
	}
	return s.next, nil
}

// scheduled is what scheduling a Recurring Tournament found.
type scheduled struct {
	// upcoming are the Occurrences in the window with a Tournament, or nil
	// when the schedule can't be read, which leaves them unknown.
	upcoming *[]Occurrence
	// declared is true when a Tournament was declared.
	declared bool
	// next is when to schedule again, or zero if nothing is due.
	next time.Time
	// reason and message say why the Recurring Tournament can't be applied,
	// and are empty when it can.
	reason, message string
}

// schedule declares a Tournament for each Occurrence in the window that has
// none, unless spec is suspended or can't be applied.
func (c *Controller) schedule(ctx context.Context, name cache.ObjectName, spec RecurringSpec) (scheduled, error) {
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
	s := scheduled{upcoming: new([]Occurrence)}
	tmpl, err := readTemplate(spec.Template, sched.Location())
	if err == nil {
		// Only the instants of an Occurrence's Tournament depend on when it
		// starts, so any Occurrence in the window checks the template.
		err = tournaments.Validate(c.svc, tmpl.organizer, tmpl.occurrence(now.Add(cmp.Or(lookahead, recurrence.DefaultLookahead))))
	}
	if err != nil {
		if s.reason, s.message, err = refusal(err); err != nil {
			return scheduled{}, err
		}
	}
	for _, start := range starts {
		manifest := fmt.Sprintf("recurring/%v/%s", name, start.Format(time.RFC3339))
		tv, err := tournaments.FindDeclared(ctx, c.svc.Queries, manifest)
		if errors.Is(err, tournament.ErrTournamentNotFound) {
			if spec.Suspend || s.reason != "" {
				continue
			}
			tv, _, err = tournaments.Declare(ctx, c.svc, manifest, tmpl.organizer, tmpl.occurrence(start))
			if err != nil {
				if s.reason, s.message, err = refusal(err); err != nil {
					return scheduled{}, fmt.Errorf("declare occurrence %s of recurring tournament %v: %w", start.Format(time.RFC3339), name, err)
				}
				continue
			}
			s.declared = true
		} else if err != nil {
			return scheduled{}, fmt.Errorf("find occurrence %s of recurring tournament %v: %w", start.Format(time.RFC3339), name, err)
		}
		*s.upcoming = append(*s.upcoming, Occurrence{StartsAt: metav1.NewTime(start), TournamentID: tv.ID.String(), TournamentStatus: tv.Status})
	}
	if s.reason == "" && !spec.Suspend {
		s.next, _ = sched.NextEntry(now)
	}
	return s, nil
}

// refusal returns the reason and message of the Synced condition when err
// says a template can't be applied, or err itself when it doesn't.
func refusal(err error) (reason, message string, _ error) {
	var p *problem.Error
	if errors.As(err, &p) && notSynced[p.Code] != "" {
		return notSynced[p.Code], describe(p, "spec.template."), nil
	}
	return "", "", err
}

// template is a Recurring Tournament's template, read.
type template struct {
	organizer               auth.Principal
	body                    tournaments.NewTournament
	registrationOpensBefore time.Duration
	loc                     *time.Location
}

// readTemplate reads a template whose Occurrences are named on the wall
// clock in loc. The error is the *problem.Error the API would return for its
// fields.
func readTemplate(t Template, loc *time.Location) (template, error) {
	var f problem.Fields
	organizer, err := auth.ParsePrincipal(t.Organizer)
	if err != nil {
		f.Add("body.organizer", "must be user:<subject> or client:<client id>", t.Organizer)
	}
	before, err := time.ParseDuration(t.RegistrationOpensBefore)
	if err != nil || before <= 0 {
		f.Add("body.registrationOpensBefore", "must be a positive duration such as 30m", t.RegistrationOpensBefore)
	}
	return template{organizer: organizer, body: t.NewTournament, registrationOpensBefore: before, loc: loc}, f.Err()
}

// occurrence returns the Tournament the template declares for the Occurrence
// at start. Its name ends with the start on the wall clock, e.g.
// "Weeknight Cup 2026-10-02 20:00".
func (t template) occurrence(start time.Time) tournaments.NewTournament {
	body := t.body
	body.Name = t.body.Name + " " + start.In(t.loc).Format("2006-01-02 15:04")
	body.StartsAt, body.RegistrationOpensAt = start, start.Add(-t.registrationOpensBefore)
	return body
}
