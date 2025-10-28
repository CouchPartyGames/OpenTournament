namespace OpenTournament.Core.Infrastructure;

public sealed class HybridCacheOptions
{
    public const string SectionName = "HybridCache";
    
    public const int MaximumKeyLength = 256;
    
    public int MaxKeyLength { get; set; } = 64;

    public int MaxPayloadBytes { get; set; } = 1024;
}