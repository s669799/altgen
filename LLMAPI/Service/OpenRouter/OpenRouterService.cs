    using System.IO;
    using System.Threading.Tasks;
    using Google.Protobuf;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using LLMAPI.Services.Interfaces;

    namespace LLMAPI.Services.OpenRouter
    {
        public class OpenRouterService : ITextGenerationService, IImageRecognitionService
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


            public async Task<string> AnalyzeImage(string model, string imageUrl)
            {
                var requestData = new
                {
                    model,
                    messages = new List<object>
                    {
                        new
                        {
                            role = "user",
                            content = new List<object>
                            {
                                new { type = "text", text = "Write a brief, one to two sentence alt text description for this image that captures the main subjects, action, and setting." },
                                new { type = "image_url", image_url = imageUrl }
                            }
                        }
                    }
                };

                return await SendRequest(requestData);
            }


            public async Task<string> AnalyzeImage(string model, ByteString imageBytes)
            {
                string base64Image = imageBytes.ToBase64();
                string dataUri = $"data:image/png;base64,{base64Image}";
                
                var requestData = new {
                    model,
                    response_format = new { type = "json_object" },
                    messages = new List<object> {
                        new {
                            role = "user",
                            content = new List<object> {
                                new { type = "text", text = "Write a brief, one to two sentence alt text description for this image." },
                                new { type = "image_bytes", image_bytes = dataUri }
                            }  
                        }
                    }
                };
        
                return await SendRequest(requestData);
            }


            /// <summary>
            /// New method that fetches the image from the URL, converts it to a base64-encoded string (with a data URI prefix),
            /// and sends it using the "image_bytes" field.
            /// </summary>
            public async Task<string> AnalyzeImageBase64(string model, string imageUrl)
            {
                ByteString imageBytes = await ReadImageFileAsync(imageUrl);
                string base64Image = Convert.ToBase64String(imageBytes.ToByteArray());
                string dataUri = $"data:image/png;base64,{base64Image}";

                var requestData = new
                {
                    model,
                    messages = new List<object>
                    {
                        new
                        {
                            role = "user",
                            content = new List<object>
                            {
                                new { type = "text", text = "Write a brief, one to two sentence alt text description for this image that captures the main subjects, action, and setting." },
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

                Console.WriteLine("Status code: " + response.StatusCode);
                Console.WriteLine("Response headers: " + response.Headers);

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Raw API response: " + responseContent);

                // Check if response is successful
                if (response.IsSuccessStatusCode)
                {
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                    // Check if error exists in the response
                    if (jsonResponse?.error != null)
                    {
                        Console.WriteLine("Error details: " + jsonResponse.error);
                        return $"Error: {jsonResponse.error}";
                    }

                    // Check if there's a valid response with text content (image description)
                    var imageDescription = jsonResponse?.choices?[0]?.message?.content;
                    if (imageDescription != null)
                    {
                        // Return the description text
                        return imageDescription.ToString();
                    }

                    // If no valid content, return a default message
                    return "No valid content returned from the model.";
                }
                else
                {
                    // If response is not successful, log the error and return
                    Console.WriteLine($"Error: {response.StatusCode}. Response: {responseContent}");
                    return $"Error: {response.StatusCode}. Response: {responseContent}";
                }
            }


            public async Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes)
            {
                return null;
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
