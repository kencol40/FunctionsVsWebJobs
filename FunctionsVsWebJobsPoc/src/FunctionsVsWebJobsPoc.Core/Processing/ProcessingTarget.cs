namespace FunctionsVsWebJobsPoc.Core.Processing;

/// <summary>
/// Identifies which implementation stack (Azure Function vs classic WebJob) produced the data
/// being processed, so shared services can route persistence to the matching set of tables.
/// </summary>
public enum ProcessingTarget
{
    Function,
    WebJob
}
