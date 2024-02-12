using OpenTournament.Common.Draw.Layout;

namespace OpenTournament.Configurations;

public static class Layout
{
    public static IServiceCollection AddTournamentLayouts(this IServiceCollection services)
    {
        services.AddKeyedSingleton(LayoutConstants.Single4,
            new SingleEliminationDraw(
                new ParticipantPositions(
                    DrawSize.Create(DrawSize.Size.Size4)), 
                    DrawSize.Create(DrawSize.Size.Size4)));
        
        services.AddKeyedSingleton(LayoutConstants.Single8,
            new SingleEliminationDraw(
                new ParticipantPositions(
                    DrawSize.Create(DrawSize.Size.Size8)), 
                    DrawSize.Create(DrawSize.Size.Size8)));
        
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
    public const string Single256 = "Single256";
    
    public const string Double4 = "Double4";
    public const string Double8 = "Double8";
    public const string Double16 = "Double16";
    public const string Double32 = "Double32";
    public const string Double64 = "Double64";
    public const string Double128 = "Double128";
    public const string Double256 = "Double256";
}