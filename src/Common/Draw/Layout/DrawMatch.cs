namespace OpenTournament.Common.Draw.Layout;

public sealed class DrawMatch
{
    public int Id { get; } 
    public int Round { get; } 
    public int WinProgression { get; private set; } = -1;

    public int Opponent1 { get; } = -1;

    public int Opponent2 { get; } = -1;

    private const int NoProgression = -1;

    public DrawMatch(int id, int round) => (Id, Round) = (id, round);
    public DrawMatch(int id, int round, int winProgression) => (Id, Round, WinProgression) = (id, round, winProgression);
    public DrawMatch(int id, int winProgression, int opp1, int opp2) => (Id, Round, WinProgression, Opponent1, Opponent2) = (id, 1, winProgression, opp1, opp2);
   
    public void SetProgression(int progression) => WinProgression = progression;

    public override string ToString()
    {
        if (Opponent1 != -1 && Opponent2 != -1)
        {
            return $"Round: {Round} Id: {Id} Next: {WinProgression}  - {Opponent1} vs {Opponent2}";
        }
        return $"Round: {Round} Id: {Id} Next: {WinProgression}";
    }
}