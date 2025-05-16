/// <summary>
/// Represents a test request containing a query and context.
/// </summary>
public class TestRequest
{
    /// <summary>
    /// Gets or sets the query to be used in the test.
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Gets or sets the context for the test query.
    /// </summary>
    public string? Context { get; set; }
}
