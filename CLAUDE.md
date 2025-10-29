# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenTournament is a tournament management system built with:
- **Backend**: .NET 9 Web API using Minimal APIs and Vertical Slice Architecture
- **Frontend**: SvelteKit with TypeScript, Tailwind CSS, and DaisyUI
- **Mobile**: .NET MAUI cross-platform application  
- **Database**: PostgreSQL with Entity Framework Core
- **Message Queue**: RabbitMQ with MassTransit
- **Caching**: Redis with Hybrid Caching
- **Authentication**: Firebase Authentication + Google OAuth
- **Observability**: OpenTelemetry with .NET Aspire Dashboard

## Development Commands

### .NET Backend
```bash
# Build the solution
dotnet build

# Run the API (from src/OpenTournament.Api/)
dotnet run

# Run database migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add [MigrationName]

# Run unit tests
cd tests/OpenTournament.Tests.Unit
dotnet test

# Run integration tests
cd tests/OpenTournament.Tests.Integration
dotnet test
```

### Frontend (SvelteKit)
```bash
# From src/OpenTournament.Frontend/
npm run dev          # Development server
npm run build        # Production build
npm run preview      # Preview production build
npm run lint         # Lint check
npm run format       # Format code
npm run check        # TypeScript/Svelte check
```

### Infrastructure (Docker)
```bash
# From extra/ directory
docker compose up    # Start PostgreSQL, Redis, RabbitMQ, and Aspire Dashboard
docker compose down  # Stop all services
```

Required `.env` file in `extra/`:
```
POSTGRES_PASSWORD=YourPassword
POSTGRES_USER=YourUser  
POSTGRES_DB=tournament
REDIS_PASSWORD=YourPassword
```

## Architecture Patterns

### Vertical Slice Architecture
The API uses Vertical Slice Architecture where each feature is self-contained:
- **Features/**: Each feature folder contains all related logic (commands, handlers, validators, endpoints)
- **Data/Models/**: Domain entities with strong typing using record types and value objects
- **Mediator/**: CQRS with MediatR for command/query handling
- **DomainEvents/**: Event-driven architecture with MassTransit

### Key Architectural Components

**Domain Models**: Located in `Data/Models/` with strongly-typed IDs:
- `Tournament`, `Competition`, `Event`, `Match`, `Participant`
- Value objects like `TournamentId`, `CompetitionId`, etc.

**Entity Framework Configuration**: 
- Configurations in `Data/EntityMapping/`
- Value converters in `Data/ValueConverters/`
- Database context: `AppDbContext.cs`

**Authentication & Authorization**:
- Firebase Authentication with custom JWT handling
- Identity management in `Identity/` folder
- Authorization policies in `Configuration/Infrastructure/AuthorizationServices.cs`

**Validation**: FluentValidation integrated with pipeline behaviors

**Background Jobs**: MassTransit consumers in `Jobs/` for handling domain events

## Development Guidelines

### API Development
- Use Minimal APIs with endpoint mapping in feature folders
- Follow CQRS pattern with MediatR
- Implement proper validation with FluentValidation
- Use strongly-typed IDs and value objects
- Handle domain events with MassTransit publishers/consumers

### Database Development
- Use Entity Framework migrations for schema changes
- Configure entities in dedicated configuration classes
- Use value converters for custom types
- Follow repository pattern through EF Core DbContext

### Frontend Development  
- Use SvelteKit with TypeScript
- Style with Tailwind CSS and DaisyUI components
- Follow component-based architecture in `src/lib/components/`
- API communication through SvelteKit load functions

### Testing
- Unit tests in `tests/OpenTournament.Tests.Unit/`
- Integration tests in `tests/OpenTournament.Tests.Integration/` 
- Use TestAuthenticationHandler for auth in integration tests
- Test domain logic, validation rules, and API endpoints

## Service Dependencies

**External Services**:
- PostgreSQL (port 5432)
- Redis (port 6379)  
- RabbitMQ (port 5672, management UI on 15672)
- .NET Aspire Dashboard (port 18888)

**Authentication**:
- Firebase Authentication
- Google OAuth integration
- JWT token validation

The system uses OpenTelemetry for observability with metrics, logging, and tracing exported to the Aspire Dashboard.