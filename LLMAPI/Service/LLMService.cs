using System.Text;
using Google.Cloud.Vision.V1;
using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Api.Gax.Grpc.Rest;
using Grpc.Core;

namespace LLMAPI.Service
{
    public interface ILLMService
    {    
        Task<string> GetDataOpenRouter(string model, string prompt);

        Task<string> GetDataFromImageGoogle(IFormFile imageFile);

        Task<string> GenerateContent(string projectId, string location, string publisher, string model);
    }

    /// <summary>
    /// Service class for handling different LLM models.
    /// </summary>
    public class LLMService : ILLMService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LLMService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Sends a prompt to a model in the OpenRouter API and retrieves the AI response.
        /// </summary>
        /// <param name="model">The model identifier (e.g., "google/gemini-flash-1.5-8b").</param>
        /// <param name="prompt">The prompt to send to the model.</param>
        /// <returns>The AI's response as a string.</returns>
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

        /// <summary>
        /// Analyzes an uploaded image using Google Vision API to detect labels and returns the results.
        /// </summary>
        /// <param name="model">The image file to be processed.</param>
        /// <returns>A string containing detected labels and confidence scores.</returns>
        public async Task<string> GetDataFromImageGoogle(IFormFile model)
        {

            try
            {
                if (model == null || model.Length == 0)
                {
                    return "No image uploaded.";
                }

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"keys/rich-world-450914-e6-b6ee1b4424e9.json");

                using var stream = model.OpenReadStream();
                var image = Google.Cloud.Vision.V1.Image.FromStream(stream);

                var client = new ImageAnnotatorClientBuilder
                {
                    GrpcAdapter = RestGrpcAdapter.Default
                }.Build();                

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

        public async Task<string> GenerateContent(
            string projectId,
            string location,
            string publisher,
            string model
            )
        {
            // Create client
            var predictionServiceClient = new PredictionServiceClientBuilder
            {
                Endpoint = $"{location}-aiplatform.googleapis.com"
            }.Build();

            // Initialize content request
            var generateContentRequest = new GenerateContentRequest
            {
                Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.4f,
                    TopP = 1,
                    TopK = 32,
                    MaxOutputTokens = 2048
                },
                Contents =
                {
                    new Content
                    {   
                        Role = "USER",
                        Parts =
                        {
                            new Part { Text = "What's in this photo?" },
                            new Part { FileData = new() { MimeType = "image/png", FileUri = "gs://generativeai-downloads/images/scones.jpg" } }
                        }
                    }   
                }
            };

            // Make the request, returning a streaming response
            using PredictionServiceClient.StreamGenerateContentStream response = predictionServiceClient.StreamGenerateContent(generateContentRequest);

            StringBuilder fullText = new();

            // Read streaming responses from server until complete
            var responseStream = response.GetResponseStream();
            await foreach (GenerateContentResponse responseItem in responseStream)
            {
                fullText.Append(responseItem.Candidates[0].Content.Parts[0].Text);
            }

            return fullText.ToString();
        }
    }

}