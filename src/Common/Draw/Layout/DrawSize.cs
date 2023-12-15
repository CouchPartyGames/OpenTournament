using System.Numerics;

namespace OpenTournament.Common.Draw.Layout;

public sealed class InvalidDrawSizeException(string message) : Exception(message);

public sealed record DrawSize
{
    public int Value { get; }

    private DrawSize(int value) => Value = value;
    
    public static DrawSize FromNumParticipants(int numParticipants)
    {
        int size = (int)BitOperations.RoundUpToPowerOf2((uint)numParticipants);
        var good = size switch
        {
            2 or 4 or 8 or 16 or 32 or 64 or 128 => size,
            _ => throw new InvalidDrawSizeException($"Unable to handle draw of size: ${size}")
        };
        return new DrawSize(good);
    }

    public int ToTotalRounds() => Value switch
    {
        1 => 1,
        2 => 2,
        4 => 3,
        8 => 4,
        16 => 5,
        32 => 6,
        64 => 7,
        128 => 8
    };
} 