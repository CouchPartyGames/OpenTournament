using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using AgonesTournament.Sdk.Core;
using AgonesTournament.Sdk.GameServer;

namespace AgonesTournament.Sdk.Tests;

public class OpenApiContractTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private static readonly Dictionary<Type, string> Schemas = new()
    {
        [typeof(Match)] = "ServerMatchView",
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
                AssertPropertyType(property.PropertyType, properties.GetProperty(property.Name));
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
        if (type == typeof(MatchId) || type == typeof(TournamentId) || type == typeof(ParticipantId))
            Assert.Equal("uuid", schema.GetProperty("format").GetString());
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
            if (schema.TryGetProperty("uniqueItems", out var unique) && unique.GetBoolean())
                Assert.Equal(value.GetArrayLength(), value.EnumerateArray().Select(v => v.GetRawText()).Distinct().Count());
            foreach (var item in value.EnumerateArray())
                ValidateJson(item, schema.GetProperty("items"), root);
        }
        else if (type == "string" && schema.TryGetProperty("format", out var format) && format.GetString() == "uuid")
            Assert.True(Guid.TryParse(value.GetString(), out _));
        else if (type == "integer")
        {
            Assert.True(value.TryGetInt32(out var number));
            if (schema.TryGetProperty("minimum", out var minimum))
                Assert.True(number >= minimum.GetInt32());
        }
    }
}
