// Package lifecycle names the statuses a Tournament moves through. The
// queries store them and the Tournament aggregate moves between them, so they
// live here, below both, where each can import them without a cycle.
package lifecycle

import "github.com/danielgtaylor/huma/v2"

// TournamentStatus is where a Tournament is in its lifecycle: Draft, then
// RegistrationOpen, CheckIn when Check-in is enabled, Running and Completed.
// It can be Cancelled at any point before Completed.
type TournamentStatus string

// Tournament statuses.
const (
	Draft            TournamentStatus = "draft"
	RegistrationOpen TournamentStatus = "registration-open"
	CheckIn          TournamentStatus = "check-in"
	Running          TournamentStatus = "running"
	Completed        TournamentStatus = "completed"
	Cancelled        TournamentStatus = "cancelled"
)

// Schema lists the Tournament statuses as an enum in the OpenAPI document,
// which also rejects any other status in a request.
func (TournamentStatus) Schema(huma.Registry) *huma.Schema {
	return enum(Draft, RegistrationOpen, CheckIn, Running, Completed, Cancelled)
}

// enum describes a string type that holds only the given values.
func enum[T ~string](values ...T) *huma.Schema {
	s := &huma.Schema{Type: huma.TypeString}
	for _, v := range values {
		s.Enum = append(s.Enum, string(v))
	}
	return s
}
