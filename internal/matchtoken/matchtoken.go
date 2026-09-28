// Package matchtoken issues and verifies the per-Match tokens Game Servers
// authenticate with (ADR-0002). A token is scoped to one Match and one
// allocation, and expires at the Match's Result Deadline plus a grace period.
package matchtoken

import (
	"errors"
	"fmt"
	"time"

	"github.com/couchpartygames/opentournament/internal/clock"
	"github.com/couchpartygames/opentournament/internal/ids"
	"github.com/golang-jwt/jwt/v5"
)

const (
	issuer   = "opentournament"
	audience = "game-server"
)

// Grace is added to the Result Deadline to get the token expiry.
const Grace = 2 * time.Minute

// Claims identify what a token is good for.
type Claims struct {
	MatchID      ids.MatchID
	AllocationID ids.AllocationID
}

type jwtClaims struct {
	Allocation string `json:"alc"`
	jwt.RegisteredClaims
}

// Issuer signs and verifies Match tokens with a key held by the service.
type Issuer struct {
	key   []byte
	clock clock.Clock
}

// NewIssuer returns an Issuer. The key must be at least 32 bytes.
func NewIssuer(key []byte, c clock.Clock) (*Issuer, error) {
	if len(key) < 32 {
		return nil, fmt.Errorf("match token key must be at least 32 bytes, got %d", len(key))
	}
	return &Issuer{key: key, clock: c}, nil
}

// Issue returns a token for one allocation of a Match.
func (i *Issuer) Issue(c Claims, resultDeadline time.Time) (string, error) {
	now := i.clock.Now()
	return jwt.NewWithClaims(jwt.SigningMethodHS256, jwtClaims{
		Allocation: c.AllocationID.String(),
		RegisteredClaims: jwt.RegisteredClaims{
			Issuer:    issuer,
			Audience:  jwt.ClaimStrings{audience},
			Subject:   c.MatchID.String(),
			IssuedAt:  jwt.NewNumericDate(now),
			ExpiresAt: jwt.NewNumericDate(resultDeadline.Add(Grace)),
		},
	}).SignedString(i.key)
}

// ErrInvalid is returned for any token that isn't valid now.
var ErrInvalid = errors.New("invalid match token")

// Verify checks a token's signature and expiry and returns what it's for.
func (i *Issuer) Verify(raw string) (Claims, error) {
	var claims jwtClaims
	_, err := jwt.ParseWithClaims(raw, &claims, func(*jwt.Token) (any, error) { return i.key, nil },
		jwt.WithValidMethods([]string{jwt.SigningMethodHS256.Alg()}),
		jwt.WithIssuer(issuer), jwt.WithAudience(audience),
		jwt.WithExpirationRequired(),
		jwt.WithTimeFunc(i.clock.Now))
	if err != nil {
		return Claims{}, ErrInvalid
	}
	matchID, err := ids.Parse[ids.MatchID](claims.Subject)
	if err != nil {
		return Claims{}, ErrInvalid
	}
	allocationID, err := ids.Parse[ids.AllocationID](claims.Allocation)
	if err != nil {
		return Claims{}, ErrInvalid
	}
	return Claims{MatchID: matchID, AllocationID: allocationID}, nil
}
