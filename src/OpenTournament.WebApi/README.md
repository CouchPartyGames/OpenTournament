# OpenTournament.WebApi

The Web API layer for the OpenTournament tournament management system. This project provides the HTTP interface and endpoint routing for the application.

## Overview

OpenTournament.WebApi is a thin API layer built on ASP.NET Core Minimal APIs that serves as the entry point for HTTP requests. It handles:

- API endpoint routing and versioning
- HTTP request/response pipeline configuration
- OpenAPI/Swagger documentation
- Global exception handling
- CORS policies
- Authentication and authorization middleware

The actual business logic, data access, and feature implementations reside in the `OpenTournament.Api` project, following the Vertical Slice Architecture pattern.

## Architecture

This project follows a **thin controller** pattern where endpoints are defined in dedicated endpoint classes that delegate to feature handlers in the core API project.

### Project Structure

```
OpenTournament.WebApi/
├── Endpoints/              # HTTP endpoint definitions
│   ├── AuthenticationEndpoints.cs
│   ├── CompetitionEndpoints.cs
│   ├── EventEndpoints.cs
│   ├── MatchEndpoints.cs
│   ├── RegistrationEndpoints.cs
│   └── TournamentEndpoints.cs
├── Middleware/             # Custom middleware
│   ├── Exceptions/         # Exception handling
│   └── Cors/              # CORS configuration
├── Properties/            # Launch settings
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## Features

### API Versioning
- URL segment versioning (e.g., `/tournaments/v1/`)
- Version 1 is the current default
- API version reporting in responses

### Endpoint Groups
All endpoints are organized into logical groups with consistent versioning:

- **Authentication** (`/authentication/v{version}`) - User authentication and authorization
- **Tournaments** (`/tournaments/v{version}`) - Tournament management
- **Competitions** (`/competitions/v{version}`) - Competition within tournaments
- **Events** (`/events/v{version}`) - Event management
- **Matches** (`/matches/v{version}`) - Match scheduling and results
- **Registrations** (`/registrations/v{version}`) - Participant registration

### Middleware Pipeline

The application configures the following middleware in order:
1. Exception Handler (global error handling)
2. Status Code Pages
3. Authentication
4. Authorization
5. CORS (when enabled)
6. HTTP Logging (when enabled)

### OpenAPI Support

OpenAPI documentation is automatically generated and available in development mode:
- Endpoint: `/openapi/v1.json`
- Includes all endpoint metadata (summaries, descriptions, tags)
- Supports API versioning

## Getting Started

### Prerequisites

- .NET 10 SDK
- OpenTournament.Api project (referenced dependency)

### Running the Application

From the `OpenTournament.WebApi` directory:

```bash
# Development
dotnet run

# Production
dotnet run --configuration Release
```

The API will start on the default Kestrel ports (typically http://localhost:5000 and https://localhost:5001).

### Configuration

Configuration is managed through `appsettings.json` and `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Additional configuration (database, authentication, etc.) is inherited from the `OpenTournament.Api` project.

## Endpoints

### Example: Tournament Endpoints

```http
POST   /tournaments/v1           # Create new tournament (authenticated)
GET    /tournaments/v1/{id}      # Get tournament details (public)
PUT    /tournaments/v1/{id}      # Update tournament (authenticated)
DELETE /tournaments/v1/{id}      # Delete tournament (authenticated)
PUT    /tournaments/v1/{id}/start # Start tournament (authenticated)
```

### Health Check

```http
GET /health                      # Application health status
```

## Security

- **Server Header**: Removed for security (configured via Kestrel)
- **Authentication**: Integrated with Firebase Authentication (configured in OpenTournament.Api)
- **Authorization**: Policy-based authorization for protected endpoints
- **CORS**: Configurable CORS policies (currently commented out in Program.cs)

## Development

### Adding New Endpoints

1. Create a new endpoint class in `Endpoints/`:

```csharp
public static class MyFeatureEndpoints
{
    public static RouteGroupBuilder MapMyFeatureEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("", MyFeature.GetAll.Endpoint)
            .WithTags("MyFeature")
            .WithSummary("Get all items")
            .WithOpenApi();

        return builder;
    }
}
```

2. Register the endpoint group in `Program.cs`:

```csharp
app.MapGroup("myfeature/v{apiVersion:apiVersion}")
    .MapMyFeatureEndpoints()
    .WithApiVersionSet(apiVersionSet);
```

### Exception Handling

Global exception handling is configured via `GlobalExceptionHandler` in the Middleware folder. All unhandled exceptions are caught and converted to appropriate HTTP responses.

## Dependencies

- **Asp.Versioning.Http** - API versioning support
- **Asp.Versioning.Mvc.ApiExplorer** - API explorer for versioned APIs
- **Microsoft.AspNetCore.OpenApi** - OpenAPI document generation
- **OpenTournament.Api** - Core business logic and features

## Related Projects

- **OpenTournament.Api** - Core business logic, data access, and features
- **OpenTournament.Frontend** - SvelteKit web frontend
- **OpenTournament.Tests.Unit** - Unit tests
- **OpenTournament.Tests.Integration** - Integration tests

## License

See the root LICENSE file for license information.
