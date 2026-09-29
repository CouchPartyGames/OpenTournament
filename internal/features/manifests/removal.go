package manifests

import (
	"context"
	"errors"
	"fmt"

	"github.com/couchpartygames/opentournament/internal/db"
	"github.com/couchpartygames/opentournament/internal/lifecycle"
	"github.com/couchpartygames/opentournament/internal/tournament"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
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
		err := c.svc.InTournament(ctx, t.ID, func(tx *tournament.Tx) error {
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
