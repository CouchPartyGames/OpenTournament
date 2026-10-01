# With Podman instead of Docker, testcontainers needs the Podman socket:
#   systemctl --user start podman.socket
# The test target points testcontainers at it when no DOCKER_HOST is set.
PODMAN_SOCKET := /run/user/$(shell id -u)/podman/podman.sock
ifeq ($(DOCKER_HOST),)
ifneq ($(wildcard $(PODMAN_SOCKET)),)
TEST_ENV := DOCKER_HOST=unix://$(PODMAN_SOCKET) TESTCONTAINERS_RYUK_DISABLED=true
endif
endif

VERSION ?= $(shell git describe --tags --always --dirty 2>/dev/null || echo dev)

.PHONY: build generate openapi openapi-check test test-engine vet image

build:
	CGO_ENABLED=0 go build -ldflags "-X main.version=$(VERSION)" -o bin/opentournament ./cmd/opentournament

generate:
	sqlc generate

# Export with a fixed build version so changes in git history don't cause drift.
openapi:
	@tmp=$$(mktemp); trap 'rm -f "$$tmp"' EXIT; \
	go run ./cmd/openapi > "$$tmp" && mv "$$tmp" api/openapi.json

openapi-check:
	@tmp=$$(mktemp); trap 'rm -f "$$tmp"' EXIT; \
	go run ./cmd/openapi > "$$tmp" || exit $$?; \
	if ! diff -u api/openapi.json "$$tmp"; then \
		echo "OpenAPI document is out of date. Run 'make openapi' and commit api/openapi.json." >&2; \
		exit 1; \
	fi

vet:
	go vet ./...

test:
	$(TEST_ENV) go test ./...

test-engine:
	go test ./internal/format/...

image:
	docker build --build-arg VERSION=$(VERSION) -t opentournament:$(VERSION) .
