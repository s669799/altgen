public class PromptResult
{
    public string Prompt { get; set; }
    public string Response { get; set; }
}

public class ImageAnalysisResult
{
    public string ImageUrl { get; set; }
    public List<PromptResult> Results { get; set; } = new();
}
