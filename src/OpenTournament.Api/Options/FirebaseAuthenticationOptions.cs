namespace OpenTournament.Api.Options;

public sealed class FirebaseAuthenticationOptions
{
    public const string SectionName = "Authentication:Firebase";

    public bool Enabled { get; init; } = false;
    
    public string Authority { get; init; } = String.Empty;
    
    public string Issuer { get; init; } = String.Empty;

    public string Audience { get; init; } = String.Empty;
}

/*
[OptionsValidator]
public partial class FirebaseAuthenticationOptionsValidator : IValidateOptions<FirebaseAuthenticationOptions> {}
*/