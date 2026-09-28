FROM docker.io/library/golang:1.27 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
ARG VERSION=dev
RUN CGO_ENABLED=0 go build -ldflags "-s -w -X main.version=${VERSION}" -o /opentournament ./cmd/opentournament

FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /opentournament /opentournament
EXPOSE 8080
ENTRYPOINT ["/opentournament"]
