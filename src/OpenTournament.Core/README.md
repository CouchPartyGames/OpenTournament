# OpenTournament.Core

Core business logic library for the OpenTournament system. This project contains domain entities, value objects, infrastructure services, and feature implementations using Vertical Slice Architecture.

## Architecture

### Vertical Slice Architecture

Features are organized as self-contained slices, each containing all the logic needed for a specific use case:

```
Features/
├── Competitions/
│   ├── Create/
│   │   ├── CreateCompetition.cs          # Endpoint/Request model
│   │   ├── CreateCompetitionCommand.cs   # Command definition
│   │   └── CreateCommandHandler.cs       # Handler implementation
│   └── Get/
│       └── GetCompetition.cs
├── Tournaments/
├── Events/
└── Matches/
```

Each feature slice is independent and encapsulates:
- Commands/Queries
- Handlers
- Validators
- DTOs
- Business logic

### Error Handling with ErrorOr

**IMPORTANT**: The project uses [ErrorOr](https://github.com/amantinband/error-or) for error handling. `OneOf` is deprecated and should not be used.

#### Handler Pattern

Handlers should return `ErrorOr<T>` where `T` is the success result type:

```csharp
using ErrorOr;

public static class CreateCommandHandler
{
    public static async Task<ErrorOr<Created>> Endpoint(
        CreateCompetitionCommand command,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Success case
        var competition = new Competition
        {
            CompetitionId = CompetitionId.New(),
            Name = command.Name
        };

        await dbContext.AddAsync(competition, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created;
    }
}
```

#### Common ErrorOr Return Types

- `ErrorOr<Created>` - For create operations
- `ErrorOr<Updated>` - For update operations
- `ErrorOr<Deleted>` - For delete operations
- `ErrorOr<Success>` - For generic success operations
- `ErrorOr<TResult>` - For queries returning data

#### Error Handling

```csharp
// Return validation errors
if (string.IsNullOrEmpty(command.Name))
{
    return Error.Validation("Name.Empty", "Competition name is required");
}

// Return not found errors
if (competition is null)
{
    return Error.NotFound("Competition.NotFound", "Competition not found");
}

// Return conflict errors
if (await dbContext.Competitions.AnyAsync(c => c.Name == command.Name))
{
    return Error.Conflict("Competition.Duplicate", "Competition name already exists");
}

// Return unauthorized errors
if (!user.HasPermission(Permission.CreateCompetition))
{
    return Error.Unauthorized("Competition.Unauthorized", "User not authorized");
}
```

## Project Structure

```
OpenTournament.Core/
├── Domain/
│   ├── Entities/          # Domain entities (Tournament, Competition, Event, Match)
│   └── ValueObjects/      # Strongly-typed IDs and value objects
├── Features/              # Vertical slices organized by feature
│   ├── Authentication/
│   ├── Competitions/
│   ├── Events/
│   ├── Matches/
│   ├── Registration/
│   ├── Templates/
│   └── Tournaments/
└── Infrastructure/        # Cross-cutting concerns
    ├── Persistence/
    │   ├── AppDbContext.cs
    │   ├── Configurations/
    │   └── ValueConverters/
    ├── AuthenticationServices.cs
    ├── AuthorizationServices.cs
    ├── HybridCacheService.cs
    ├── OpenTelemetryConfiguration.cs
    ├── PostgresServices.cs
    ├── RabbitMqService.cs
    └── SignalRService.cs
```

## Domain Layer

### Entities

Domain entities are located in `Domain/Entities/` and represent core business concepts:

- `Tournament` - Tournament container
- `Competition` - Competition within a tournament
- `Event` - Individual event in a competition
- `Match` - Individual match between participants
- `Participant` - User or team participating
- `Template` - Tournament template configuration

### Value Objects

Strongly-typed identifiers in `Domain/ValueObjects/` provide type safety:

- `TournamentId` - Tournament identifier
- `CompetitionId` - Competition identifier
- `EventId` - Event identifier
- `MatchId` - Match identifier
- `ParticipantId` - Participant identifier

**Benefits:**
- Type safety (can't accidentally mix up IDs)
- Self-documenting code
- Validation at construction time

```csharp
// Value object usage
var competitionId = CompetitionId.New();           // Generate new ID
var parsed = CompetitionId.TryParse(idString);     // Parse from string
```

## Infrastructure Layer

### Database (Entity Framework Core)

- **DbContext**: `AppDbContext` in `Infrastructure/Persistence/`
- **Database**: PostgreSQL
- **Configurations**: Entity configurations in `Infrastructure/Persistence/Configurations/`
- **Value Converters**: Custom type converters in `Infrastructure/Persistence/ValueConverters/`

### Services

Infrastructure services are configured and registered in the `Infrastructure/` folder:

- **Authentication**: Firebase JWT authentication (`AuthenticationServices.cs`)
- **Authorization**: Policy-based authorization (`AuthorizationServices.cs`)
- **Caching**: Hybrid cache with Redis (`HybridCacheService.cs`)
- **Database**: PostgreSQL with EF Core (`PostgresServices.cs`)
- **Messaging**: RabbitMQ integration (`RabbitMqService.cs`)
- **Observability**: OpenTelemetry (`OpenTelemetryConfiguration.cs`)
- **Real-time**: SignalR configuration (`SignalRService.cs`)

## Creating a New Feature

### 1. Create Feature Folder Structure

```bash
Features/
└── YourFeature/
    ├── Create/
    │   ├── CreateCommand.cs
    │   ├── CreateCommandHandler.cs
    │   └── CreateYourFeature.cs
    └── Get/
        ├── GetQuery.cs
        └── GetYourFeature.cs
```

### 2. Define Command/Query

```csharp
namespace OpenTournament.Core.Features.YourFeature.Create;

public sealed record CreateCommand(
    string Name,
    string Description
);
```

### 3. Implement Handler with ErrorOr

```csharp
using ErrorOr;
using OpenTournament.Core.Infrastructure.Persistence;

namespace OpenTournament.Core.Features.YourFeature.Create;

public static class CreateCommandHandler
{
    public static async Task<ErrorOr<Created>> Endpoint(
        CreateCommand command,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Error.Validation(
                "Name.Empty",
                "Name is required"
            );
        }

        // Check for duplicates
        var exists = await dbContext.YourEntities
            .AnyAsync(e => e.Name == command.Name, cancellationToken);

        if (exists)
        {
            return Error.Conflict(
                "YourEntity.Duplicate",
                $"An entity with name '{command.Name}' already exists"
            );
        }

        // Create entity
        var entity = new YourEntity
        {
            Id = YourEntityId.New(),
            Name = command.Name,
            Description = command.Description
        };

        await dbContext.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created;
    }
}
```

### 4. Validation (Optional)

For complex validation, use FluentValidation:

```csharp
using FluentValidation;

public class CreateCommandValidator : AbstractValidator<CreateCommand>
{
    public CreateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(3, 100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
```

## Dependencies

The project targets **.NET 10.0** and uses the following key packages:

```xml
<PackageReference Include="ErrorOr" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
<PackageReference Include="CouchPartyGames.TournamentGenerator" />
```

### Key Libraries

- **ErrorOr** - Functional error handling (replaces OneOf)
- **Entity Framework Core** - ORM and database access
- **Npgsql** - PostgreSQL provider
- **JWT Bearer** - Authentication
- **Tournament Generator** - Tournament bracket generation

## Best Practices

### 1. Use ErrorOr for All Handlers

Always return `ErrorOr<T>` from handlers instead of throwing exceptions for business logic errors:

```csharp
// ✅ Good
public static async Task<ErrorOr<Competition>> Endpoint(...)
{
    if (competition is null)
        return Error.NotFound("Competition.NotFound", "Competition not found");
}

// ❌ Bad (don't throw for business logic errors)
public static async Task<Competition> Endpoint(...)
{
    if (competition is null)
        throw new NotFoundException("Competition not found");
}
```

### 2. Use Strongly-Typed IDs

Always use value objects for IDs:

```csharp
// ✅ Good
var tournamentId = TournamentId.New();
var competition = await dbContext.Competitions
    .FirstOrDefaultAsync(c => c.Id == competitionId);

// ❌ Bad
var tournamentId = Guid.NewGuid();
```

### 3. Keep Features Independent

Each feature should be self-contained and not depend on other features directly.

### 4. Use Async/Await Consistently

Always use async methods and pass CancellationToken:

```csharp
public static async Task<ErrorOr<T>> Endpoint(
    Command command,
    AppDbContext dbContext,
    CancellationToken cancellationToken)  // ✅ Always include
{
    await dbContext.SaveChangesAsync(cancellationToken);
}
```

### 5. Meaningful Error Messages

Provide clear, actionable error messages:

```csharp
// ✅ Good
return Error.Validation(
    "Tournament.MaxParticipants",
    "Max participants must be greater than min participants"
);

// ❌ Bad
return Error.Validation("Invalid", "Error");
```

## Testing

When writing tests for handlers:

```csharp
[Fact]
public async Task Endpoint_ValidCommand_ReturnsCreated()
{
    // Arrange
    var command = new CreateCommand("Test Competition", "Description");
    var dbContext = CreateInMemoryDbContext();
    var cancellationToken = CancellationToken.None;

    // Act
    var result = await CreateCommandHandler.Endpoint(
        command,
        dbContext,
        cancellationToken
    );

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Should().BeOfType<Created>();
}

[Fact]
public async Task Endpoint_EmptyName_ReturnsValidationError()
{
    // Arrange
    var command = new CreateCommand("", "Description");
    var dbContext = CreateInMemoryDbContext();
    var cancellationToken = CancellationToken.None;

    // Act
    var result = await CreateCommandHandler.Endpoint(
        command,
        dbContext,
        cancellationToken
    );

    // Assert
    result.IsError.Should().BeTrue();
    result.FirstError.Type.Should().Be(ErrorType.Validation);
}
```

## Migration from OneOf

If you encounter legacy code using `OneOf`, migrate to `ErrorOr`:

### Before (OneOf - Deprecated)
```csharp
public static async Task<OneOf<Created, ValidationProblem>> Endpoint(...)
{
    return new Created();
}
```

### After (ErrorOr - Current)
```csharp
public static async Task<ErrorOr<Created>> Endpoint(...)
{
    return Result.Created;
}
```

## Related Documentation

- See [OpenTournament main README](../../../README.md) for overall project documentation
- See [OpenTournament.Api README](../OpenTournament.Api/README.md) for API-specific documentation

## License

Part of the OpenTournament project.
