using System.Text;
using FunctionsVsWebJobsPoc.Core.Csv;
using Xunit;

namespace FunctionsVsWebJobsPoc.Core.Tests.Csv;

public class CsvRowParserTests
{
    [Fact]
    public void ParseRowsAsJson_ReturnsOneJsonObjectPerDataRow()
    {
        var csv = "Name,Age\nAlice,30\nBob,25\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var parser = new CsvRowParser();

        var rows = parser.ParseRowsAsJson(stream);

        Assert.Equal(2, rows.Count);
        Assert.Contains("Alice", rows[0]);
        Assert.Contains("30", rows[0]);
        Assert.Contains("Bob", rows[1]);
        Assert.Contains("25", rows[1]);
    }

    [Fact]
    public void ParseRowsAsJson_NoDataRows_ReturnsEmptyList()
    {
        var csv = "Name,Age\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var parser = new CsvRowParser();

        var rows = parser.ParseRowsAsJson(stream);

        Assert.Empty(rows);
    }

    [Fact]
    public void ParseRowsAsJson_EmptyStream_ReturnsEmptyList()
    {
        using var stream = new MemoryStream();
        var parser = new CsvRowParser();

        var rows = parser.ParseRowsAsJson(stream);

        Assert.Empty(rows);
    }
}
