namespace FunctionsVsWebJobsPoc.Core.Entities;

/// <summary>
/// Common shape shared by both the function and webjob message-data entities so processing
/// code can operate against a single abstraction regardless of the target table.
/// </summary>
public interface IMessageDataRecord
{
    int Id { get; set; }
    string MessageId { get; set; }
    string BodyJson { get; set; }
    DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Row written by the Azure Function service bus trigger stack. Maps to table function_message_data.</summary>
public class FunctionMessageData : IMessageDataRecord
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string BodyJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
}

/// <summary>Row written by the WebJob service bus trigger stack. Maps to table webjob_message_data.</summary>
public class WebJobMessageData : IMessageDataRecord
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string BodyJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
}
