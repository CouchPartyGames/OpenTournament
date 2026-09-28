// Package agones is the Game Server port's adapter for Agones (ADR-0001).
// It allocates GameServers from each Game's warm Fleet with
// GameServerAllocations, watches allocated GameServers through client-go
// informers, tells running servers about Forfeits with an annotation, and
// releases servers by deleting them.
//
// RBAC needed: create gameserverallocations; get, list, watch, patch and
// delete gameservers, in every Fleet namespace.
package agones

import (
	"context"
	"encoding/json"
	"fmt"
	"strings"
	"time"

	agonesv1 "agones.dev/agones/pkg/apis/agones/v1"
	allocationv1 "agones.dev/agones/pkg/apis/allocation/v1"
	"agones.dev/agones/pkg/client/clientset/versioned"
	"agones.dev/agones/pkg/client/informers/externalversions"
	listers "agones.dev/agones/pkg/client/listers/agones/v1"
	"github.com/couchpartygames/opentournament/internal/gameserver"
	"github.com/couchpartygames/opentournament/internal/ids"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/labels"
	"k8s.io/apimachinery/pkg/types"
	"k8s.io/client-go/rest"
	"k8s.io/client-go/tools/cache"
)

// Labels and annotations the service puts on the GameServers it allocates.
const (
	LabelManagedBy       = "app.kubernetes.io/managed-by"
	ManagedBy            = "opentournament"
	LabelMatch           = "opentournament/match-id"
	LabelAllocation      = "opentournament/allocation-id"
	AnnotationMatchToken = "opentournament/match-token"
	// AnnotationForfeited lists the Participant IDs who forfeited, comma
	// separated. Servers watch it through the Agones SDK's WatchGameServer.
	AnnotationForfeited = "opentournament/forfeited"
)

// Port talks to Agones.
type Port struct {
	client    versioned.Interface
	informers []cache.SharedIndexInformer
	listers   map[string]listers.GameServerLister
}

var _ gameserver.Port = (*Port)(nil)

// New connects to the cluster and starts informers for GameServers in the
// given namespaces, waiting until their caches have synced.
func New(ctx context.Context, cfg *rest.Config, namespaces []string) (*Port, error) {
	client, err := versioned.NewForConfig(cfg)
	if err != nil {
		return nil, fmt.Errorf("agones client: %w", err)
	}
	p := &Port{client: client, listers: map[string]listers.GameServerLister{}}
	for _, ns := range namespaces {
		if _, ok := p.listers[ns]; ok {
			continue
		}
		factory := externalversions.NewSharedInformerFactoryWithOptions(client, 10*time.Minute,
			externalversions.WithNamespace(ns),
			externalversions.WithTweakListOptions(func(o *metav1.ListOptions) {
				o.LabelSelector = LabelManagedBy + "=" + ManagedBy
			}))
		gs := factory.Agones().V1().GameServers()
		p.informers = append(p.informers, gs.Informer())
		p.listers[ns] = gs.Lister()
		factory.Start(ctx.Done())
		for typ, ok := range factory.WaitForCacheSync(ctx.Done()) {
			if !ok {
				return nil, fmt.Errorf("sync %v informer in namespace %s", typ, ns)
			}
		}
	}
	return p, nil
}

func (p *Port) Allocate(ctx context.Context, req gameserver.AllocationRequest) (gameserver.Server, error) {
	gsa := &allocationv1.GameServerAllocation{
		Spec: allocationv1.GameServerAllocationSpec{
			Selectors: []allocationv1.GameServerSelector{{
				LabelSelector: metav1.LabelSelector{MatchLabels: map[string]string{agonesv1.FleetNameLabel: req.Fleet.Name}},
			}},
			MetaPatch: allocationv1.MetaPatch{
				Labels: map[string]string{
					LabelManagedBy:  ManagedBy,
					LabelMatch:      req.MatchID.String(),
					LabelAllocation: req.AllocationID.String(),
				},
				Annotations: map[string]string{AnnotationMatchToken: req.Token},
			},
		},
	}
	out, err := p.client.AllocationV1().GameServerAllocations(req.Fleet.Namespace).Create(ctx, gsa, metav1.CreateOptions{})
	if err != nil {
		return gameserver.Server{}, fmt.Errorf("create game server allocation: %w", err)
	}
	if out.Status.State != allocationv1.GameServerAllocationAllocated {
		return gameserver.Server{}, fmt.Errorf("%w: allocation %s", gameserver.ErrNoCapacity, out.Status.State)
	}
	s := gameserver.Server{
		Name:         qualified(req.Fleet.Namespace, out.Status.GameServerName),
		Address:      out.Status.Address,
		MatchID:      req.MatchID,
		AllocationID: req.AllocationID,
		State:        gameserver.Healthy,
	}
	if len(out.Status.Ports) > 0 {
		s.Port = int(out.Status.Ports[0].Port)
	}
	return s, nil
}

func (p *Port) Servers(context.Context) ([]gameserver.Server, error) {
	var out []gameserver.Server
	for ns, l := range p.listers {
		sel := labels.SelectorFromSet(labels.Set{LabelManagedBy: ManagedBy})
		list, err := l.GameServers(ns).List(sel)
		if err != nil {
			return nil, err
		}
		for _, gs := range list {
			match, err := ids.Parse[ids.MatchID](gs.Labels[LabelMatch])
			if err != nil {
				continue
			}
			allocation, _ := ids.Parse[ids.AllocationID](gs.Labels[LabelAllocation])
			s := gameserver.Server{
				Name: qualified(ns, gs.Name), Address: gs.Status.Address,
				MatchID: match, AllocationID: allocation, State: gameserver.Healthy,
			}
			if len(gs.Status.Ports) > 0 {
				s.Port = int(gs.Status.Ports[0].Port)
			}
			if gs.IsBeingDeleted() || agonesv1.TerminalGameServerStates[gs.Status.State] {
				s.State = gameserver.Failed
			}
			out = append(out, s)
		}
	}
	return out, nil
}

func (p *Port) Watch(ctx context.Context, onChange func()) error {
	handler := cache.ResourceEventHandlerFuncs{
		AddFunc:    func(any) { onChange() },
		UpdateFunc: func(any, any) { onChange() },
		DeleteFunc: func(any) { onChange() },
	}
	for _, inf := range p.informers {
		reg, err := inf.AddEventHandler(handler)
		if err != nil {
			return err
		}
		defer inf.RemoveEventHandler(reg)
	}
	<-ctx.Done()
	return nil
}

func (p *Port) NotifyForfeits(ctx context.Context, server string, participants []ids.ParticipantID) error {
	ns, name := split(server)
	list := make([]string, len(participants))
	for i, id := range participants {
		list[i] = id.String()
	}
	patch, err := json.Marshal(map[string]any{
		"metadata": map[string]any{"annotations": map[string]string{AnnotationForfeited: strings.Join(list, ",")}},
	})
	if err != nil {
		return err
	}
	_, err = p.client.AgonesV1().GameServers(ns).Patch(ctx, name, types.MergePatchType, patch, metav1.PatchOptions{})
	return err
}

func (p *Port) Release(ctx context.Context, server string) error {
	ns, name := split(server)
	err := p.client.AgonesV1().GameServers(ns).Delete(ctx, name, metav1.DeleteOptions{})
	if apierrors.IsNotFound(err) {
		return nil
	}
	return err
}

// Server names are namespace-qualified, since Fleets can live in different
// namespaces.
func qualified(ns, name string) string { return ns + "/" + name }

func split(server string) (string, string) {
	ns, name, _ := strings.Cut(server, "/")
	return ns, name
}
