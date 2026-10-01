package app

import (
	"encoding/json"
	"io"
	"net/http"
	"reflect"

	"github.com/couchpartygames/opentournament/internal/features/gameservers"
	"github.com/couchpartygames/opentournament/internal/features/matches"
	"github.com/couchpartygames/opentournament/internal/features/participants"
	"github.com/couchpartygames/opentournament/internal/features/registrations"
	"github.com/couchpartygames/opentournament/internal/features/structure"
	"github.com/couchpartygames/opentournament/internal/features/tournaments"
	"github.com/couchpartygames/opentournament/internal/format"
	"github.com/couchpartygames/opentournament/internal/problem"
	"github.com/couchpartygames/opentournament/internal/tournament"
	"github.com/danielgtaylor/huma/v2"
	"github.com/danielgtaylor/huma/v2/adapters/humago"
)

// WriteOpenAPI writes the service's OpenAPI 3.1 document without connecting to
// PostgreSQL, Keycloak or Agones. The version is the service's build version;
// use "dev" for the committed contract so builds don't create unrelated diffs.
func WriteOpenAPI(w io.Writer, version string) error {
	api := newAPI(http.NewServeMux(), &tournament.Service{}, Config{Version: version})
	enc := json.NewEncoder(w)
	enc.SetIndent("", "  ")
	return enc.Encode(api.OpenAPI())
}

// Registration only describes handlers and schemas; it doesn't call the
// service. Keeping it shared makes offline exports follow the running API.
func newAPI(mux *http.ServeMux, svc *tournament.Service, cfg Config) huma.API {
	problem.Install()
	hc := huma.DefaultConfig("Open Tournament API", cfg.Version)
	hc.Info.Description = "Real-time tournaments for online games, played on Agones Game Servers."
	hc.OpenAPIPath = APIPrefix + "/openapi"
	hc.DocsPath = ""
	if cfg.Docs {
		hc.DocsPath = APIPrefix + "/docs"
		hc.DocsRenderer = huma.DocsRendererScalar
	}
	hc.SchemasPath = APIPrefix + "/schemas"
	hc.Components.SecuritySchemes = map[string]*huma.SecurityScheme{
		"keycloak":   {Type: "http", Scheme: "bearer", BearerFormat: "JWT", Description: "A Keycloak-issued access token."},
		"matchToken": {Type: "http", Scheme: "bearer", BearerFormat: "JWT", Description: "The per-Match token a Game Server receives when it is allocated."},
	}
	hc.Components.Schemas.RegisterTypeAlias(reflect.TypeFor[format.Kind](), reflect.TypeFor[formatSchema]())
	api := humago.New(mux, hc)

	tournaments.Register(api, svc)
	registrations.Register(api, svc)
	participants.Register(api, svc)
	structure.Register(api, svc)
	matches.Register(api, svc)
	gameservers.Register(api, svc)
	return api
}

// formatSchema stands in for a Stage's Format in the OpenAPI document, which
// lists every Format as an enum and so rejects any other in a request. It
// lives here to keep the Format engine free of the HTTP framework.
type formatSchema format.Kind

// Schema lists the Formats as an enum.
func (formatSchema) Schema(huma.Registry) *huma.Schema {
	s := &huma.Schema{Type: huma.TypeString}
	for _, k := range format.Kinds {
		s.Enum = append(s.Enum, string(k))
	}
	return s
}
