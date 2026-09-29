// Package manifests is the slice that declares Tournaments from Tournament
// Manifests: Kubernetes resources, synced from git, that hold a Tournament's
// configuration and its Organizer (ADR-0005). It watches the Manifests of the
// configured namespaces through a rate-limited workqueue, creates the
// Tournament each one declares and keeps its settings matching, since git is
// their source of truth, and reports it in the Manifest's status.
//
// It also watches Recurring Tournaments, and declares a Tournament for each
// Occurrence of their schedules within the lookahead window.
//
// Every replica runs the controller, without leader election: a Manifest
// declares at most one Tournament, and each Occurrence of a Recurring
// Tournament one more, and their status is a function of those Tournaments,
// so replicas that reconcile the same Manifest converge.
//
// RBAC needed: get, list and watch tournaments.opentournament.io and
// recurringtournaments.opentournament.io, and update their status, in every
// watched namespace.
package manifests

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"log/slog"
	"slices"
	"strings"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"k8s.io/apimachinery/pkg/api/equality"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	"k8s.io/apimachinery/pkg/api/meta"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime/schema"
	"k8s.io/client-go/dynamic"
	"k8s.io/client-go/dynamic/dynamicinformer"
	"k8s.io/client-go/tools/cache"
	"k8s.io/client-go/util/workqueue"
)

// Resource is the Tournament Manifest's Kubernetes resource.
var Resource = schema.GroupVersionResource{Group: "opentournament.io", Version: "v1alpha1", Resource: "tournaments"}

// Spec is a Tournament Manifest's desired configuration: what the API takes
// to create a Tournament, plus its Organizer.
type Spec struct {
	// Organizer is a Principal ID: user:<keycloak subject> or client:<keycloak client id>.
	Organizer string `json:"organizer"`
	tournaments.NewTournament
}

// Status reports the declared Tournament of a Tournament Manifest.
type Status struct {
	// ObservedGeneration is the latest Manifest generation the Tournament
	// matches. While the Manifest isn't Synced, it stays the last one that was.
	ObservedGeneration int64                      `json:"observedGeneration,omitempty"`
	TournamentID       string                     `json:"tournamentId,omitempty"`
	TournamentStatus   lifecycle.TournamentStatus `json:"tournamentStatus,omitempty"`
	Conditions         []metav1.Condition         `json:"conditions,omitempty"`
}

// ConditionSynced is true when the Tournament matches its Manifest's current
// generation, and false when that generation can't be applied.
const ConditionSynced = "Synced"

// Reasons of the Synced condition. While true, it keeps the reason of the
// last change that synced it.
const (
	ReasonCreated   = "Created"
	ReasonUpdated   = "Updated"
	ReasonUnchanged = "Unchanged"

	ReasonValidationFailed    = "ValidationFailed"
	ReasonSettingsFrozen      = "SettingsFrozen"
	ReasonOrganizerNotTrusted = "OrganizerNotTrusted"
)

// synced are the reason and message of a Synced Manifest, by what declaring
// its Tournament changed.
var synced = map[tournaments.Change]struct{ reason, message string }{
	tournaments.Created:   {ReasonCreated, "The Tournament is created"},
	tournaments.Updated:   {ReasonUpdated, "The Tournament's settings are updated"},
	tournaments.Unchanged: {ReasonUnchanged, "The Tournament matches the Manifest"},
}

// notSynced are the reasons a Manifest can't be applied, by the code of the
// problem the API would return for the same request.
var notSynced = map[string]string{
	problem.CodeValidationFailed:      ReasonValidationFailed,
	tournaments.CodeSettingsFrozen:    ReasonSettingsFrozen,
	tournaments.CodeNotTrustedForGame: ReasonOrganizerNotTrusted,
}

// Controller declares the Tournaments of the Manifests in its namespaces.
type Controller struct {
	client     dynamic.Interface
	svc        *tournament.Service
	namespaces []string
	queue      workqueue.TypedRateLimitingInterface[item]
	// Resync is the period at which every Manifest is reconciled again when
	// nothing happens, so its status follows the Tournament's. It also sets
	// the interval between removal sweeps.
	Resync time.Duration
	// Logger reports reconciliation failures and skipped removal sweeps.
	Logger *slog.Logger
}

