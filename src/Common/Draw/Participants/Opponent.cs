namespace OpenTournament.Common.Draw.Participants;

public sealed record Opponent(string Id, int Rank)
{
    public static Opponent CreateBye()
    {
        return new Opponent("99999", 99999);
    }
};

public sealed record OpponentOrder(int Value);
