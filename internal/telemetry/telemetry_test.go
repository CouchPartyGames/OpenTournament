package telemetry

import (
	"context"
	"testing"
)

func TestSetup(t *testing.T) {
	for _, signal := range []string{"TRACES", "METRICS", "LOGS"} {
		t.Setenv("OTEL_"+signal+"_EXPORTER", "none")
	}
	shutdown, err := Setup(context.Background(), "test")
	if err != nil {
		t.Fatalf("Setup() = %v", err)
	}
	if err := shutdown(context.Background()); err != nil {
		t.Errorf("shutdown() = %v", err)
	}
}
