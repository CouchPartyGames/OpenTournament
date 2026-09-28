// Package games is the Game catalog, loaded from configuration.
package games

import (
	"fmt"
	"os"
	"slices"

	"sigs.k8s.io/yaml"
)

// KeycloakIdentity is the Player Identity kind of a Keycloak user; its value
// is the user's subject.
const KeycloakIdentity = "keycloak"

// Fleet is the Agones Fleet a Game's Game Servers are allocated from.
type Fleet struct {
	Name      string `json:"name"`
	Namespace string `json:"namespace"`
}

// Game is a video game Tournaments can be played in.
type Game struct {
	ID               string   `json:"id"`
	Name             string   `json:"name"`
	IdentityKinds    []string `json:"identityKinds"`
	MaximumMatchSize int      `json:"maximumMatchSize"`
	Fleet            Fleet    `json:"fleet"`
	// TrustedClients are the Keycloak client IDs allowed to act for the Game.
	TrustedClients []string `json:"trustedClients"`
}

// Accepts reports whether Participants may register with this identity kind.
func (g Game) Accepts(kind string) bool { return slices.Contains(g.IdentityKinds, kind) }

// Trusts reports whether a Keycloak client may act for the Game.
func (g Game) Trusts(clientID string) bool {
	return clientID != "" && slices.Contains(g.TrustedClients, clientID)
}

// Config is the catalog file.
type Config struct {
	Games []Game `json:"games"`
	// IdentityClaims maps a Player Identity kind to the Keycloak token claim
	// that carries a user's linked identity of that kind, e.g. steam: steam_id.
	IdentityClaims map[string]string `json:"identityClaims"`
}

// Catalog looks Games up by ID.
type Catalog struct {
	games          map[string]Game
	IdentityClaims map[string]string
}

// New builds a catalog, checking every Game is usable.
func New(cfg Config) (*Catalog, error) {
	c := &Catalog{games: map[string]Game{}, IdentityClaims: cfg.IdentityClaims}
	for _, g := range cfg.Games {
		switch {
		case g.ID == "":
			return nil, fmt.Errorf("a game has no id")
		case c.games[g.ID].ID != "":
			return nil, fmt.Errorf("game %q is defined twice", g.ID)
		case len(g.IdentityKinds) == 0:
			return nil, fmt.Errorf("game %q accepts no identity kinds", g.ID)
		case g.MaximumMatchSize < 2:
			return nil, fmt.Errorf("game %q: maximumMatchSize must be at least 2", g.ID)
		case g.Fleet.Name == "":
			return nil, fmt.Errorf("game %q has no fleet", g.ID)
		}
		if g.Fleet.Namespace == "" {
			g.Fleet.Namespace = "default"
		}
		c.games[g.ID] = g
	}
	return c, nil
}

// Load reads a catalog from a YAML or JSON file.
func Load(path string) (*Catalog, error) {
	b, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read game catalog: %w", err)
	}
	var cfg Config
	if err := yaml.UnmarshalStrict(b, &cfg); err != nil {
		return nil, fmt.Errorf("parse game catalog %s: %w", path, err)
	}
	return New(cfg)
}

// Get returns a Game by ID.
func (c *Catalog) Get(id string) (Game, bool) {
	g, ok := c.games[id]
	return g, ok
}

// Namespaces lists every namespace a Game's Fleet lives in.
func (c *Catalog) Namespaces() []string {
	seen := map[string]bool{}
	var out []string
	for _, g := range c.games {
		if !seen[g.Fleet.Namespace] {
			seen[g.Fleet.Namespace] = true
			out = append(out, g.Fleet.Namespace)
		}
	}
	return out
}
