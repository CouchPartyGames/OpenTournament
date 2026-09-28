// Package ids defines distinct identifier types over version-7 UUIDs, so that
// a MatchID can never be passed where a TournamentID is expected.
package ids

import (
	"fmt"

	"github.com/danielgtaylor/huma/v2"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5/pgtype"
)

type (
	tournament  struct{}
	stage       struct{}
	group       struct{}
	match       struct{}
	participant struct{}
	allocation  struct{}
)

// ID is a version-7 UUID tagged with the kind of thing it identifies.
type ID[T any] [16]byte

type (
	TournamentID  = ID[tournament]
	StageID       = ID[stage]
	GroupID       = ID[group]
	MatchID       = ID[match]
	ParticipantID = ID[participant]
	AllocationID  = ID[allocation]
)

// New returns a new version-7 ID, e.g. ids.New[ids.MatchID]().
func New[I ~[16]byte]() I { return I(uuid.Must(uuid.NewV7())) }

// Parse parses the canonical text form of an ID, e.g. ids.Parse[ids.MatchID](s).
func Parse[I ~[16]byte](s string) (I, error) {
	u, err := uuid.Parse(s)
	if err != nil {
		var zero I
		return zero, fmt.Errorf("invalid id %q", s)
	}
	return I(u), nil
}

// UUIDs converts IDs for queries that take uuid arrays.
func UUIDs[I ~[16]byte](in []I) []uuid.UUID {
	out := make([]uuid.UUID, len(in))
	for i, id := range in {
		out[i] = uuid.UUID(id)
	}
	return out
}

func (id ID[T]) String() string { return uuid.UUID(id).String() }

// IsZero reports whether the ID is unset.
func (id ID[T]) IsZero() bool { return id == ID[T]{} }

func (id ID[T]) MarshalText() ([]byte, error) { return []byte(id.String()), nil }

func (id *ID[T]) UnmarshalText(b []byte) error {
	parsed, err := Parse[ID[T]](string(b))
	if err != nil {
		return err
	}
	*id = parsed
	return nil
}

// ScanUUID implements pgtype.UUIDScanner.
func (id *ID[T]) ScanUUID(v pgtype.UUID) error {
	if !v.Valid {
		*id = ID[T]{}
		return nil
	}
	*id = ID[T](v.Bytes)
	return nil
}

// UUIDValue implements pgtype.UUIDValuer. A zero ID is stored as NULL.
func (id ID[T]) UUIDValue() (pgtype.UUID, error) {
	return pgtype.UUID{Bytes: id, Valid: !id.IsZero()}, nil
}

// Schema describes IDs as UUID strings in the OpenAPI document.
func (ID[T]) Schema(huma.Registry) *huma.Schema {
	return &huma.Schema{Type: huma.TypeString, Format: "uuid"}
}
