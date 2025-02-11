using System.Text;
using System.Text.Json;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LLMAPI.Service;

public interface ILLMService
{
    Task<string> GetDataOpenRouter(string model, string prompt);
//    string GetDataFromImageGoogle();
}


public class LLMService : ILLMService
{
    private readonly IHttpClientFactory _httpClientFactory ;
    private readonly IConfiguration _configuration;

    public LLMService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> GetDataOpenRouter(string model, string prompt)
    {
/*        var client = _httpClientFactory.CreateClient();

        client.BaseAddress = new Uri("https://openrouter.ai/api/v1");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer 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");
*/
        var OpenRouterAPIKey = _configuration["OpenRouter:APIKey"];
        var OpenRouterAPIUrl = _configuration["OpenRouter:APIUrl"];

            var requestData = new
            {
                model = model,
                messages = new List<object>
                {
                    new { role = "user", content = prompt }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestData);

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenRouterAPIKey}");

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(OpenRouterAPIUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                    return jsonResponse?.choices?[0]?.message?.content ?? "No response from AI";
                }
                else
                {
                    return $"Error: {response.StatusCode}";
                }
            }
    }
/*     
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
 */
}