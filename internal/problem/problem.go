// Package problem defines the typed domain errors of the service and maps
// every error to a Problem Details (RFC 9457) response.
package problem

import (
	"context"
	"errors"
	"fmt"
	"log/slog"
	"net/http"

	"github.com/danielgtaylor/huma/v2"
)

// Kind is the class of an expected failure.
type Kind int

const (
	Invalid Kind = iota + 1
	NotFound
	Conflict
	Unauthenticated
	Forbidden
)

var statusOf = map[Kind]int{
	Invalid:         http.StatusUnprocessableEntity,
	NotFound:        http.StatusNotFound,
	Conflict:        http.StatusConflict,
	Unauthenticated: http.StatusUnauthorized,
	Forbidden:       http.StatusForbidden,
}

// FieldError is one invalid input field.
type FieldError struct {
	Location string `json:"location,omitempty" doc:"Where the error occurred, e.g. body.stages[0].advancement"`
	Message  string `json:"message" doc:"What is wrong with it"`
	Value    any    `json:"value,omitempty" doc:"The offending value"`
}

// Error is an expected failure with a specific error code.
type Error struct {
	Kind    Kind
	Code    string
	Message string
	Fields  []FieldError
}

func (e *Error) Error() string { return fmt.Sprintf("%s: %s", e.Code, e.Message) }

// New returns an expected failure.
func New(kind Kind, code, format string, args ...any) *Error {
	return &Error{Kind: kind, Code: code, Message: fmt.Sprintf(format, args...)}
}

// Fields collects field errors so a validation can report all of them at once.
type Fields []FieldError

// Add records a field error.
func (f *Fields) Add(location, message string, value any) {
	*f = append(*f, FieldError{Location: location, Message: message, Value: value})
}

// Err returns a validation error carrying every field error, or nil.
func (f Fields) Err() error {
	if len(f) == 0 {
		return nil
	}
	return &Error{Kind: Invalid, Code: CodeValidationFailed, Message: "the request is invalid", Fields: f}
}

// Error codes shared by several features.
const (
	CodeValidationFailed    = "validation-failed"
	CodeBadRequest          = "bad-request"
	CodeUnauthenticated     = "unauthenticated"
	CodeForbidden           = "forbidden"
	CodeNotOrganizer        = "not-organizer"
	CodeInternal            = "internal-error"
	CodeTournamentNotFound  = "tournament-not-found"
	CodeMatchNotFound       = "match-not-found"
	CodeParticipantNotFound = "participant-not-found"
	CodeWrongStatus         = "wrong-status"
)

// Details is a Problem Details (RFC 9457) response with an error code.
type Details struct {
	Type   string       `json:"type,omitempty" doc:"A URI reference identifying the problem type"`
	Title  string       `json:"title" doc:"A short summary of the problem type"`
	Status int          `json:"status" doc:"The HTTP status code"`
	Detail string       `json:"detail,omitempty" doc:"An explanation specific to this occurrence"`
	Code   string       `json:"code" doc:"A stable, specific error code"`
	Errors []FieldError `json:"errors,omitempty" doc:"Every invalid field, for validation failures"`
}

func (d *Details) Error() string  { return d.Code + ": " + d.Detail }
func (d *Details) GetStatus() int { return d.Status }

// ContentType makes Huma send application/problem+json.
func (d *Details) ContentType(ct string) string {
	if ct == "application/json" {
		return "application/problem+json"
	}
	return ct
}

func details(status int, code, detail string, fields []FieldError) *Details {
	return &Details{Type: "about:blank", Title: http.StatusText(status), Status: status, Detail: detail, Code: code, Errors: fields}
}

// From maps any error to Problem Details. Unrecognized errors become a 500,
// and are logged since they are bugs or infrastructure failures.
func From(ctx context.Context, err error) error {
	if err == nil {
		return nil
	}
	var d *Details
	if errors.As(err, &d) {
		return d
	}
	var e *Error
	if errors.As(err, &e) {
		return details(statusOf[e.Kind], e.Code, e.Message, e.Fields)
	}
	if errors.Is(err, context.Canceled) {
		return details(499, "request-cancelled", "the request was cancelled", nil)
	}
	slog.ErrorContext(ctx, "unexpected error", "error", err)
	return details(http.StatusInternalServerError, CodeInternal, "an unexpected error occurred", nil)
}

// humaCodes gives Huma's own errors (request decoding and per-field
// validation) a code.
var humaCodes = map[int]string{
	http.StatusBadRequest:            CodeBadRequest,
	http.StatusUnprocessableEntity:   CodeValidationFailed,
	http.StatusUnauthorized:          CodeUnauthenticated,
	http.StatusForbidden:             CodeForbidden,
	http.StatusNotFound:              "not-found",
	http.StatusMethodNotAllowed:      "method-not-allowed",
	http.StatusRequestEntityTooLarge: "request-too-large",
	http.StatusUnsupportedMediaType:  "unsupported-media-type",
}

// Install hooks the mapper into Huma, so its own errors are Problem Details
// with codes too.
func Install() {
	huma.NewError = func(status int, msg string, errs ...error) huma.StatusError {
		var fields []FieldError
		for _, err := range errs {
			if err == nil {
				continue
			}
			var ed *huma.ErrorDetail
			if errors.As(err, &ed) {
				fields = append(fields, FieldError{Location: ed.Location, Message: ed.Message, Value: ed.Value})
			} else {
				fields = append(fields, FieldError{Message: err.Error()})
			}
		}
		code, ok := humaCodes[status]
		if !ok {
			code = CodeInternal
			if status < 500 {
				code = "request-failed"
			}
		}
		return details(status, code, msg, fields)
	}
}

// Register registers a Huma operation whose errors go through From.
func Register[I, O any](api huma.API, op huma.Operation, handler func(context.Context, *I) (*O, error)) {
	huma.Register(api, op, func(ctx context.Context, in *I) (*O, error) {
		out, err := handler(ctx, in)
		if err != nil {
			return nil, From(ctx, err)
		}
		return out, nil
	})
}
