using System.Text;
using UmobQuiz.Api.Application.Game;
using UmobQuiz.Api.Domain.Entities;

namespace UmobQuiz.Tests;

public sealed class GameHistoryCsvWriterTests
{
    [Fact]
    public void EscapeField_PrefixesFormulaCharacters()
    {
        Assert.Equal("'=1+1", GameHistoryCsvWriter.EscapeField("=1+1"));
        Assert.Equal("'+danger", GameHistoryCsvWriter.EscapeField("+danger"));
        Assert.Equal("'-10", GameHistoryCsvWriter.EscapeField("-10"));
        Assert.Equal("'@sum", GameHistoryCsvWriter.EscapeField("@sum"));
    }

    [Fact]
    public void EscapeField_QuotesCommasAndQuotes()
    {
        Assert.Equal("\"a,b\"", GameHistoryCsvWriter.EscapeField("a,b"));
        Assert.Equal("\"say \"\"hi\"\"\"", GameHistoryCsvWriter.EscapeField("say \"hi\""));
    }

    [Fact]
    public void EscapeField_LeavesSafeValuesUnchanged()
    {
        Assert.Equal("Won", GameHistoryCsvWriter.EscapeField("Won"));
        Assert.Equal("150", GameHistoryCsvWriter.EscapeField("150"));
        Assert.Equal(string.Empty, GameHistoryCsvWriter.EscapeField(string.Empty));
    }

    [Fact]
    public async Task WriteRowAsync_WritesHeaderAndDataRow()
    {
        await using var stream = new MemoryStream();
        await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

        var sessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var start = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 27, 12, 1, 0, DateTimeKind.Utc);

        await GameHistoryCsvWriter.WriteHeaderAsync(writer, CancellationToken.None);
        await GameHistoryCsvWriter.WriteRowAsync(
            writer,
            new GameHistoryCsvRow(sessionId, start, end, 100, GameSessionStatus.Won.ToString()),
            CancellationToken.None);
        await writer.FlushAsync();

        var text = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("SessionId,StartTimeUtc,EndTimeUtc,Score,Status", text);
        Assert.Contains(sessionId.ToString(), text);
        Assert.Contains("2026-05-27T12:00:00.000Z", text);
        Assert.Contains("2026-05-27T12:01:00.000Z", text);
        Assert.Contains(",100,Won", text);
    }
}
