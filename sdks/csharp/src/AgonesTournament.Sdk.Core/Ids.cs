using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgonesTournament.Sdk.Core;

/// <summary>The UUID of one Match.</summary>
/// <param name="Value">The UUID stored by the service.</param>
[JsonConverter(typeof(MatchIdConverter))]
public readonly record struct MatchId(Guid Value)
{
    /// <summary>Returns the UUID in its canonical hyphenated form.</summary>
    public override string ToString() => Value.ToString("D");
}

/// <summary>The UUID of one Tournament.</summary>
/// <param name="Value">The UUID stored by the service.</param>
[JsonConverter(typeof(TournamentIdConverter))]
public readonly record struct TournamentId(Guid Value)
{
    /// <summary>Returns the UUID in its canonical hyphenated form.</summary>
    public override string ToString() => Value.ToString("D");
}

/// <summary>The UUID of one Participant in a Tournament.</summary>
/// <param name="Value">The UUID stored by the service.</param>
[JsonConverter(typeof(ParticipantIdConverter))]
public readonly record struct ParticipantId(Guid Value)
{
    /// <summary>Returns the UUID in its canonical hyphenated form.</summary>
    public override string ToString() => Value.ToString("D");
}

internal abstract class UuidConverter<T>(Func<Guid, T> create, Func<T, Guid> value) : JsonConverter<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => create(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, T id, JsonSerializerOptions options)
        => writer.WriteStringValue(value(id));
}

internal sealed class MatchIdConverter() : UuidConverter<MatchId>(id => new(id), id => id.Value);
internal sealed class TournamentIdConverter() : UuidConverter<TournamentId>(id => new(id), id => id.Value);
internal sealed class ParticipantIdConverter() : UuidConverter<ParticipantId>(id => new(id), id => id.Value);

/// <summary>The UUID of one Stage.</summary>
/// <param name="Value">The UUID stored by the service.</param>
[JsonConverter(typeof(StageIdConverter))]
public readonly record struct StageId(Guid Value)
{
    /// <summary>Returns the UUID in its canonical hyphenated form.</summary>
    public override string ToString() => Value.ToString("D");
}

internal sealed class StageIdConverter() : UuidConverter<StageId>(id => new(id), id => id.Value);

/// <summary>The UUID of one Group.</summary>
/// <param name="Value">The UUID stored by the service.</param>
[JsonConverter(typeof(GroupIdConverter))]
public readonly record struct GroupId(Guid Value)
{
    /// <summary>Returns the UUID in its canonical hyphenated form.</summary>
    public override string ToString() => Value.ToString("D");
}

internal sealed class GroupIdConverter() : UuidConverter<GroupId>(id => new(id), id => id.Value);
