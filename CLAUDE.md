# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

OpenTournament is a tournament management system built with a .NET 10 backend and SvelteKit frontend. The project uses Vertical Slice Architecture for backend features and minimal APIs for endpoint definitions.

## Technology Stack

### Backend (.NET 10)
- **OpenTournament.WebApi**: ASP.NET Core minimal API web layer (entry point)
- **OpenTournament.Core**: Core business logic with Vertical Slice Architecture
- **Entity Framework Core** with PostgreSQL
- **ErrorOr** for functional error handling (OneOf is deprecated)
- API versioning with URL-based versioning (`/v1/`, `/v2/`)
- Scalar for API documentation (in development mode)

### Frontend (SvelteKit)
- **Svelte 5** with TypeScript
- **Tailwind CSS 4 (beta)** and **DaisyUI**
- SvelteKit with adapter-auto
- Vite build system
- Auth.js (@auth/sveltekit) for authentication

### Infrastructure
- PostgreSQL database
- Redis for caching (Hybrid Cache)
- RabbitMQ for messaging (MassTransit)
- OpenTelemetry for observability
- Aspire Dashboard for development monitoring
- Docker Compose for local development

### Testing
- **xUnit** testing framework
- **FluentAssertions** for test assertions
- **NSubstitute** for mocking (unit tests)
- **Testcontainers** for integration tests
- **Bogus** for test data generation
- **Respawn** for database cleanup between tests

## Common Commands

### Backend (.NET)

```bash
# Build the solution
dotnet build

# Run the API locally
cd src/OpenTournament.WebApi
dotnet run

# Run tests (all)
dotnet test

# Run unit tests only
dotnet test tests/OpenTournament.Tests.Unit

# Run integration tests only
dotnet test tests/OpenTournament.Tests.Integration

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"

# Create database migration
cd src/OpenTournament.Core
dotnet ef migrations add MigrationName --startup-project ../OpenTournament.WebApi

# Apply migrations
cd src/OpenTournament.WebApi
dotnet ef database update --project ../OpenTournament.Core
```

### Frontend (SvelteKit)

```bash
cd src/OpenTournament.Frontend

# Install dependencies
npm install

# Run development server
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Type-check
npm run check

# Type-check with watch mode
npm run check:watch

# Format code
npm run format

# Lint
npm run lint
```

### Docker

```bash
# Start infrastructure (PostgreSQL, Redis, RabbitMQ, Aspire Dashboard)
cd extra
docker compose up

# Stop infrastructure
docker compose down

# View logs
docker compose logs -f
```

Create `extra/.env` file first with:
```
POSTGRES_PASSWORD=YourPassword
POSTGRES_USER=YourUser
POSTGRES_DB=tournament
REDIS_PASSWORD=YourPassword
```

Access Aspire Dashboard at http://localhost:18888 for monitoring OpenTelemetry metrics.

## Architecture

### Backend: Vertical Slice Architecture

Features are organized as self-contained slices in `OpenTournament.Core/Features/`:

```
Features/
├── Authentication/
├── Competitions/
│   ├── Create/
│   │   ├── CreateCompetition.cs
│   │   ├── CreateCompetitionCommand.cs
│   │   └── CreateCompetitionHandler.cs
│   └── Get/
├── Events/
├── Matches/
├── Registration/
├── Templates/
└── Tournaments/
```

Each feature slice contains:
- Command/Query definitions
- Handler implementation
- Validators (FluentValidation)
- DTOs/Response models
- All related business logic

### API Layer Structure

