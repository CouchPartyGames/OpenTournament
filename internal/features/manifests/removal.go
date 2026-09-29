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
	"k8s.io/apimachinery/pkg/apis/meta/v1/unstructured"
	"k8s.io/apimachinery/pkg/runtime/schema"
	"k8s.io/client-go/tools/cache"
)

// sweep runs only after every Tournament Manifest watch has synced. It
// removes the Tournaments of Occurrences too when recurringSynced says the
// Recurring Tournament watches have, and while their CRD is served. Read the
// declared Tournaments before listing their sources so a concurrent
// declaration cannot look removed just because it happened after the list. A
// failed list aborts the whole sweep.
func (c *Controller) sweep(ctx context.Context, recurringSynced bool) error {
	declared, err := c.svc.Queries.ListDeclaredTournaments(ctx, c.namespaces)
	if err != nil {
		return fmt.Errorf("list declared tournaments: %w", err)
	}
	present := map[key]bool{}
	sweepOccurrences := recurringSynced
	for _, ns := range c.namespaces {
		for _, resource := range []schema.GroupVersionResource{Resource, RecurringResource} {
			if resource == RecurringResource && !sweepOccurrences {
				continue
			}
			list, err := c.client.Resource(resource).Namespace(ns).List(ctx, metav1.ListOptions{})
			if resource == RecurringResource && apierrors.IsNotFound(err) {
				// The CRD was removed after the watches synced.
				c.Logger.WarnContext(ctx, "recurring tournaments can't be listed; the removal sweep leaves their Occurrences alone", "error", err)
				sweepOccurrences = false
				continue
			}
			if err != nil {
				return fmt.Errorf("list %s in namespace %s for removal: %w", resource.Resource, ns, err)
			}
			for _, u := range list.Items {
				present[key{resource, cache.ObjectName{Namespace: ns, Name: u.GetName()}}] = true
			}
		}
	}
	type removal struct {
		t      db.Tournament
		source key
	}
	var considered int
	var removed []removal
	for _, t := range declared {
		source, err := sourceOf(*t.Manifest)
		if err != nil {
			return err
		}
		if source.resource == RecurringResource && !sweepOccurrences {
			continue
		}
		considered++
		if unstarted(t.Status) && !present[source] {
			removed = append(removed, removal{t, source})
		}
	}
	if len(removed) > considered/2 {
		c.Logger.ErrorContext(ctx, "skipping manifest removal sweep: more than half of declared Tournaments would be removed",
			"removals", len(removed), "declared", considered, "namespaces", c.namespaces)
		return nil
	}
	for _, r := range removed {
		err = c.svc.InTournament(ctx, r.t.ID, func(tx *tournament.Tx) error {
			// The Manifest or Recurring Tournament may have been reapplied
			// while another namespace was listed or while we waited for the
			// Tournament lock. Only a fresh NotFound authorizes removal; any
			// other read failure stops it.
			_, err := c.get(ctx, r.source)
			if err == nil {
				return nil
			}
			if !apierrors.IsNotFound(err) {
				return fmt.Errorf("check removed %s %v: %w", r.source.resource.Resource, r.source.name, err)
			}
			return remove(tx)
		})
		if err != nil && !errors.Is(err, tournament.ErrTournamentNotFound) {
			return fmt.Errorf("remove tournament %s after %s %v was deleted: %w", r.t.ID, r.source.resource.Resource, r.source.name, err)
		}
	}
	return nil
}

// get reads the object k names from the cluster.
func (c *Controller) get(ctx context.Context, k key) (*unstructured.Unstructured, error) {
	return c.client.Resource(k.resource).Namespace(k.name.Namespace).Get(ctx, k.name.Name, metav1.GetOptions{})
}

// sourceOf returns the object that declared a Tournament, from the manifest
// the Tournament records: tournament/<namespace>/<name> for a Tournament
// Manifest, or recurring/<namespace>/<name>/<start> for an Occurrence of a
// Recurring Tournament.
func sourceOf(manifest string) (key, error) {
	parts := strings.Split(manifest, "/")
	switch {
	case len(parts) == 3 && parts[0] == "tournament":
		return key{Resource, cache.ObjectName{Namespace: parts[1], Name: parts[2]}}, nil
	case len(parts) == 4 && parts[0] == "recurring":
		return key{RecurringResource, cache.ObjectName{Namespace: parts[1], Name: parts[2]}}, nil
	}
	return key{}, fmt.Errorf("parse manifest %s: not tournament/<namespace>/<name> or recurring/<namespace>/<name>/<start>", manifest)
}

// unstarted reports whether a Tournament in status s can still be removed.
func unstarted(s lifecycle.TournamentStatus) bool {
	return s == lifecycle.Draft || s == lifecycle.RegistrationOpen || s == lifecycle.CheckIn
}

// remove applies the removal policy to a declared Tournament its manifest no
// longer wants. A Draft is deleted, since nobody can have registered yet. One
// in Registration Open or Check-in is cancelled, which releases its Game
// Servers and announces it live. One that started or finished is left alone,
// so a change in git never stops a Tournament in play. The status is read
// under the lock, as the scheduler or another replica may have moved it on.
func remove(tx *tournament.Tx) error {
	switch tx.T.Status {
	case lifecycle.Draft:
		return tx.Q.DeleteTournament(tx.Ctx(), tx.T.ID)
	case lifecycle.RegistrationOpen, lifecycle.CheckIn:
		return tx.Cancel()
	}
	return nil
}
