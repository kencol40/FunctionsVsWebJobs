using System.Globalization;
using System.Text.Json;
using CsvHelper;

namespace FunctionsVsWebJobsPoc.Core.Csv;

public interface ICsvRowParser
{
    /// <summary>
    /// Reads a CSV stream and returns each data row serialized as a JSON object,
    /// keyed by the CSV header names.
    /// </summary>
    IReadOnlyList<string> ParseRowsAsJson(Stream csvStream);
}

public class CsvRowParser : ICsvRowParser
{
    public IReadOnlyList<string> ParseRowsAsJson(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream, leaveOpen: true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<string>();

        if (!csv.Read() || !csv.ReadHeader())
        {
            return rows;
        }

        var headers = csv.HeaderRecord ?? Array.Empty<string>();

        while (csv.Read())
        {
            var record = new Dictionary<string, string?>(headers.Length);
            foreach (var header in headers)
            {
                record[header] = csv.GetField(header);
            }

            rows.Add(JsonSerializer.Serialize(record));
        }

        return rows;
    }
}