// New returns a Controller for the Manifests in namespaces. logger must be
// non-nil. Set Resync and Logger before calling Run.
func New(client dynamic.Interface, svc *tournament.Service, namespaces []string, logger *slog.Logger) *Controller {
	return &Controller{
		client:     client,
		svc:        svc,
		namespaces: namespaces,
		queue: workqueue.NewTypedRateLimitingQueueWithConfig(
			workqueue.DefaultTypedControllerRateLimiter[item](),
			workqueue.TypedRateLimitingQueueConfig[item]{Name: "manifests"}),
		Resync: 30 * time.Second,
		Logger: logger,
	}
}

// Run watches the Manifests and reconciles each one that changes, until ctx
// ends. The informers list every Manifest first, so each is reconciled at
// startup. Run can only be called once.
func (c *Controller) Run(ctx context.Context) {
	ctx, cancel := context.WithCancel(ctx)
	var factories []dynamicinformer.DynamicSharedInformerFactory
	defer func() {
		// Cancel first even if watch setup fails, then join every informer.
		cancel()
		for _, factory := range factories {
			factory.Shutdown()
		}
	}()
	go func() {
		<-ctx.Done()
		c.queue.ShutDown()
	}()
	handler := func(resource schema.GroupVersionResource) cache.ResourceEventHandlerFuncs {
		enqueue := func(obj any) {
			name, err := cache.DeletionHandlingObjectToName(obj)
			if err != nil {
				c.Logger.ErrorContext(ctx, "unexpected object from the manifest informer", "resource", resource.Resource, "error", err)
				return
			}
			c.queue.Add(item{resource, name})
		}
		return cache.ResourceEventHandlerFuncs{
			AddFunc:    enqueue,
			UpdateFunc: func(_, obj any) { enqueue(obj) },
			DeleteFunc: enqueue,
		}
	}
	var synced []cache.InformerSynced
	for _, ns := range c.namespaces {
		factory := dynamicinformer.NewFilteredDynamicSharedInformerFactory(c.client, c.Resync, ns, nil)
		informer := factory.ForResource(Resource).Informer()
		if _, err := informer.AddEventHandler(handler(Resource)); err != nil {
			c.Logger.ErrorContext(ctx, "can't watch the manifests of a namespace; its Tournaments won't be declared", "namespace", ns, "error", err)
			return
		}
		synced = append(synced, informer.HasSynced)
		// Only the removal sweep needs the Tournament Manifests' watches to
		// have synced. Not waiting for the Recurring Tournaments' keeps
		// Tournament Manifests working in a cluster without their CRD.
		recurring := factory.ForResource(RecurringResource).Informer()
		if _, err := recurring.AddEventHandler(handler(RecurringResource)); err != nil {
			c.Logger.ErrorContext(ctx, "can't watch the recurring tournaments of a namespace; their Occurrences won't be declared", "namespace", ns, "error", err)
			return
		}
		factory.Start(ctx.Done())
		factories = append(factories, factory)
	}
	if !cache.WaitForCacheSync(ctx.Done(), synced...) {
		return
	}
	// The empty item is a sweep, not a Manifest. Queueing it at startup and
	// periodically catches removals even when no deletion event was seen.
	sweep := item{}
	c.queue.Add(sweep)
	for {
		it, shutdown := c.queue.Get()
		if shutdown {
			return
		}
		var next time.Time
		var err error
		switch it.resource {
		case Resource:
			err = c.Reconcile(ctx, it.name)
		case RecurringResource:
			next, err = c.ReconcileRecurring(ctx, it.name)
		default:
			err = c.sweep(ctx)
			c.queue.AddAfter(sweep, c.Resync)
		}
		if err != nil && ctx.Err() == nil {
			if it == sweep {
				c.Logger.WarnContext(ctx, "manifest removal sweep failed; retrying", "error", err)
			} else {
				c.Logger.WarnContext(ctx, "reconcile manifest failed; retrying", "resource", it.resource.Resource, "manifest", it.name, "error", err)
			}
			c.queue.AddRateLimited(it)
		} else {
			c.queue.Forget(it)
		}
		if !next.IsZero() {
			// Look again when the next Occurrence enters the window, rather
			// than wait up to a whole resync for it.
			c.queue.AddAfter(it, next.Sub(c.svc.Clock.Now()))
		}
		c.queue.Done(it)
	}
}

// item is what the queue holds: a Manifest of a resource to reconcile, or a
// removal sweep when zero.
type item struct {
	resource schema.GroupVersionResource
	name     cache.ObjectName
}

