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

.PHONY: build generate test test-engine vet image

build:
	CGO_ENABLED=0 go build -ldflags "-X main.version=$(VERSION)" -o bin/opentournament ./cmd/opentournament

generate:
	sqlc generate

vet:
	go vet ./...

test:
	$(TEST_ENV) go test ./...

test-engine:
	go test ./internal/format/...

image:
	docker build --build-arg VERSION=$(VERSION) -t opentournament:$(VERSION) .
