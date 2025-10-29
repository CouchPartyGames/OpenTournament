# OpenTournament

[![Basic validation](https://github.com/CouchPartyGames/OpenTournament/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/CouchPartyGames/OpenTournament/actions/workflows/dotnet.yml)

A modern, open-source tournament management system built with .NET 10 and SvelteKit. OpenTournament provides a complete solution for organizing, managing, and running competitive tournaments with support for various bracket formats.

## Features

- **Tournament Management**: Create and manage tournaments with multiple competitions and events
- **Bracket Generation**: Automatic bracket generation using single elimination format
- **Real-time Updates**: Live match updates and notifications via SignalR
- **User Registration**: Participant registration and management
- **Match Management**: Track match results, progression, and tournament standings
- **Template System**: Reusable tournament templates for quick setup
- **REST API**: Full-featured API with versioning and OpenAPI documentation
- **Authentication**: Firebase JWT authentication with Google OAuth support
- **Observability**: OpenTelemetry integration with Aspire Dashboard for monitoring

## Technology Stack

### Backend
- **.NET 10** - Modern, high-performance framework
- **ASP.NET Core Minimal APIs** - Lightweight, fast APIs
- **Entity Framework Core** - ORM with PostgreSQL
- **Vertical Slice Architecture** - Feature-focused code organization
- **ErrorOr** - Functional error handling
- **MassTransit** - Distributed messaging with RabbitMQ
- **OpenTelemetry** - Distributed tracing and metrics

### Frontend
- **SvelteKit** - Modern web framework with Svelte 5
- **TypeScript** - Type-safe JavaScript
- **Tailwind CSS 4** - Utility-first CSS framework
- **DaisyUI** - Component library for Tailwind
- **Auth.js** - Authentication library

### Infrastructure
- **PostgreSQL** - Primary database
- **Redis** - Caching layer
- **RabbitMQ** - Message broker
- **Docker** - Containerization
- **Aspire Dashboard** - Development monitoring

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 18+](https://nodejs.org/) (for frontend)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/) (for infrastructure)
- [PostgreSQL 16](https://www.postgresql.org/) (or use Docker Compose)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/CouchPartyGames/OpenTournament.git
cd OpenTournament
```

### 2. Start Infrastructure Services

Create an `.env` file in the `extra` directory:

```bash
cd extra
cat > .env << EOF
POSTGRES_PASSWORD=YourPassword
POSTGRES_USER=YourUser
POSTGRES_DB=tournament
REDIS_PASSWORD=YourPassword
EOF
```

Start PostgreSQL, Redis, RabbitMQ, and Aspire Dashboard:

```bash
docker compose up -d
```

Services will be available at:
- PostgreSQL: `localhost:5432`
- Redis: `localhost:6379`
- RabbitMQ Management: `http://localhost:15672`
- Aspire Dashboard: `http://localhost:18888`

### 3. Setup Database

Apply Entity Framework migrations:

```bash
cd ../src/OpenTournament.WebApi
dotnet ef database update --project ../OpenTournament.Core
```

### 4. Run the Backend API

```bash
cd src/OpenTournament.WebApi
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- API Documentation: `https://localhost:5001/scalar/v1`

### 5. Run the Frontend

```bash
cd src/OpenTournament.Frontend
npm install
npm run dev
```

The frontend will be available at `http://localhost:5173`

## Project Structure

```
OpenTournament/
├── src/
│   ├── OpenTournament.WebApi/      # API entry point (Minimal APIs)
│   ├── OpenTournament.Core/        # Core business logic (Vertical Slices)
│   └── OpenTournament.Frontend/    # SvelteKit frontend
├── tests/
│   ├── OpenTournament.Tests.Unit/  # Unit tests
│   └── OpenTournament.Tests.Integration/  # Integration tests
├── extra/
│   ├── docker-compose.yaml         # Infrastructure services
│   └── opentournament.service      # SystemD service file
├── Directory.Packages.props        # Central package management
└── OpenTournament.slnx             # Solution file
```

### Architecture

OpenTournament uses **Vertical Slice Architecture** where features are organized as self-contained slices:

```
OpenTournament.Core/Features/
├── Authentication/
│   ├── Login/
│   └── Register.cs
├── Competitions/
│   ├── Create/
│   │   ├── CreateCompetitionCommand.cs
│   │   └── CreateCompetitionHandler.cs
│   └── Get/
│       ├── GetCompetitionQuery.cs
│       ├── GetCompetitionResponse.cs
│       └── GetCompetitionHandler.cs
├── Tournaments/
│   ├── Create/
│   │   ├── CreateTournamentCommand.cs
│   │   ├── CreateTournamentValidator.cs
│   │   └── CreateTournamentHandler.cs
│   ├── Get/
│   ├── Update/
│   ├── Delete/
│   └── Start/
├── Events/
│   ├── Create/
│   └── Get/
├── Matches/
│   ├── Complete/
│   ├── Get/
│   └── Update/
├── Registration/
│   ├── Join/
│   ├── Leave/
│   └── List/
└── Templates/
    ├── Create/
    ├── Get/
    ├── Update/
    ├── Delete/
    └── List/
```

Each feature slice contains:
- Commands/Queries
- Handlers
- Validators (optional, using FluentValidation)
- Response DTOs
- Business logic

### Example: Creating a Tournament Feature Slice

#### 1. Command Definition

The command defines the input data for creating a tournament:

```csharp
// CreateTournamentCommand.cs
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core.Features.Tournaments.Create;

public sealed record CreateTournamentCommand(
    [Required]
    [property: Description("name of the tournament")]
    string Name,

    [property: Description("start time of the tournament")]
    DateTime StartTime,

    [property: Description("minimum number of users required to start")]
    int MinParticipants,

    [property: Description("maximum number of users allowed to join")]
    int MaxParticipants,

    [property: Description("single or double elimination tournament")]
    EliminationMode Mode,

    [property: Description("seed players randomly or by rank")]
    DrawSeeding Seeding
);
```

#### 2. Validator (Optional but Recommended)

FluentValidation provides complex validation rules:

```csharp
// CreateTournamentValidator.cs
using FluentValidation;

namespace OpenTournament.Core.Features.Tournaments.Create;

public sealed class CreateTournamentValidator : AbstractValidator<CreateTournamentCommand>
{
    public CreateTournamentValidator()
    {
        RuleFor(c => c.Name)
            .Length(3, 50)
            .NotEmpty();

        RuleFor(c => c.StartTime)
            .GreaterThan(DateTime.Now);

        RuleFor(c => c.MinParticipants)
            .GreaterThan(2);

        RuleFor(c => c.MaxParticipants)
            .GreaterThan(c => c.MinParticipants)
            .LessThanOrEqualTo(256);

        RuleFor(c => c.Mode)
            .IsInEnum();

        RuleFor(c => c.Seeding)
            .IsInEnum();
    }
}
```

#### 3. Handler Implementation

The handler processes the command and returns `ErrorOr<T>`:

```csharp
// CreateTournamentHandler.cs
using ErrorOr;
using FluentValidation.Results;
using OpenTournament.Core.Domain.Entities;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Create;

public static class CreateTournamentHandler
{
    public static async Task<ErrorOr<Created>> HandleAsync(
        CreateTournamentCommand command,
        string userId,
        AppDbContext dbContext,
        CancellationToken ct)
    {
        // Validate the command
        CreateTournamentValidator validator = new();
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);
        if (!validationResult.IsValid)
        {
            return Error.Validation();
        }

        // Create the tournament entity
        var tournament = new Tournament
        {
            Id = TournamentId.NewTournamentId(),
            Name = command.Name,
            Creator = Creator.New(new ParticipantId(userId))
        };

        // Persist to database
        await dbContext.AddAsync(tournament, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Created;
    }
}
```

#### 4. Query and Response (for Get operations)

For read operations, define a query and response:

```csharp
// GetCompetitionQuery.cs
namespace OpenTournament.Core.Features.Competitions.Get;

public record GetCompetitionQuery();

// GetTournamentResponse.cs
using OpenTournament.Core.Domain.Entities;

namespace OpenTournament.Core.Features.Tournaments.Get;

public sealed record GetTournamentResponse(Tournament Tournament);

// GetTournamentHandler.cs
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using OpenTournament.Core.Domain.ValueObjects;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.Tournaments.Get;

public static class GetTournamentHandler
{
    public static async Task<ErrorOr<GetTournamentResponse>> HandleAsync(
        string id,
        AppDbContext dbContext,
        CancellationToken token)
    {
        var tournamentId = TournamentId.TryParse(id);
        if (tournamentId is null)
        {
            return Error.Validation();
        }

        var tournament = await dbContext
            .Tournaments
            .Include(m => m.Matches)
            .FirstOrDefaultAsync(m => m.Id == tournamentId, token);

        if (tournament is null)
        {
            return Error.NotFound();
        }

        return new GetTournamentResponse(tournament);
    }
}
```

See [src/OpenTournament.Core/README.md](src/OpenTournament.Core/README.md) for detailed architecture documentation.

## Development

### Backend Development

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run unit tests only
dotnet test tests/OpenTournament.Tests.Unit

# Run integration tests only
dotnet test tests/OpenTournament.Tests.Integration

# Create a new migration
cd src/OpenTournament.Core
dotnet ef migrations add MigrationName --startup-project ../OpenTournament.WebApi

# Apply migrations
cd src/OpenTournament.WebApi
dotnet ef database update --project ../OpenTournament.Core
```

### Frontend Development

```bash
cd src/OpenTournament.Frontend

# Run development server
npm run dev

# Type-check
npm run check

# Format code
npm run format

# Lint
npm run lint

# Build for production
npm run build
```

### API Documentation

When running in development mode, API documentation is available via Scalar:
- Scalar UI: `https://localhost:5001/scalar/v1`
- OpenAPI spec: `https://localhost:5001/openapi/v1.json`

## Testing

### Unit Tests

Unit tests use xUnit, FluentAssertions, and NSubstitute:

```bash
dotnet test tests/OpenTournament.Tests.Unit
```

### Integration Tests

Integration tests use Testcontainers for real PostgreSQL instances:

```bash
dotnet test tests/OpenTournament.Tests.Integration
```

### Test Structure

- **FluentAssertions** - Readable assertions
- **NSubstitute** - Mocking framework
- **Testcontainers** - Docker containers for integration tests
- **Bogus** - Test data generation
- **Respawn** - Database cleanup between tests

## Deployment

### Docker and Docker Compose

The `extra/docker-compose.yaml` file provides all required infrastructure services.

To start services:

```bash
cd extra
docker compose up -d
```

To stop services:

```bash
docker compose down
```

To view logs:

```bash
docker compose logs -f
```

### SystemD Configuration

For production deployments on Linux, a SystemD service file is provided at `extra/opentournament.service`.

1. Copy the service file:
```bash
sudo cp extra/opentournament.service /etc/systemd/system/
```

2. Edit the service file to match your installation paths

3. Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable --now opentournament.service
```

4. Check service status:
```bash
sudo systemctl status opentournament.service
```

## API Versioning

The API uses URL-based versioning:

- Version 1: `/tournaments/v1/`, `/events/v1/`, `/matches/v1/`
- Version 2: `/tournaments/v2/` (when available)

All endpoints are versioned to maintain backward compatibility.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow Vertical Slice Architecture for new features
- Use ErrorOr for error handling (OneOf is deprecated)
- Use strongly-typed IDs for domain entities
- Write unit tests for business logic
- Write integration tests for API endpoints
- Follow existing code style and conventions

See [CLAUDE.md](CLAUDE.md) for detailed development guidance.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a list of changes and version history.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/CouchPartyGames/OpenTournament).

---

Built with ❤️ by [CouchPartyGames](https://github.com/CouchPartyGames)