package manifests

import (
	"context"
	"errors"
	"fmt"
	"strings"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/tournament"
	apierrors "k8s.io/apimachinery/pkg/api/errors"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/client-go/tools/cache"
)

// sweep runs only after every watch has synced. Read the declared Tournaments
// before listing Manifests so a concurrent declaration cannot look removed
// just because it happened after the list. A failed list aborts the whole sweep.
func (c *Controller) sweep(ctx context.Context) error {
	declared, err := c.svc.Queries.ListDeclaredTournaments(ctx, c.namespaces)
	if err != nil {
		return fmt.Errorf("list declared tournaments: %w", err)
	}
	present := map[string]bool{}
	for _, ns := range c.namespaces {
		list, err := c.client.Resource(Resource).Namespace(ns).List(ctx, metav1.ListOptions{})
		if err != nil {
			return fmt.Errorf("list manifests in namespace %s for removal: %w", ns, err)
		}
		for _, u := range list.Items {
			present["tournament/"+ns+"/"+u.GetName()] = true
		}
	}
	var removed []db.Tournament
	for _, t := range declared {
		unstarted := t.Status == lifecycle.Draft || t.Status == lifecycle.RegistrationOpen || t.Status == lifecycle.CheckIn
		if unstarted && !present[*t.Manifest] {
			removed = append(removed, t)
		}
	}
	if len(removed) > len(declared)/2 {
		c.Logger.ErrorContext(ctx, "skipping manifest removal sweep: more than half of declared Tournaments would be removed",
			"removals", len(removed), "declared", len(declared), "namespaces", c.namespaces)
		return nil
	}
	for _, t := range removed {
		name, err := cache.ParseObjectName(strings.TrimPrefix(*t.Manifest, "tournament/"))
		if err != nil {
			return fmt.Errorf("parse manifest %s: %w", *t.Manifest, err)
		}
		err = c.svc.InTournament(ctx, t.ID, func(tx *tournament.Tx) error {
			// A Manifest may have been reapplied while another namespace was
			// listed or while we waited for the Tournament lock. Only a fresh
			// NotFound authorizes removal; any other read failure stops it.
			_, err := c.client.Resource(Resource).Namespace(name.Namespace).Get(ctx, name.Name, metav1.GetOptions{})
			if err == nil {
				return nil
			}
			if !apierrors.IsNotFound(err) {
				return fmt.Errorf("check removed manifest %v: %w", name, err)
			}
			// The scheduler or another replica may have changed the Tournament
			// since the sweep read it. Never remove a Tournament that started.
			switch tx.T.Status {
			case lifecycle.Draft:
				return tx.Q.DeleteTournament(ctx, tx.T.ID)
			case lifecycle.RegistrationOpen, lifecycle.CheckIn:
				return tx.Cancel()
			}
			return nil
		})
		if err != nil && !errors.Is(err, tournament.ErrTournamentNotFound) {
			return fmt.Errorf("remove tournament %s after manifest removal: %w", t.ID, err)
		}
	}
	return nil
}
