// Package manifests is the slice that declares Tournaments from Tournament
// Manifests: Kubernetes resources, synced from git, that hold a Tournament's
// configuration and its Organizer (ADR-0005). It watches the Manifests of the
// configured namespaces through a rate-limited workqueue, creates the
// Tournament each one declares, and reports it in the Manifest's status.
//
// Every replica runs the controller, without leader election: a Manifest
// declares at most one Tournament, and its status is a function of that
// Tournament, so replicas that reconcile the same Manifest converge.
//
// RBAC needed: get, list and watch tournaments.opentournament.io, and update
// tournaments/status, in every watched namespace.
package manifests

import (
	"context"
	"encoding/json"
	"fmt"
	"log/slog"
	"slices"
	"time"

	"github.com/couchpartygames/opentournament/internal/auth"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
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
	// ObservedGeneration is the Manifest generation the Tournament matches.
	// Edits aren't applied yet, so it stays the generation that declared it.
	ObservedGeneration int64                      `json:"observedGeneration,omitempty"`
	TournamentID       string                     `json:"tournamentId,omitempty"`
	TournamentStatus   lifecycle.TournamentStatus `json:"tournamentStatus,omitempty"`
	Conditions         []metav1.Condition         `json:"conditions,omitempty"`
}

// ConditionSynced is true when the Tournament matches its Manifest's
// observed generation.
const ConditionSynced = "Synced"

// ReasonCreated says the Tournament of a Synced Manifest was created from it.
const ReasonCreated = "Created"

// Controller declares the Tournaments of the Manifests in its namespaces.
type Controller struct {
	client     dynamic.Interface
	svc        *tournament.Service
	namespaces []string
	queue      workqueue.TypedRateLimitingInterface[cache.ObjectName]
	// Resync is the period at which every Manifest is reconciled again when
	// nothing happens, so its status follows the Tournament's. It also sets
	// the interval between removal sweeps.
	Resync time.Duration
	// Logger reports reconciliation failures and skipped removal sweeps.
	Logger *slog.Logger
}

// New returns a Controller for the Manifests in namespaces.
func New(client dynamic.Interface, svc *tournament.Service, namespaces []string) *Controller {
	return &Controller{
		client:     client,
		svc:        svc,
		namespaces: namespaces,
		queue: workqueue.NewTypedRateLimitingQueueWithConfig(
			workqueue.DefaultTypedControllerRateLimiter[cache.ObjectName](),
			workqueue.TypedRateLimitingQueueConfig[cache.ObjectName]{Name: "manifests"}),
		Resync: 30 * time.Second,
		Logger: slog.Default(),
	}
}

// Run watches the Manifests and reconciles each one that changes, until ctx
// ends. The informers list every Manifest first, so each is reconciled at
// startup. Run can only be called once.
func (c *Controller) Run(ctx context.Context) {
	go func() {
		<-ctx.Done()
		c.queue.ShutDown()
	}()
	enqueue := func(obj any) {
		name, err := cache.DeletionHandlingObjectToName(obj)
		if err != nil {
			c.Logger.ErrorContext(ctx, "unexpected object from the manifest informer", "error", err)
			return
		}
		c.queue.Add(name)
	}
	handler := cache.ResourceEventHandlerFuncs{
		AddFunc:    enqueue,
		UpdateFunc: func(_, obj any) { enqueue(obj) },
		DeleteFunc: enqueue,
	}
	var synced []cache.InformerSynced
	for _, ns := range c.namespaces {
		factory := dynamicinformer.NewFilteredDynamicSharedInformerFactory(c.client, c.Resync, ns, nil)
		informer := factory.ForResource(Resource).Informer()
		if _, err := informer.AddEventHandler(handler); err != nil {
			c.Logger.ErrorContext(ctx, "can't watch the manifests of a namespace; its Tournaments won't be declared", "namespace", ns, "error", err)
			return
		}
		synced = append(synced, informer.HasSynced)
		factory.Start(ctx.Done())
		// Shutdown waits for the informers, which stop once ctx has ended,
		// before the queue below lets Run return.
		defer factory.Shutdown()
	}
	if !cache.WaitForCacheSync(ctx.Done(), synced...) {
		return
	}
	// The empty name is a sweep, not a Manifest. Queueing it at startup and
	// periodically catches removals even when no deletion event was seen.
	sweep := cache.ObjectName{}
	c.queue.Add(sweep)
	for {
		name, shutdown := c.queue.Get()
		if shutdown {
			return
		}
		var err error
		if name == sweep {
			err = c.sweep(ctx)
			c.queue.AddAfter(sweep, c.Resync)
		} else {
			err = c.Reconcile(ctx, name)
		}
		if err != nil && ctx.Err() == nil {
			c.Logger.WarnContext(ctx, "reconcile manifest failed; retrying", "manifest", name, "error", err)
			c.queue.AddRateLimited(name)
		} else {
			c.queue.Forget(name)
		}
		c.queue.Done(name)
	}
}

// Reconcile declares the Tournament of one Manifest. Manifests outside the
// watched namespaces are ignored.
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
	organizer, err := auth.ParsePrincipal(spec.Organizer)
	if err != nil {
		return fmt.Errorf("manifest %v: %w", name, err)
	}
	tv, err := tournaments.Declare(ctx, c.svc, "tournament/"+name.String(), organizer, spec.NewTournament)
	if err != nil {
		return fmt.Errorf("declare the tournament of manifest %v: %w", name, err)
	}

	var status, want Status
	if err := convert(u.Object["status"], &status); err != nil {
		return fmt.Errorf("read status of manifest %v: %w", name, err)
	}
	if err := convert(status, &want); err != nil {
		return err
	}
	// Edits aren't applied yet, so a later generation isn't observed: the
	// Tournament still matches the generation that declared it.
	if want.TournamentID != tv.ID.String() {
		want.ObservedGeneration = u.GetGeneration()
	}
	want.TournamentID = tv.ID.String()
	want.TournamentStatus = tv.Status
	meta.SetStatusCondition(&want.Conditions, metav1.Condition{
		Type: ConditionSynced, Status: metav1.ConditionTrue, Reason: ReasonCreated,
		Message: "The Tournament is created", ObservedGeneration: want.ObservedGeneration,
		LastTransitionTime: metav1.NewTime(c.svc.Clock.Now()),
	})
	// Writing only on change keeps the status update's own watch event from
	// triggering another write.
	if equality.Semantic.DeepEqual(status, want) {
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

// convert copies a value between its unstructured and typed forms, through
// the JSON both of them define.
func convert(from, to any) error {
	b, err := json.Marshal(from)
	if err != nil {
		return err
	}
	return json.Unmarshal(b, to)
}
