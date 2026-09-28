// Package telemetry sets up OpenTelemetry traces, metrics and logs. The
// exporters follow the standard OTEL_* environment variables (e.g.
// OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_TRACES_EXPORTER=none). Logs go to
// stdout as JSON and, bridged from log/slog, to OpenTelemetry.
package telemetry

import (
	"context"
	"errors"
	"log/slog"
	"os"

	"go.opentelemetry.io/contrib/bridges/otelslog"
	"go.opentelemetry.io/contrib/exporters/autoexport"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/log/global"
	"go.opentelemetry.io/otel/propagation"
	sdklog "go.opentelemetry.io/otel/sdk/log"
	sdkmetric "go.opentelemetry.io/otel/sdk/metric"
	"go.opentelemetry.io/otel/sdk/resource"
	sdktrace "go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
)

// Name is the service and instrumentation name.
const Name = "opentournament"

// Setup installs the global providers and the default logger. The returned
// function flushes and stops them.
func Setup(ctx context.Context, version string) (func(context.Context) error, error) {
	stdout := slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{Level: slog.LevelInfo})
	if os.Getenv("OTEL_SDK_DISABLED") == "true" {
		slog.SetDefault(slog.New(stdout))
		return func(context.Context) error { return nil }, nil
	}
	res, err := resource.Merge(resource.Default(), resource.NewWithAttributes(semconv.SchemaURL,
		semconv.ServiceName(Name), semconv.ServiceVersion(version)))
	if err != nil {
		return nil, err
	}
	spans, err := autoexport.NewSpanExporter(ctx)
	if err != nil {
		return nil, err
	}
	metrics, err := autoexport.NewMetricReader(ctx)
	if err != nil {
		return nil, err
	}
	logs, err := autoexport.NewLogExporter(ctx)
	if err != nil {
		return nil, err
	}
	tp := sdktrace.NewTracerProvider(sdktrace.WithBatcher(spans), sdktrace.WithResource(res))
	mp := sdkmetric.NewMeterProvider(sdkmetric.WithReader(metrics), sdkmetric.WithResource(res))
	lp := sdklog.NewLoggerProvider(sdklog.WithProcessor(sdklog.NewBatchProcessor(logs)), sdklog.WithResource(res))
	otel.SetTracerProvider(tp)
	otel.SetMeterProvider(mp)
	global.SetLoggerProvider(lp)
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(propagation.TraceContext{}, propagation.Baggage{}))
	slog.SetDefault(slog.New(slog.NewMultiHandler(stdout, otelslog.NewHandler(Name, otelslog.WithLoggerProvider(lp)))))

	return func(ctx context.Context) error {
		return errors.Join(tp.Shutdown(ctx), mp.Shutdown(ctx), lp.Shutdown(ctx))
	}, nil
}
