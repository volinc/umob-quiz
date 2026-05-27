namespace UmobQuiz.Api.Configuration;

public sealed class GbfsProviderOptions
{
    public const string SectionName = "GbfsProviders";

    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string GbfsUrl { get; init; }
}
