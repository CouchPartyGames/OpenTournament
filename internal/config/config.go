// Package config reads the service configuration from the environment.
package config

import (
	"encoding/base64"
	"errors"
	"fmt"
	"os"
	"slices"
	"strconv"
	"strings"
	"time"
)

// Config is the service configuration.
type Config struct {
	// HTTPAddr is where the API listens (OT_HTTP_ADDR, default :8080).
	HTTPAddr string
	// DatabaseURL is the PostgreSQL connection string (OT_DATABASE_URL).
	DatabaseURL string
	// OIDCIssuer is the Keycloak realm URL (OT_OIDC_ISSUER).
	OIDCIssuer string
	// GamesFile is the Game catalog (OT_GAMES_FILE).
	GamesFile string
	// MatchTokenKey signs Match tokens; every replica needs the same one
	// (OT_MATCH_TOKEN_KEY, base64, at least 32 bytes).
	MatchTokenKey []byte
	// GameServers selects the Game Server port: agones, or fake for local
	// development without a cluster (OT_GAME_SERVERS, default agones).
	GameServers string
	// Kubeconfig is used outside a cluster (OT_KUBECONFIG); in-cluster
	// configuration is used when empty.
	Kubeconfig string
	// ManifestNamespaces are the namespaces whose Tournament Manifests are
	// declared (OT_MANIFEST_NAMESPACES, comma separated). Empty turns
	// declared Tournaments off.
	ManifestNamespaces []string
	// Docs serves Scalar API docs at /api/v1/docs (OT_DOCS).
	Docs bool
	// EventRetention is how long live-update events are kept (OT_EVENT_RETENTION, default 1h).
	EventRetention time.Duration
	// ShutdownTimeout bounds graceful shutdown (OT_SHUTDOWN_TIMEOUT, default 30s).
	ShutdownTimeout time.Duration
}

// FromEnv reads the configuration, reporting every problem at once.
func FromEnv() (Config, error) {
	var errs []error
	required := func(name string) string {
		v := os.Getenv(name)
		if v == "" {
			errs = append(errs, fmt.Errorf("%s is required", name))
		}
		return v
	}
	duration := func(name string, def time.Duration) time.Duration {
		v := os.Getenv(name)
		if v == "" {
			return def
		}
		d, err := time.ParseDuration(v)
		if err != nil {
			errs = append(errs, fmt.Errorf("%s: %w", name, err))
		}
		return d
	}
	c := Config{
		HTTPAddr:        os.Getenv("OT_HTTP_ADDR"),
		DatabaseURL:     required("OT_DATABASE_URL"),
		OIDCIssuer:      required("OT_OIDC_ISSUER"),
		GamesFile:       required("OT_GAMES_FILE"),
		GameServers:     os.Getenv("OT_GAME_SERVERS"),
		Kubeconfig:      os.Getenv("OT_KUBECONFIG"),
		EventRetention:  duration("OT_EVENT_RETENTION", time.Hour),
		ShutdownTimeout: duration("OT_SHUTDOWN_TIMEOUT", 30*time.Second),
	}
	if c.HTTPAddr == "" {
		c.HTTPAddr = ":8080"
	}
	if c.GameServers == "" {
		c.GameServers = "agones"
	}
	if c.GameServers != "agones" && c.GameServers != "fake" {
		errs = append(errs, fmt.Errorf("OT_GAME_SERVERS must be agones or fake, got %q", c.GameServers))
	}
	for _, ns := range strings.Split(os.Getenv("OT_MANIFEST_NAMESPACES"), ",") {
		if ns = strings.TrimSpace(ns); ns != "" && !slices.Contains(c.ManifestNamespaces, ns) {
			c.ManifestNamespaces = append(c.ManifestNamespaces, ns)
		}
	}
	if v := os.Getenv("OT_DOCS"); v != "" {
		b, err := strconv.ParseBool(v)
		if err != nil {
			errs = append(errs, fmt.Errorf("OT_DOCS: %w", err))
		}
		c.Docs = b
	}
	if key := required("OT_MATCH_TOKEN_KEY"); key != "" {
		b, err := base64.StdEncoding.DecodeString(key)
		switch {
		case err != nil:
			errs = append(errs, fmt.Errorf("OT_MATCH_TOKEN_KEY must be base64: %w", err))
		case len(b) < 32:
			errs = append(errs, fmt.Errorf("OT_MATCH_TOKEN_KEY must decode to at least 32 bytes"))
		}
		c.MatchTokenKey = b
	}
	return c, errors.Join(errs...)
}
