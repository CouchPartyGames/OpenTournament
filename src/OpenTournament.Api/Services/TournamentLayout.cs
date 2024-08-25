using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Services;

public static class TournamentLayout
{
    public static IServiceCollection AddTournamentLayouts(this IServiceCollection services)
    {
        Dictionary<DrawSize.Size, string> combos = new()
        {
            { DrawSize.Size.Size4, LayoutConstants.Single4 },
            { DrawSize.Size.Size8, LayoutConstants.Single8 },
            { DrawSize.Size.Size16, LayoutConstants.Single16 },
            { DrawSize.Size.Size32, LayoutConstants.Single32 },
            { DrawSize.Size.Size64, LayoutConstants.Single64 },
            { DrawSize.Size.Size128, LayoutConstants.Single128 },
        };

        foreach (var item in combos)
        {
            services.AddKeyedSingleton(item.Value,
                new SingleEliminationDraw(
                    new FirstRoundPositions(DrawSize.Create(item.Key))));
        }
        
        return services;
    }
    
}

public static class LayoutConstants
{
    public const string Single4 = "Single4";
    public const string Single8 = "Single8";
    public const string Single16 = "Single16";
    public const string Single32 = "Single32";
    public const string Single64 = "Single64";
    public const string Single128 = "Single128";
    
    public const string Double4 = "Double4";
    public const string Double8 = "Double8";
    public const string Double16 = "Double16";
    public const string Double32 = "Double32";
    public const string Double64 = "Double64";
    public const string Double128 = "Double128";
}