The WebApi project (`src/OpenTournament.WebApi/`) contains:
- **Program.cs**: Application bootstrap and minimal API configuration
- **Endpoints/**: Endpoint mapping classes (e.g., `TournamentEndpoints.cs`)
- **Middleware/**: Cross-cutting concerns (exception handling, CORS)

Endpoints use ASP.NET Core minimal APIs with typed results:
```csharp
builder.MapPut("/{id}", async Task<Results<NoContent, BadRequest>> (
    string id,
    HttpContext httpContext,
    AppDbContext dbContext,
    CancellationToken ct) =>
{
    var result = await Handler.HandleAsync(id, httpContext, dbContext, ct);
    return result.IsError ? TypedResults.BadRequest() : TypedResults.NoContent();
})
```

### Domain Model

Located in `OpenTournament.Core/Domain/`:

**Entities**: Tournament, Competition, Event, Match, Participant, Template

**Value Objects**: Strongly-typed IDs for type safety
- `TournamentId`, `CompetitionId`, `EventId`, `MatchId`, `ParticipantId`
- Create with: `var id = TournamentId.New()`
- Parse with: `var id = TournamentId.TryParse(stringValue)`

### Error Handling with ErrorOr

**CRITICAL**: Use `ErrorOr<T>` for all handler return types. `OneOf` is deprecated.

Handlers return `ErrorOr<T>`:
```csharp
public static async Task<ErrorOr<Created>> HandleAsync(
    CreateCommand command,
    AppDbContext dbContext,
    CancellationToken cancellationToken)
{
    // Validation errors
    if (string.IsNullOrEmpty(command.Name))
        return Error.Validation("Name.Empty", "Name is required");

    // Not found errors
    if (entity is null)
        return Error.NotFound("Entity.NotFound", "Entity not found");

    // Conflict errors
    if (await dbContext.Entities.AnyAsync(e => e.Name == command.Name))
        return Error.Conflict("Entity.Duplicate", "Entity already exists");

    // Success
    await dbContext.SaveChangesAsync(cancellationToken);
    return Result.Created;
}
```

Common return types:
- `ErrorOr<Created>` - Create operations
- `ErrorOr<Updated>` - Update operations
- `ErrorOr<Deleted>` - Delete operations
- `ErrorOr<Success>` - Generic success
- `ErrorOr<TResult>` - Query operations

### Infrastructure Services

Located in `OpenTournament.Core/Infrastructure/`:

- **Persistence/**: EF Core DbContext, configurations, value converters
- **Authentication/**: Firebase JWT authentication
- **Authorization/**: Policy-based authorization
- **Caching/**: Hybrid cache with Redis
- **Messaging/**: RabbitMQ with MassTransit
- **Observability/**: OpenTelemetry configuration

All services are registered through extension methods (e.g., `AddPostgres()`, `AddAuthenticationServices()`).

### Frontend Structure

```
src/OpenTournament.Frontend/src/
├── lib/
│   └── components/
├── routes/
│   ├── +layout.svelte          # Root layout
│   ├── +page.svelte            # Home page
│   ├── admin/                  # Admin pages
│   ├── login/                  # Authentication
│   ├── about/
│   ├── contact/
│   └── search/
└── app.html
```

SvelteKit file-based routing:
- `+page.svelte` - Page component
- `+page.js/ts` - Page load function
- `+layout.svelte` - Layout component
- `+error.svelte` - Error page

## Creating New Features

### 1. Backend Feature (Vertical Slice)

```bash
# Create feature directory structure
mkdir -p src/OpenTournament.Core/Features/YourFeature/Create
```

Define command:
```csharp
namespace OpenTournament.Core.Features.YourFeature.Create;

public sealed record CreateCommand(string Name, string Description);
```

Implement handler:
```csharp
public static class CreateCommandHandler
{
    public static async Task<ErrorOr<Created>> HandleAsync(
        CreateCommand command,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.Name))
            return Error.Validation("Name.Empty", "Name is required");

        // Business logic
        var entity = new YourEntity
        {
            Id = YourEntityId.New(),
            Name = command.Name
        };

        await dbContext.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created;
    }
}
```

### 2. Add API Endpoint

In `src/OpenTournament.WebApi/Endpoints/YourFeatureEndpoints.cs`:

```csharp
public static class YourFeatureEndpoints
{
    public static RouteGroupBuilder MapYourFeatureEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/", async Task<Results<Created, BadRequest>> (
            CreateCommand command,
            AppDbContext dbContext,
            CancellationToken ct) =>
        {
            var result = await CreateCommandHandler.HandleAsync(command, dbContext, ct);
            return result.IsError ? TypedResults.BadRequest() : TypedResults.Created();
        })
        .WithTags("YourFeature")
        .WithSummary("Create YourFeature")
        .RequireAuthorization();

        return builder;
    }
}
```

Register in `Program.cs`:
```csharp
app.MapGroup("yourfeature/v{apiVersion:apiVersion}")
    .MapYourFeatureEndpoints()
    .WithApiVersionSet(apiVersionSet);
```

### 3. Frontend Page

Create `src/OpenTournament.Frontend/src/routes/yourpage/+page.svelte`:

```svelte
<script lang="ts">
    // Page logic
</script>

<div>
    <!-- Page content -->
</div>
```

## Best Practices

### Backend
1. **Always use ErrorOr**: Never throw exceptions for business logic errors
2. **Use strongly-typed IDs**: `TournamentId.New()` not `Guid.NewGuid()`
3. **Keep features independent**: No cross-feature dependencies
4. **Always pass CancellationToken**: Include in all async methods
5. **Meaningful error codes**: Use format `"Entity.ErrorType"` (e.g., `"Tournament.NotFound"`)

### Frontend
1. **Use TypeScript**: All new components should be `.svelte` with TypeScript in `<script lang="ts">`
2. **Follow SvelteKit conventions**: Use `+page.svelte`, `+layout.svelte`, `+page.ts` patterns
3. **Use Tailwind classes**: Leverage Tailwind CSS and DaisyUI components

### Testing
1. **Unit tests**: Test handlers in isolation with NSubstitute for dependencies
2. **Integration tests**: Use Testcontainers for real PostgreSQL database
3. **Use FluentAssertions**: `result.IsError.Should().BeFalse()`
4. **Test both success and error paths**: Especially validation and not-found cases

## Package Management

The solution uses **Central Package Management** (Directory.Packages.props). Package versions are defined centrally at the solution level. When adding packages:

```bash
# Add package reference (version managed centrally)
dotnet add package PackageName
```

## Important Files

- **Directory.Packages.props**: Central package version management
- **OpenTournament.slnx**: Solution file (XML format)
- **src/OpenTournament.Core/README.md**: Detailed Core library documentation
- **extra/docker-compose.yaml**: Local development infrastructure
- **.github/workflows/dotnet.yml**: CI/CD workflow

## Development Workflow

1. Start infrastructure: `cd extra && docker compose up`
2. Run migrations: `cd src/OpenTournament.WebApi && dotnet ef database update --project ../OpenTournament.Core`
3. Start API: `cd src/OpenTournament.WebApi && dotnet run`
4. Start frontend: `cd src/OpenTournament.Frontend && npm run dev`
5. Access:
   - API: https://localhost:5001 (Scalar docs: /scalar/v1)
   - Frontend: http://localhost:5173
   - Aspire Dashboard: http://localhost:18888

## API Versioning

The API uses URL-based versioning:
- Version 1: `/authentication/v1/`, `/tournaments/v1/`, etc.
- Configure in endpoint groups with `WithApiVersionSet(apiVersionSet)`
- New versions defined in `Program.cs`: `apiVersionSet.HasApiVersion(new ApiVersion(2))`
