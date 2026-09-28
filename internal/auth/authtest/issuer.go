// Package authtest is a test JWT issuer standing in for Keycloak: it serves
// an OIDC discovery document and JWKS, and signs tokens.
package authtest

import (
	"crypto/rand"
	"crypto/rsa"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"time"

	"github.com/go-jose/go-jose/v4"
	"github.com/golang-jwt/jwt/v5"
)

// Issuer is a running test issuer.
type Issuer struct {
	URL    string
	key    *rsa.PrivateKey
	server *httptest.Server
}

// Start starts an issuer. Close it when done.
func Start() *Issuer {
	key, err := rsa.GenerateKey(rand.Reader, 2048)
	if err != nil {
		panic(err)
	}
	iss := &Issuer{key: key}
	mux := http.NewServeMux()
	mux.HandleFunc("/.well-known/openid-configuration", func(w http.ResponseWriter, _ *http.Request) {
		json.NewEncoder(w).Encode(map[string]any{
			"issuer":                                iss.URL,
			"jwks_uri":                              iss.URL + "/jwks",
			"authorization_endpoint":                iss.URL + "/auth",
			"token_endpoint":                        iss.URL + "/token",
			"id_token_signing_alg_values_supported": []string{"RS256"},
		})
	})
	mux.HandleFunc("/jwks", func(w http.ResponseWriter, _ *http.Request) {
		json.NewEncoder(w).Encode(jose.JSONWebKeySet{Keys: []jose.JSONWebKey{
			{Key: &key.PublicKey, KeyID: "test", Algorithm: "RS256", Use: "sig"},
		}})
	})
	iss.server = httptest.NewServer(mux)
	iss.URL = iss.server.URL
	return iss
}

// Close stops the issuer.
func (i *Issuer) Close() { i.server.Close() }

// Token signs a token with the given claims, valid for an hour.
func (i *Issuer) Token(claims map[string]any) string {
	c := jwt.MapClaims{
		"iss": i.URL,
		"aud": "account",
		"iat": time.Now().Unix(),
		"exp": time.Now().Add(time.Hour).Unix(),
	}
	for k, v := range claims {
		c[k] = v
	}
	tok := jwt.NewWithClaims(jwt.SigningMethodRS256, c)
	tok.Header["kid"] = "test"
	s, err := tok.SignedString(i.key)
	if err != nil {
		panic(err)
	}
	return s
}

// User returns a token for a human Keycloak user.
func (i *Issuer) User(subject string, extra ...map[string]any) string {
	claims := map[string]any{"sub": subject, "azp": "frontend"}
	for _, e := range extra {
		for k, v := range e {
			claims[k] = v
		}
	}
	return i.Token(claims)
}

// Client returns a service-account token for a Keycloak client.
func (i *Issuer) Client(clientID string) string {
	return i.Token(map[string]any{"sub": "service-account-" + clientID, "azp": clientID, "client_id": clientID})
}
