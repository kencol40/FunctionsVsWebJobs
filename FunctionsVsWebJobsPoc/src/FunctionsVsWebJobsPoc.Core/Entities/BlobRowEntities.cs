namespace FunctionsVsWebJobsPoc.Core.Entities;

/// <summary>
/// Common shape shared by both the function and webjob blob-row entities so processing
/// code can operate against a single abstraction regardless of the target table.
/// </summary>
public interface IBlobRowRecord
{
    int Id { get; set; }
    string BlobName { get; set; }
    string RowJson { get; set; }
    DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Row written by the Azure Function blob trigger stack. Maps to table function_blobrow_data.</summary>
public class FunctionBlobRow : IBlobRowRecord
{
    public int Id { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string RowJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Row written by the WebJob blob trigger stack. Maps to table webjob_blobrow_data.</summary>
public class WebJobBlobRow : IBlobRowRecord
{
    public int Id { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string RowJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
}
