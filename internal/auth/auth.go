// Package auth verifies Keycloak-issued bearer tokens and describes who is
// calling.
package auth

import (
	"context"
	"fmt"
	"net/http"
	"strings"

	"github.com/coreos/go-oidc/v3/oidc"
	"github.com/couchpartygames/opentournament/internal/games"
	"github.com/couchpartygames/opentournament/internal/problem"
)

// Identity is a Player Identity: a kind (keycloak, steam, ...) and a value.
type Identity struct {
	Kind  string `json:"kind" minLength:"1" maxLength:"64" doc:"The Player Identity kind, e.g. keycloak or steam"`
	Value string `json:"value" minLength:"1" maxLength:"256" doc:"The identity's value, e.g. a Steam ID"`
}

// Key is how an identity appears in private event recipient lists.
func (i Identity) Key() string { return "identity:" + i.Kind + ":" + i.Value }

// Principal is an authenticated caller: a human user, or a Game backend
// using a Keycloak service client.
type Principal struct {
	Subject string
	// ClientID is set when the caller is a service client (client credentials).
	ClientID string
	// Identities are the Player Identities the caller owns: their Keycloak
	// identity and any linked identities exposed as claims.
	Identities []Identity
}

// ID is how the Principal is stored as an Organizer.
func (p Principal) ID() string {
	if p.ClientID != "" {
		return "client:" + p.ClientID
	}
	return "user:" + p.Subject
}

// ParsePrincipal reads a Principal from its ID: user:<keycloak subject> or
// client:<keycloak client id>. The Principal has no Player Identities.
func ParsePrincipal(id string) (Principal, error) {
	kind, value, _ := strings.Cut(id, ":")
	if value != "" {
		switch kind {
		case "user":
			return Principal{Subject: value}, nil
		case "client":
			return Principal{ClientID: value}, nil
		}
	}
	return Principal{}, fmt.Errorf("principal %q is neither user:<subject> nor client:<client id>", id)
}

// IsService reports whether the caller is a service client rather than a human.
func (p Principal) IsService() bool { return p.ClientID != "" }

// Owns reports whether the Principal holds this Player Identity.
func (p Principal) Owns(id Identity) bool {
	for _, own := range p.Identities {
		if own == id {
			return true
		}
	}
	return false
}

// Keys are the recipient keys the Principal may read private events for.
func (p Principal) Keys() []string {
	keys := []string{p.ID()}
	for _, id := range p.Identities {
		keys = append(keys, id.Key())
	}
	return keys
}

// Security is the OpenAPI security requirement of operations that take a
// Keycloak bearer token.
func Security() []map[string][]string { return []map[string][]string{{"keycloak": {}}} }

// Verifier turns a bearer token into a Principal.
type Verifier interface {
	Verify(ctx context.Context, token string) (Principal, error)
}

// OIDC verifies tokens against an issuer's discovery document and JWKS.
type OIDC struct {
	verifier       *oidc.IDTokenVerifier
	identityClaims map[string]string
}

// NewOIDC discovers the issuer. Access tokens are checked for signature,
// issuer and expiry; the audience is not checked, since Keycloak access
// tokens are issued for many clients.
func NewOIDC(ctx context.Context, issuer string, identityClaims map[string]string) (*OIDC, error) {
	provider, err := oidc.NewProvider(ctx, issuer)
	if err != nil {
		return nil, fmt.Errorf("oidc discovery for %s: %w", issuer, err)
	}
	return &OIDC{
		verifier:       provider.Verifier(&oidc.Config{SkipClientIDCheck: true}),
		identityClaims: identityClaims,
	}, nil
}

// Verify checks a Keycloak access token. Service-account tokens become a
// Game backend Principal; others a human with their Player Identities.
func (o *OIDC) Verify(ctx context.Context, raw string) (Principal, error) {
	tok, err := o.verifier.Verify(ctx, raw)
	if err != nil {
		return Principal{}, err
	}
	var claims map[string]any
	if err := tok.Claims(&claims); err != nil {
		return Principal{}, err
	}
	p := Principal{Subject: tok.Subject}
	// Keycloak puts client_id (clientId in older versions) on service-account tokens.
	for _, c := range []string{"client_id", "clientId"} {
		if s, ok := claims[c].(string); ok && s != "" {
			p.ClientID = s
		}
	}
	if !p.IsService() {
		p.Identities = append(p.Identities, Identity{Kind: games.KeycloakIdentity, Value: tok.Subject})
		for kind, claim := range o.identityClaims {
			if s, ok := claims[claim].(string); ok && s != "" {
				p.Identities = append(p.Identities, Identity{Kind: kind, Value: s})
			}
		}
	}
	return p, nil
}

type ctxKey struct{}

type result struct {
	principal Principal
	err       error
	present   bool
}

// Middleware verifies the bearer token of every request that carries one,
// except on paths where tokens are something else (Match tokens).
func Middleware(v Verifier, skipPrefix string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			if skipPrefix != "" && strings.HasPrefix(r.URL.Path, skipPrefix) {
				next.ServeHTTP(w, r)
				return
			}
			if token, ok := BearerToken(r.Header.Get("Authorization")); ok {
				p, err := v.Verify(r.Context(), token)
				r = r.WithContext(context.WithValue(r.Context(), ctxKey{}, result{principal: p, err: err, present: true}))
			}
			next.ServeHTTP(w, r)
		})
	}
}

// BearerToken extracts the token of an Authorization header.
func BearerToken(header string) (string, bool) {
	scheme, token, ok := strings.Cut(header, " ")
	if !ok || !strings.EqualFold(scheme, "Bearer") || token == "" {
		return "", false
	}
	return token, true
}

var errInvalidToken = problem.New(problem.Unauthenticated, problem.CodeUnauthenticated, "the bearer token is invalid or expired")

// Optional returns the caller, if any. A token that is present but invalid
// is still an error.
func Optional(ctx context.Context) (*Principal, error) {
	r, _ := ctx.Value(ctxKey{}).(result)
	if !r.present {
		return nil, nil
	}
	if r.err != nil {
		return nil, errInvalidToken
	}
	return &r.principal, nil
}

// Required returns the caller, or an error when the request is anonymous.
func Required(ctx context.Context) (Principal, error) {
	p, err := Optional(ctx)
	if err != nil {
		return Principal{}, err
	}
	if p == nil {
		return Principal{}, problem.New(problem.Unauthenticated, problem.CodeUnauthenticated, "this operation requires a bearer token")
	}
	return *p, nil
}

// WithPrincipal stores a verified Principal in a context, for callers that
// authenticate outside the HTTP middleware (the live-update socket).
func WithPrincipal(ctx context.Context, p Principal) context.Context {
	return context.WithValue(ctx, ctxKey{}, result{principal: p, present: true})
}
