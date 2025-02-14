using System.Text;
using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace LLMAPI.Service
{
    public interface ILLMService
    {
        Task<string> GetDataOpenRouter(string model, string prompt);
        Task<string> GetDataFromImageGoogle(IFormFile imageFile);
    }

    public class LLMService : ILLMService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LLMService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> GetDataOpenRouter(string model, string prompt)
        {
            var OpenRouterAPIKey = _configuration["OpenRouter:APIKey"];
            var OpenRouterAPIUrl = _configuration["OpenRouter:APIUrl"];

            var requestData = new
            {
                model,
                messages = new List<object>
                {
                    new { role = "user", content = prompt }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestData);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenRouterAPIKey}");

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(OpenRouterAPIUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                return jsonResponse?.choices?[0]?.message?.content ?? "No response from AI";
            }
            else
            {
                return $"Error: {response.StatusCode}";
            }
        }


        public async Task<string> GetDataFromImageGoogle(IFormFile imageFile)
        {

            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    return "No image uploaded.";
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"keys/single-arcanum-449511-q4-e5c1f9347373.json");

                using var stream = imageFile.OpenReadStream();
                var image = Google.Cloud.Vision.V1.Image.FromStream(stream);

                var client = ImageAnnotatorClient.Create();

                // Perform label detection on the image file
                IReadOnlyList<EntityAnnotation> labels = await client.DetectLabelsAsync(image);

                if (labels == null || labels.Count == 0)
                {
                    return "No labels detected.";
                }

                // Format results as alt text
                var altText = new StringBuilder();
                foreach (var label in labels)
                {
                    altText.AppendLine($"{label.Description} (Confidence: {label.Score:F2})");
                }

                return altText.ToString();
            }
            catch (Exception ex)
            {
                return $"Error processing image: {ex.Message}";
            }
        }
    }
}