// Reconcile makes the Tournament of one Manifest match it, and reports in
// the Manifest's status whether it does. A Manifest that can't be applied is
// reported rather than retried. Manifests outside the watched namespaces are
// ignored.
func (c *Controller) Reconcile(ctx context.Context, name cache.ObjectName) error {
	if !slices.Contains(c.namespaces, name.Namespace) {
		return nil
	}
	u, err := c.client.Resource(Resource).Namespace(name.Namespace).Get(ctx, name.Name, metav1.GetOptions{})
	if apierrors.IsNotFound(err) {
		return nil
	} else if err != nil {
		return fmt.Errorf("get manifest %v: %w", name, err)
	}
	var spec Spec
	if err := convert(u.Object["spec"], &spec); err != nil {
		return fmt.Errorf("read manifest %v: %w", name, err)
	}
	manifest := "tournament/" + name.String()
	tv, change, err := c.declare(ctx, manifest, spec)
	var p *problem.Error
	reason, refused := "", false
	if errors.As(err, &p) {
		reason, refused = notSynced[p.Code]
	}
	if err != nil && !refused {
		return fmt.Errorf("declare the tournament of manifest %v: %w", name, err)
	}
	if refused {
		// The status still points to the Tournament, if the Manifest declared one.
		tv, err = tournaments.FindDeclared(ctx, c.svc.Queries, manifest)
		if err != nil && !errors.Is(err, tournament.ErrTournamentNotFound) {
			return fmt.Errorf("find the tournament of manifest %v: %w", name, err)
		}
	}

	var status, want Status
	if err := convert(u.Object["status"], &status); err != nil {
		return fmt.Errorf("read status of manifest %v: %w", name, err)
	}
	if err := convert(status, &want); err != nil {
		return err
	}
	want.TournamentID, want.TournamentStatus = "", ""
	if !tv.ID.IsZero() {
		want.TournamentID, want.TournamentStatus = tv.ID.String(), tv.Status
	}
	cond := metav1.Condition{Type: ConditionSynced, ObservedGeneration: u.GetGeneration(), LastTransitionTime: metav1.NewTime(c.svc.Clock.Now())}
	prev := meta.FindStatusCondition(want.Conditions, ConditionSynced)
	switch {
	case refused:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionFalse, reason, describe(p, "spec.")
	case change == tournaments.Unchanged && prev != nil && prev.Status == metav1.ConditionTrue:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, prev.Reason, prev.Message
	default:
		cond.Status, cond.Reason, cond.Message = metav1.ConditionTrue, synced[change].reason, synced[change].message
	}
	if cond.Status == metav1.ConditionTrue {
		want.ObservedGeneration = u.GetGeneration()
	}
	meta.SetStatusCondition(&want.Conditions, cond)
	// Writing only on change keeps the status update's own watch event from
	// triggering another write. But a Tournament that was just changed always
	// writes: if another replica applied a later generation meanwhile, the
	// write conflicts on resourceVersion, and the retry reapplies the latest.
	if equality.Semantic.DeepEqual(status, want) && change == tournaments.Unchanged {
		return nil
	}
	var object map[string]any
	if err := convert(want, &object); err != nil {
		return err
	}
	u.Object["status"] = object
	if _, err := c.client.Resource(Resource).Namespace(name.Namespace).UpdateStatus(ctx, u, metav1.UpdateOptions{}); err != nil {
		return fmt.Errorf("update status of manifest %v: %w", name, err)
	}
	return nil
}

// declare makes the Tournament of a Manifest match its spec.
func (c *Controller) declare(ctx context.Context, manifest string, spec Spec) (tournaments.TournamentView, tournaments.Change, error) {
	organizer, err := auth.ParsePrincipal(spec.Organizer)
	if err != nil {
		return tournaments.TournamentView{}, tournaments.Unchanged,
			problem.Fields{{Location: "body.organizer", Message: "must be user:<subject> or client:<client id>", Value: spec.Organizer}}.Err()
	}
	return tournaments.Declare(ctx, c.svc, manifest, organizer, spec.NewTournament)
}

// describe says why a Manifest can't be applied. It lists every field
// message, located under prefix in the Manifest, such as "spec.", rather than
// in the API's request body.
func describe(p *problem.Error, prefix string) string {
	if len(p.Fields) == 0 {
		return p.Message
	}
	msgs := make([]string, len(p.Fields))
	for i, f := range p.Fields {
		location := f.Location
		if field, ok := strings.CutPrefix(location, "body."); ok {
			location = prefix + field
		}
		msgs[i] = location + ": " + f.Message
	}
	return "The Manifest is invalid: " + strings.Join(msgs, "; ")
}

// convert copies a value between its unstructured and typed forms, through
// the JSON both of them define.
func convert(from, to any) error {
	b, err := json.Marshal(from)
	if err != nil {
		return err
	}
	return json.Unmarshal(b, to)
}
