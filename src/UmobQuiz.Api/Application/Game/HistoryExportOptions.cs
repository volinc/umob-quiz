namespace UmobQuiz.Api.Application.Game;

public sealed record HistoryExportOptions(
    int Limit,
    bool IncludeActive,
    DateTime? FromUtc,
    DateTime? ToUtc)
{
    public const int DefaultExportLimit = 1000;
    public const int MaxExportLimit = 10000;

    public static int ClampLimit(int? limit) =>
        Math.Clamp(limit ?? DefaultExportLimit, 1, MaxExportLimit);
}
