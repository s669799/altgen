using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;

namespace LLMAPI.Services.OpenRouter
{
    public class OpenRouterService : ITextGenerationService, IImageRecognitionService, IImageFileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OpenRouterService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<string> GenerateText(string model, string prompt)
        {
            var requestData = new
            {
                model,
                messages = new List<object>
                {
                    new { role = "user", content = prompt }
                }
            };

            return await SendRequest(requestData);
        }

        public async Task<string> AnalyzeImage(string model, string imageUrl, string prompt)
        {
            var requestData = new
            {
                model,
                messages = new List<object>
                {
                    new {
                        role = "user",
                        content = new List<object>
                        {
                            new { type = "text", text = prompt },
                            new { type = "image_url", image_url = new { url = imageUrl } }
                        }
                    }
                }
            };

            return await SendRequest(requestData);
        }

        public async Task<string> AnalyzeImage(string model, ByteString imageBytes, string prompt)
        {
            string base64Image = imageBytes.ToBase64();
            string dataUri = $"data:image/png;base64,{base64Image}";

            var requestData = new
            {
                model,
                messages = new List<object>
                {
                    new {
                        role = "user",
                        content = new List<object>
                        {
                            new { type = "text", text = prompt },
                            new { type = "image_bytes", image_bytes = dataUri }
                        }
                    }
                }
            };

            return await SendRequest(requestData);
        }

        private async Task<string> SendRequest(object requestData)
        {
            var openRouterAPIKey = _configuration["OpenRouter:APIKey"];
            var openRouterAPIUrl = _configuration["OpenRouter:APIUrl"];

            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openRouterAPIKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:5256");
            client.DefaultRequestHeaders.Add("X-Title", "AltGen");

            var jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(openRouterAPIUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Status code: " + response.StatusCode);
            Console.WriteLine("Response headers: " + response.Headers);
            Console.WriteLine("Raw API response: " + responseContent);

            if (response.IsSuccessStatusCode)
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                if (jsonResponse?.error != null)
                {
                    Console.WriteLine("Error details: " + jsonResponse.error);
                    return $"Error: {jsonResponse.error}";
                }

                var imageDescription = jsonResponse?.choices?[0]?.message?.content;
                if (imageDescription != null)
                {
                    return imageDescription.ToString();
                }

                return "No valid content returned from the model.";
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}. Response: {responseContent}");
                return $"Error: {response.StatusCode}. Response: {responseContent}";
            }
        }

        public async Task<ByteString> ConvertImageToByteString(IFormFile imageFile)
        {
            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            return ByteString.CopyFrom(memoryStream.ToArray());
        }

        public async Task<ByteString> ReadImageFileAsync(string url)
        {
            using HttpClient client = new();
            using var response = await client.GetAsync(url);
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            return ByteString.CopyFrom(imageBytes);
        }
    }
}
