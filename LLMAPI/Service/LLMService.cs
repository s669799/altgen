using System.Text;
using System.Text.Json;
using Google.Cloud.Vision.V1;

namespace LLMAPI.Service;

public interface ILLMService
{
    Task<string> GetDataOpenRouter();
    string GetDataFromImageGoogle();
}


public class LLMService : ILLMService
{
    private readonly IHttpClientFactory _httpClientFactory ;

    public LLMService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataOpenRouter()
    {
        var client = _httpClientFactory.CreateClient();

        client.BaseAddress = new Uri("https://openrouter.ai/api/v1");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer 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");

        var requestBody = new
        {
            model = "google/gemini-2.0-flash-thinking-exp:free",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "Do you know who I am?"
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://openrouter.ai/api/v1/chat/completions", jsonContent);
        response.EnsureSuccessStatusCode(); // Throws an exception if the status code is not successful

        return await response.Content.ReadAsStringAsync();
    }
    
    public string GetDataFromImageGoogle()
    {


        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"keys/single-arcanum-449511-q4-e5c1f9347373.json");
        
        var client = ImageAnnotatorClient.Create();
        
        // The path to the image file to annotate
        var imageFilePath = "testImage/1.jpg";

        // Load the image file into memory
        var image = Image.FromFile(imageFilePath);

        // Perform label detection on the image file
        IReadOnlyList<EntityAnnotation> labels = client.DetectLabels(image);

        client.

        return labels?.ToString();
    }
    
}