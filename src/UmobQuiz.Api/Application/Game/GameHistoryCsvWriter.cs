using System.Globalization;
using System.Text;

namespace UmobQuiz.Api.Application.Game;

public sealed record GameHistoryCsvRow(
    Guid SessionId,
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    int Score,
    string Status);

public static class GameHistoryCsvWriter
{
    private const string Header = "SessionId,StartTimeUtc,EndTimeUtc,Score,Status";

    public static async Task WriteHeaderAsync(StreamWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync(Header.AsMemory(), cancellationToken);
    }

    public static async Task WriteRowAsync(
        StreamWriter writer,
        GameHistoryCsvRow row,
        CancellationToken cancellationToken)
    {
        var line = string.Join(
            ',',
            EscapeField(row.SessionId.ToString()),
            EscapeField(FormatUtc(row.StartTimeUtc)),
            EscapeField(row.EndTimeUtc is null ? string.Empty : FormatUtc(row.EndTimeUtc.Value)),
            EscapeField(row.Score.ToString(CultureInfo.InvariantCulture)),
            EscapeField(row.Status));

        await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    public static string EscapeField(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (NeedsFormulaEscape(value))
        {
            value = "'" + value;
        }

        if (value.IndexOfAny([',', '"', '\r', '\n']) >= 0)
        {
            return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
        }

        return value;
    }

    private static bool NeedsFormulaEscape(string value)
    {
        var first = value[0];
        return first is '=' or '+' or '-' or '@' or '\t' or '\r';
    }

    private static string FormatUtc(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
}
