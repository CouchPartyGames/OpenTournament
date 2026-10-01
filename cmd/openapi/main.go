// Command openapi exports the service's OpenAPI document to standard output.
package main

import (
	"flag"
	"log/slog"
	"os"

	"github.com/couchpartygames/opentournament/internal/app"
)

func main() {
	version := flag.String("version", "dev", "service build version to include in the document")
	flag.Parse()
	if err := app.WriteOpenAPI(os.Stdout, *version); err != nil {
		slog.Error("export OpenAPI", "error", err)
		os.Exit(1)
	}
}
