using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;
using AgonesTournament.Sdk.Organizer;

namespace AgonesTournament.Sdk.Tests;

public class OpenApiContractTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private static readonly Dictionary<Type, string> Schemas = new()
    {
        [typeof(NewTournament)] = "NewTournament",
        [typeof(TournamentSettings)] = "TournamentSettings",
        [typeof(StageSettings)] = "StageSettings",
        [typeof(Match)] = "ServerMatchView",
        [typeof(Tournament)] = "TournamentView",
        [typeof(TournamentPage)] = "ListOutputBody",
        [typeof(CheckInSettings)] = "CheckInSettings",
        [typeof(ConfiguredStage)] = "ConfiguredStageView",
        [typeof(PlayerIdentity)] = "Identity",
        [typeof(RegisteredParticipant)] = "ParticipantView",
        [typeof(ParticipantRegistrations)] = "ParticipantRegistrations",
        [typeof(TournamentStructure)] = "StructureView",
        [typeof(Stage)] = "StageView",
        [typeof(Group)] = "GroupView",
        [typeof(GroupParticipant)] = "GroupParticipantView",
        [typeof(Round)] = "RoundView",
        [typeof(Standing)] = "StandingView",
        [typeof(TournamentMatch)] = "MatchView",
        [typeof(MatchDetails)] = "MatchDetailView",
        [typeof(FinalPlacement)] = "PlacementView",
        [typeof(FinalPlacements)] = "PlacementsView",
        [typeof(Participant)] = "ServerParticipantView",
        [typeof(Bout)] = "BoutView",
        [typeof(BoutResult)] = "BoutResultView",
        [typeof(BoutPlacement)] = "BoutPlacement",
        [typeof(ProblemDetails)] = "Details",
        [typeof(FieldError)] = "FieldError"
    };

    [Fact]
    public void ResponseAndSharedModelsMatchEveryContractPropertyTypeAndRequiredField()
    {
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        var schemas = contract.RootElement.GetProperty("components").GetProperty("schemas");
        foreach (var (type, schemaName) in Schemas)
        {
            var schema = schemas.GetProperty(schemaName);
            var properties = schema.GetProperty("properties");
            var metadata = Json.GetTypeInfo(type);
            Assert.Equal(properties.EnumerateObject().Select(p => p.Name).Where(n => n != "$schema").Order(),
                metadata.Properties.Select(p => p.Name).Order());
            Assert.Equal(schema.GetProperty("required").EnumerateArray().Select(p => p.GetString()).Order(),
                metadata.Properties.Where(p => p.IsRequired).Select(p => p.Name).Order());
            foreach (var property in metadata.Properties)
            {
                var propertySchema = properties.GetProperty(property.Name);
                AssertPropertyType(property.PropertyType, propertySchema);
                if (property.IsRequired && propertySchema.TryGetProperty("type", out _))
                {
                    var nullability = new NullabilityInfoContext().Create((PropertyInfo)property.AttributeProvider!);
                    Assert.Equal(SchemaTypes(propertySchema).Contains("null"), nullability.ReadState == NullabilityState.Nullable);
                }
            }
        }
    }

    [Fact]
    public async Task EveryGameServerOperationUsesContractPathsMethodsSecurityAndRequestSchemas()
    {
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        var root = contract.RootElement;
        var paths = root.GetProperty("paths");
        var operations = new HashSet<string>();
        using var fakeHttp = new HttpHandler(async (request, cancellationToken) =>
        {
            var path = request.RequestUri!.AbsolutePath.Replace("/bouts/1", "/bouts/{bout}");
            var method = request.Method.Method.ToLowerInvariant();
            var operation = paths.GetProperty(path).GetProperty(method);
            operations.Add(method + " " + path);
            Assert.Equal("Bearer assigned-token", request.Headers.Authorization!.ToString());
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("matchToken", out _));
            var responses = operation.GetProperty("responses");
            Assert.Equal("#/components/schemas/Details", responses.GetProperty("default").GetProperty("content")
                .GetProperty("application/problem+json").GetProperty("schema").GetProperty("$ref").GetString());
            if (method == "get")
            {
                Assert.Equal("#/components/schemas/ServerMatchView", responses.GetProperty("200").GetProperty("content")
                    .GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString());
                return HttpHandler.Json("""
                    {"matchId":"11111111-1111-1111-1111-111111111111","tournamentId":"22222222-2222-2222-2222-222222222222",
                     "gameId":"marbles","status":"ready","format":"free-for-all","bouts":2,"participants":null,"completedBouts":null}
                    """);
            }
            Assert.True(responses.TryGetProperty("204", out _));
            if (operation.TryGetProperty("requestBody", out var requestBody))
            {
                Assert.NotNull(request.Content);
                Assert.Equal("application/json", request.Content.Headers.ContentType!.MediaType);
                using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(cancellationToken));
                ValidateJson(body.RootElement, requestBody.GetProperty("content").GetProperty("application/json").GetProperty("schema"), root);
            }
            else
                Assert.Null(request.Content);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        using var http = new HttpClient(fakeHttp);
        var client = new GameServerClient(new TournamentHttpClient(http, new Uri("https://example.com")), "assigned-token");
        var match = await client.MatchAsync();
        Assert.Equal(MatchFormat.FreeForAll, match.Format);
        Assert.Null(match.BestOf);
        Assert.Equal(2, match.Bouts);
        Assert.Null(match.Participants);
        Assert.Null(match.CompletedBouts);
        var participant = new ParticipantId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        await client.ReportStartedAsync();
        await client.ReportWinnerAsync(1, participant);
        await client.ReportPlacementsAsync(1, [new BoutPlacement(participant, 1, -10)]);
        await client.ReportNoShowsAsync(1, [participant]);
        Assert.Equal(paths.EnumerateObject().Where(p => p.Name.StartsWith("/api/v1/game-server/", StringComparison.Ordinal))
            .SelectMany(p => p.Value.EnumerateObject().Select(m => m.Name + " " + p.Name)).Order(), operations.Order());
    }

    [Fact]
    public async Task EveryGameClientOperationMatchesContractPathsSecurityBodiesAndResponses()
    {
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        var root = contract.RootElement;
        const string tournamentJson = """
            {"id":"22222222-2222-2222-2222-222222222222","gameId":"marbles","name":"Weekly Tournament",
             "organizer":"user:anna","status":"registration-open","startsAt":"2026-10-01T12:00:00Z",
             "registrationOpensAt":"2026-10-01T11:00:00Z","capacity":16,"minimumParticipants":2,
             "checkIn":{"enabled":true,"windowSeconds":60},"stages":[{"id":"55555555-5555-5555-5555-555555555555",
             "position":1,"status":"pending","format":"single-elimination","bestOf":3,"resultDeadlineSeconds":300}],
             "registered":1,"checkedIn":0,"createdAt":"2026-09-30T12:00:00Z","updatedAt":"2026-09-30T12:00:00Z"}
            """;
        const string participantJson = """
            {"id":"33333333-3333-3333-3333-333333333333","identity":{"kind":"keycloak","value":"anna"},
             "status":"registered","registeredAt":"2026-10-01T11:00:00Z"}
            """;
        const string matchJson = """
            {"id":"11111111-1111-1111-1111-111111111111","tournamentId":"22222222-2222-2222-2222-222222222222",
             "groupId":"44444444-4444-4444-4444-444444444444","key":"R1-M1","round":1,"status":"completed",
             "participants":["33333333-3333-3333-3333-333333333333"],
             "bouts":[{"bout":1,"results":[{"participantId":"33333333-3333-3333-3333-333333333333","won":true}]}],
             "aborts":0,"serverAllocated":false,"result":"win","winnerId":"33333333-3333-3333-3333-333333333333"}
            """;
        var fixtures = new Dictionary<string, string>
        {
            ["list-tournaments"] = "{\"tournaments\":[" + tournamentJson + "]}",
            ["get-tournament"] = tournamentJson,
            ["register"] = participantJson,
            ["check-in"] = participantJson.Replace("registered\"", "checked-in\""),
            ["my-registrations"] = "{\"participants\":[" + participantJson + "]}",
            ["get-match"] = matchJson,
            ["get-structure"] = """
                {"tournamentId":"22222222-2222-2222-2222-222222222222","status":"running",
                 "stages":[{"id":"55555555-5555-5555-5555-555555555555","position":1,"format":"single-elimination","status":"running",
                 "groups":[{"id":"44444444-4444-4444-4444-444444444444","position":1,"status":"running",
                 "participants":[{"participantId":"33333333-3333-3333-3333-333333333333","seed":1}],
                 "standings":[{"participantId":"33333333-3333-3333-3333-333333333333","position":1,"played":1,"wins":1,"losses":0,"points":3}],
                 "rounds":[{"round":1,"matches":[
                """ + matchJson + "]}]}]}]}",
            ["get-placements"] = """
                {"tournamentId":"22222222-2222-2222-2222-222222222222","status":"completed",
                 "placements":[{"participantId":"33333333-3333-3333-3333-333333333333",
                 "identity":{"kind":"keycloak","value":"anna"},"from":1,"to":1}]}
                """
        };
        var seen = new HashSet<string>();
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath
                .Replace("22222222-2222-2222-2222-222222222222", "{tournamentId}")
                .Replace("33333333-3333-3333-3333-333333333333", "{participantId}")
                .Replace("11111111-1111-1111-1111-111111111111", "{matchId}");
            var operation = root.GetProperty("paths").GetProperty(path).GetProperty(request.Method.Method.ToLowerInvariant());
            var name = operation.GetProperty("operationId").GetString()!;
            seen.Add(name);
            if (operation.TryGetProperty("security", out var security))
            {
                Assert.True(security[0].TryGetProperty("keycloak", out _));
                Assert.Equal("Bearer keycloak-token", request.Headers.Authorization!.ToString());
            }
            else
                Assert.Null(request.Headers.Authorization);
            if (operation.TryGetProperty("parameters", out var parameters))
            {
                var queries = request.RequestUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                foreach (var query in queries)
                {
                    var parts = query.Split('=');
                    var parameter = Assert.Single(parameters.EnumerateArray(), p => p.GetProperty("name").GetString() == parts[0]);
                    var schema = parameter.GetProperty("schema");
                    using var value = JsonDocument.Parse(schema.GetProperty("type").GetString() == "integer"
                        ? parts[1] : JsonSerializer.Serialize(Uri.UnescapeDataString(parts[1])));
                    ValidateJson(value.RootElement, schema, root);
                }
            }
            if (operation.TryGetProperty("requestBody", out var requestBody))
            {
                using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
                ValidateJson(body.RootElement, requestBody.GetProperty("content").GetProperty("application/json").GetProperty("schema"), root);
            }
            else
                Assert.Null(request.Content);
            var response = Assert.Single(operation.GetProperty("responses").EnumerateObject(), p => p.Name.StartsWith('2'));
            if (response.Name == "204")
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var fixture = fixtures[name];
            using var fixtureJson = JsonDocument.Parse(fixture);
            ValidateJson(fixtureJson.RootElement, response.Value.GetProperty("content").GetProperty("application/json").GetProperty("schema"), root);
            return HttpHandler.Json(fixture, (HttpStatusCode)int.Parse(response.Name));
        });
        using var http = new HttpClient(fakeHttp);
        var transport = new TournamentHttpClient(http, new Uri("https://tournament.example"));
        var anonymous = new AgonesTournament.Sdk.GameClient.GameClient(transport);
        var signedIn = new AgonesTournament.Sdk.GameClient.GameClient(transport, _ => Task.FromResult("keycloak-token"));
        var tournamentId = new TournamentId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var participantId = new ParticipantId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        Assert.Equal("marbles", Assert.Single((await anonymous.ListTournamentsAsync("marbles", TournamentStatus.RegistrationOpen)).Tournaments!).GameId);
        Assert.Equal(3, Assert.Single((await anonymous.TournamentAsync(tournamentId)).Stages!).BestOf);
        var structure = await anonymous.StructureAsync(tournamentId);
        var group = Assert.Single(Assert.Single(structure.Stages!).Groups!);
        Assert.Equal(3, Assert.Single(group.Standings!).Points);
        var round = Assert.Single(group.Rounds!);
        Assert.Equal(1, round.Number);
        Assert.True(Assert.Single(Assert.Single(Assert.Single(round.Matches!).Bouts!).Results!).Won);
        Assert.Equal(1, Assert.Single((await anonymous.FinalPlacementsAsync(tournamentId)).Placements!).From);
        Assert.Equal(MatchResult.Win, (await anonymous.MatchAsync(new MatchId(Guid.Parse("11111111-1111-1111-1111-111111111111")))).Result);
        Assert.Equal(participantId, (await signedIn.RegisterAsync(tournamentId)).Id);
        await signedIn.RegisterAsync(tournamentId, new PlayerIdentity { Kind = "steam", Value = "external-id" });
        Assert.Equal(ParticipantStatus.CheckedIn, (await signedIn.CheckInAsync(tournamentId, participantId)).Status);
        Assert.Equal("anna", Assert.Single((await signedIn.MyRegistrationsAsync(tournamentId)).Participants!).Identity.Value);
        await signedIn.UnregisterAsync(tournamentId, participantId);
        Assert.Equal(new[] { "check-in", "get-match", "get-placements", "get-structure", "get-tournament",
            "list-tournaments", "my-registrations", "register", "unregister" }.Order(), seen.Order());
    }

    [Fact]
    public async Task EveryOrganizerOperationMatchesContractPathsSecurityBodiesAndResponses()
    {
        using var contract = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "openapi.json")));
        var root = contract.RootElement;
        var seen = new HashSet<string>();
        var fixtures = new Dictionary<string, string>
        {
            ["get-structure"] = """
                {"tournamentId":"22222222-2222-2222-2222-222222222222","status":"running","stages":null}
                """,
            ["get-placements"] = """
                {"tournamentId":"22222222-2222-2222-2222-222222222222","status":"completed",
                 "placements":[{"participantId":"33333333-3333-3333-3333-333333333333","identity":{"kind":"steam","value":"external-id"},"from":5,"to":8}]}
                """,
            ["get-match"] = """
                {"id":"11111111-1111-1111-1111-111111111111","tournamentId":"22222222-2222-2222-2222-222222222222",
                 "groupId":"44444444-4444-4444-4444-444444444444","key":"R1-M1","round":1,"status":"allocating",
                 "participants":["33333333-3333-3333-3333-333333333333"],"bouts":null,"aborts":0,"serverAllocated":true,
                 "serverAddress":"192.0.2.1","serverPort":7777}
                """,
            ["resolve-stalled-match"] = """
                {"id":"11111111-1111-1111-1111-111111111111","tournamentId":"22222222-2222-2222-2222-222222222222",
                 "groupId":"44444444-4444-4444-4444-444444444444","key":"R1-M1","round":1,"status":"completed",
                 "participants":["33333333-3333-3333-3333-333333333333"],"bouts":null,"aborts":0,"serverAllocated":false,"result":"win"}
                """,
            ["create-tournament"] = OrganizerClientTests.TournamentJson,
            ["edit-tournament"] = OrganizerClientTests.TournamentJson,
            ["get-tournament"] = OrganizerClientTests.TournamentJson,
            ["cancel-tournament"] = OrganizerClientTests.TournamentJson.Replace("draft", "cancelled"),
            ["list-tournaments"] = "{\"tournaments\":[" + OrganizerClientTests.TournamentJson + "]}",
            ["list-participants"] = "{\"participants\":[" + OrganizerClientTests.ParticipantJson + "]}",
            ["register"] = OrganizerClientTests.ParticipantJson,
            ["check-in"] = OrganizerClientTests.ParticipantJson.Replace("registered\"", "checked-in\"")
        };
        using var fakeHttp = new HttpHandler(async (request, ct) =>
        {
            var path = request.RequestUri!.AbsolutePath
                .Replace("22222222-2222-2222-2222-222222222222", "{tournamentId}")
                .Replace("33333333-3333-3333-3333-333333333333", "{participantId}")
                .Replace("11111111-1111-1111-1111-111111111111", "{matchId}");
            var operation = root.GetProperty("paths").GetProperty(path).GetProperty(request.Method.Method.ToLowerInvariant());
            var name = operation.GetProperty("operationId").GetString()!;
            seen.Add(name);
            Assert.Equal("Bearer backend-token", request.Headers.Authorization!.ToString());
            if (operation.TryGetProperty("security", out var security))
                Assert.True(security[0].TryGetProperty("keycloak", out _));
            Assert.Equal("#/components/schemas/Details", operation.GetProperty("responses").GetProperty("default")
                .GetProperty("content").GetProperty("application/problem+json").GetProperty("schema").GetProperty("$ref").GetString());
            if (operation.TryGetProperty("parameters", out var parameters))
                foreach (var query in request.RequestUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = query.Split('=');
                    var parameter = Assert.Single(parameters.EnumerateArray(), p => p.GetProperty("name").GetString() == parts[0]);
                    var schema = parameter.GetProperty("schema");
                    using var value = JsonDocument.Parse(schema.GetProperty("type").GetString() == "integer"
                        ? parts[1] : JsonSerializer.Serialize(Uri.UnescapeDataString(parts[1])));
                    ValidateJson(value.RootElement, schema, root);
                }
            if (operation.TryGetProperty("requestBody", out var requestBody))
            {
                Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
                using var body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(ct));
                ValidateJson(body.RootElement, requestBody.GetProperty("content").GetProperty("application/json").GetProperty("schema"), root);
            }
            else
                Assert.Null(request.Content);
            var response = Assert.Single(operation.GetProperty("responses").EnumerateObject(), p => p.Name.StartsWith('2'));
            if (response.Name == "204")
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var fixture = fixtures[name];
            using var fixtureJson = JsonDocument.Parse(fixture);
            ValidateJson(fixtureJson.RootElement, response.Value.GetProperty("content").GetProperty("application/json").GetProperty("schema"), root);
            return HttpHandler.Json(fixture, (HttpStatusCode)int.Parse(response.Name));
        });
        using var http = new HttpClient(fakeHttp);
        var client = new OrganizerClient(new TournamentHttpClient(http, new Uri("https://tournament.example")),
            _ => Task.FromResult("backend-token"));
        var tournamentId = new TournamentId(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        var participantId = new ParticipantId(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var matchId = new MatchId(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var settings = OrganizerClientTests.NewTournament() with
        {
            CheckIn = new CheckInSettings { Enabled = true, WindowSeconds = 300 },
            MinimumParticipants = 8,
            Stages = [
                new StageSettings { Format = MatchFormat.FreeForAll, Bouts = 2, Groups = 2, Advancement = 2, ResultDeadlineSeconds = 600 },
                new StageSettings { Format = MatchFormat.Swiss, BestOf = 3, SwissRounds = 3, Advancement = 2, ResultDeadlineSeconds = 600 },
                new StageSettings { Format = MatchFormat.SingleElimination, BestOf = 1, ResultDeadlineSeconds = 600 }]
        };
        Assert.Equal(tournamentId, (await client.CreateTournamentAsync(settings)).Id);
        await client.EditTournamentAsync(tournamentId, settings);
        Assert.Equal(tournamentId, (await client.TournamentAsync(tournamentId)).Id);
        Assert.Single((await client.ListTournamentsAsync("marbles", TournamentStatus.Draft)).Tournaments!);
        Assert.Equal(participantId, Assert.Single((await client.ListParticipantsAsync(tournamentId)).Participants!).Id);
        await client.RegisterAsync(tournamentId, new PlayerIdentity { Kind = "steam", Value = "external-id" });
        Assert.Equal(ParticipantStatus.CheckedIn, (await client.CheckInAsync(tournamentId, participantId)).Status);
        await client.DisqualifyAsync(tournamentId, participantId);
        Assert.Equal(TournamentStatus.Running, (await client.StructureAsync(tournamentId)).Status);
        var placements = Assert.Single((await client.FinalPlacementsAsync(tournamentId)).Placements!);
        Assert.Equal(5, placements.From);
        Assert.Equal(8, placements.To);
        var match = await client.MatchAsync(matchId);
        Assert.Equal("192.0.2.1", match.ServerAddress);
        Assert.Equal(7777, match.ServerPort);
        Assert.Equal(MatchStatus.Completed, (await client.ResolveWinnerAsync(matchId, participantId)).Status);
        Assert.Equal(MatchStatus.Completed, (await client.ResolveDoubleForfeitAsync(matchId)).Status);
        Assert.Equal(TournamentStatus.Cancelled, (await client.CancelTournamentAsync(tournamentId)).Status);
        Assert.Equal(new[] { "create-tournament", "edit-tournament", "get-tournament", "list-tournaments", "cancel-tournament",
            "list-participants", "register", "check-in", "disqualify", "get-structure", "get-placements", "get-match", "resolve-stalled-match" }.Order(), seen.Order());
    }

    private static void AssertPropertyType(Type type, JsonElement schema)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (Schemas.TryGetValue(type, out var name))
        {
            Assert.Equal("#/components/schemas/" + name, schema.GetProperty("$ref").GetString());
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            Assert.Contains("array", SchemaTypes(schema));
            AssertPropertyType(type.GetGenericArguments()[0], schema.GetProperty("items"));
            return;
        }
        if (type == typeof(JsonElement)) // FieldError.value permits arbitrary JSON.
        {
            Assert.False(schema.TryGetProperty("type", out _));
            return;
        }
        var expectedType = type == typeof(bool) ? "boolean" : type == typeof(int) || type == typeof(long) ? "integer" : "string";
        Assert.Contains(expectedType, SchemaTypes(schema));
        if (type == typeof(int) || type == typeof(long))
            Assert.Equal(type == typeof(int) ? "int32" : "int64", schema.GetProperty("format").GetString());
        if (type == typeof(MatchId) || type == typeof(TournamentId) || type == typeof(ParticipantId) || type == typeof(StageId) || type == typeof(GroupId))
            Assert.Equal("uuid", schema.GetProperty("format").GetString());
        if (type == typeof(DateTimeOffset))
            Assert.Equal("date-time", schema.GetProperty("format").GetString());
        if (type.IsEnum)
            Assert.Equal(schema.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).Order(),
                Enum.GetValues(type).Cast<object>().Select(value => JsonSerializer.SerializeToElement(value, type, Json).GetString()).Order());
    }

    private static IEnumerable<string?> SchemaTypes(JsonElement schema)
    {
        var types = schema.GetProperty("type");
        return types.ValueKind == JsonValueKind.Array ? types.EnumerateArray().Select(t => t.GetString()) : [types.GetString()];
    }

    // Only the JSON Schema vocabulary used by these request bodies is needed here.
    private static void ValidateJson(JsonElement value, JsonElement schema, JsonElement root)
    {
        if (schema.TryGetProperty("$ref", out var reference))
            schema = root.GetProperty("components").GetProperty("schemas").GetProperty(reference.GetString()!.Split('/')[^1]);
        var type = value.ValueKind switch
        {
            JsonValueKind.Object => "object", JsonValueKind.Array => "array", JsonValueKind.String => "string",
            JsonValueKind.Number => "integer", JsonValueKind.Null => "null", _ => "boolean"
        };
        Assert.Contains(type, SchemaTypes(schema));
        if (schema.TryGetProperty("enum", out var values))
            Assert.Contains(values.EnumerateArray(), item => item.GetRawText() == value.GetRawText());
        if (type == "object")
        {
            var properties = schema.GetProperty("properties");
            if (schema.TryGetProperty("required", out var required))
                foreach (var property in required.EnumerateArray())
                    Assert.True(value.TryGetProperty(property.GetString()!, out _), $"Missing {property}");
            foreach (var property in value.EnumerateObject())
            {
                Assert.True(properties.TryGetProperty(property.Name, out var propertySchema), $"Unexpected {property.Name}");
                ValidateJson(property.Value, propertySchema, root);
            }
        }
        else if (type == "array")
        {
            if (schema.TryGetProperty("minItems", out var minimum))
                Assert.True(value.GetArrayLength() >= minimum.GetInt32());
            if (schema.TryGetProperty("maxItems", out var maximum))
                Assert.True(value.GetArrayLength() <= maximum.GetInt32());
            if (schema.TryGetProperty("uniqueItems", out var unique) && unique.GetBoolean())
                Assert.Equal(value.GetArrayLength(), value.EnumerateArray().Select(v => v.GetRawText()).Distinct().Count());
            foreach (var item in value.EnumerateArray())
                ValidateJson(item, schema.GetProperty("items"), root);
        }
        else if (type == "string")
        {
            var text = value.GetString()!;
            if (schema.TryGetProperty("minLength", out var minimum))
                Assert.True(text.Length >= minimum.GetInt32());
            if (schema.TryGetProperty("maxLength", out var maximum))
                Assert.True(text.Length <= maximum.GetInt32());
            if (schema.TryGetProperty("format", out var format))
            {
                if (format.GetString() == "uuid")
                    Assert.True(Guid.TryParse(text, out _));
                if (format.GetString() == "date-time")
                    Assert.True(DateTimeOffset.TryParse(text, out _));
            }
        }
        else if (type == "integer")
        {
            Assert.True(value.TryGetInt32(out var number));
            if (schema.TryGetProperty("minimum", out var minimum))
                Assert.True(number >= minimum.GetInt32());
            if (schema.TryGetProperty("maximum", out var maximum))
                Assert.True(number <= maximum.GetInt32());
        }
    }
}
