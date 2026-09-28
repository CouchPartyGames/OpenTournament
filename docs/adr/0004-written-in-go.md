# The service is written in Go

The v1 spec was first written for .NET and was switched to Go before any code existed. Agones and Kubernetes are themselves written in Go, so their official clients, `client-go` informers and the Agones SDK are all native Go, and coordinating Game Servers (ADR-0001) is the riskiest part of the system. In .NET that integration would rely on generic Kubernetes clients and hand-maintained Agones CRD types.
