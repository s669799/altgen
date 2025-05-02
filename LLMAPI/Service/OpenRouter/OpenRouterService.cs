// LLMAPI.Services/OpenRouter/OpenRouterService.cs
using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
// using LLMAPI.Service.Interfaces; // Double check if this is needed or a typo
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json; // Use System.Text.Json
using System.Text.Json.Serialization; // Use System.Text.Json.Serialization attributes
using System.Text.Json.Nodes; // For JsonDocument parsing
using System.Threading.Tasks;
using Google.Protobuf;
using System.Net.Http.Headers;
using LLMAPI.Service.Interfaces;

namespace LLMAPI.Services.OpenRouter
{
    /// <summary>
    /// Service to interact with the OpenRouter API for text generation and image recognition.
    /// Implements interfaces for text generation, image recognition, and image file handling.
    /// </summary>
    public class OpenRouterService : ITextGenerationService, IImageRecognitionService, IImageFileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        // Consider adding an ILogger for robust logging in production
        // private readonly ILogger<OpenRouterService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenRouterService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
        /// <param name="configuration">Application configuration provider.</param>
        // public OpenRouterService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<OpenRouterService> logger)
        public OpenRouterService(IHttpClientFactory httpClientFactory, IConfiguration configuration /*, ILogger<OpenRouterService> logger */)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // _logger = logger;
        }

        // --- ITextGenerationService Implementation ---

        /// <summary>
        /// Generates text using a specified model and prompt via the OpenRouter API.
        /// </summary>
        /// <param name="model">The LLM model to use for text generation (e.g., "openai/gpt-4o-mini").</param>
        /// <param name="prompt">The text prompt to send to the LLM.</param>
        /// <returns>The generated text response from the LLM or throws an Exception on error.</returns>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
        /// <exception cref="JsonException">Thrown if the JSON response cannot be parsed.</exception>
        /// <exception cref="Exception">Thrown for API-specific errors indicated in the response body.</exception>
        public async Task<string> GenerateText(string model, string prompt)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                // _logger.LogError("GenerateText called with null or empty model.");
                Console.WriteLine("Error: GenerateText called with null or empty model."); // Dev logging
                throw new ArgumentNullException(nameof(model), "Model cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(prompt))
            {
                // _logger.LogWarning("GenerateText called with null or empty prompt.");
                Console.WriteLine("Warning: GenerateText called with null or empty prompt."); // Dev logging
                // Depending on API, empty prompt might be allowed, but for robustness, let's check
                throw new ArgumentNullException(nameof(prompt), "Prompt cannot be null or empty.");
            }


            var requestData = new
            {
                model,
                messages = new List<object>
                {
                    new { role = "user", content = prompt }
                }
                // Add other parameters like temperature, max_tokens if needed by your standard text generation
            };

            return await SendRequest(requestData); // SendRequest is responsible for error handling and throwing
        }

        // --- IImageRecognitionService Implementation ---

        /// <summary>
        /// Analyzes an image from a given URL using a specified model and prompt, potentially including CNN context.
        /// This overload is typically used by the LLMController (process-request or process-request-body with URL).
        /// </summary>
        /// <param name="model">The LLM model to use for image analysis.</param>
        /// <param name="imageUrl">The URL of the image to analyze.</param>
        /// <param name="textPrompt">The base text prompt to guide the image analysis.</param>
        /// <param name="predictedAircraft">Optional predicted aircraft type provided by the client.</param>
        /// <param name="probability">Optional probability of the CNN prediction provided by the client.</param>
        /// <param name="temperature">Temperature setting for the LLM.</param>
        /// <returns>The image description generated by the LLM or throws an Exception on error.</returns>
        /// <exception cref="ArgumentNullException">Thrown if model or imageUrl is null/empty.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request to the LLM endpoint fails.</exception>
        /// <exception cref="JsonException">Thrown if the JSON response cannot be parsed.</exception>
        /// <exception cref="Exception">Thrown for API-specific errors indicated in the response body.</exception>
        public async Task<string> AnalyzeImage(
            string model,
            string imageUrl, // Accepts URL string
            string textPrompt, // Base prompt (user + default)
            string? predictedAircraft, // Optional CNN data (from LLMRequest)
            double? probability,       // Optional CNN data (from LLMRequest)
            double temperature)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                // _logger.LogError("AnalyzeImage (URL) called with null or empty model.");
                Console.WriteLine("Error: AnalyzeImage (URL) called with null or empty model."); // Dev logging
                throw new ArgumentNullException(nameof(model), "Model cannot be null or empty.");
            }
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                // _logger.LogError("AnalyzeImage (URL) called with null or empty imageUrl.");
                Console.WriteLine("Error: AnalyzeImage (URL) called with null or empty imageUrl."); // Dev logging
                                                                                                    // Note: textPrompt can be null/empty if the client provides no prompt and you don't fallback to a default *here*
                                                                                                    // but the controller should handle the default prompt logic.
                throw new ArgumentNullException(nameof(imageUrl), "Image URL cannot be null or empty.");
            }


            string compositePrompt = BuildCompositePrompt(textPrompt, predictedAircraft, probability); // Builds prompt including optional CNN data

            var requestData = new
            {
                model,
                temperature,
                max_tokens = 500, // Example value, configure as needed
                messages = new List<object>
                {
                    new {
                        role = "user",
                        content = new List<object>
                        {
                            new { type = "text", text = compositePrompt },
                            // OpenRouter supports 'image_url' with a direct URL
                            new { type = "image_url", image_url = new { url = imageUrl } }
                        }
                    }
                }
            };
            return await SendRequest(requestData); // SendRequest is responsible for error handling and throwing
        }


        /// <summary>
        /// Analyzes an image from byte data using a specified model and prompt, including CNN context.
        /// This overload is typically used by the CNNController after image download and CNN prediction.
        /// </summary>
        /// <param name="model">The LLM model to use for image analysis.</param>
        /// <param name="imageBytes">The image data as a ByteString obtained from an IImageFileService.</param>
        /// <param name="textPrompt">The base text prompt to guide the image analysis.</param>
        /// <param name="predictedAircraft">Predicted aircraft type from CNN (expected in CNN workflow).</param>
        /// <param name="probability">Probability of the CNN prediction (expected in CNN workflow).</param>
        /// <param name="temperature">Temperature setting for the LLM.</param>
        /// <returns>The image description generated by the LLM or throws an Exception on error.</returns>
        /// <exception cref="ArgumentNullException">Thrown if model or imageBytes is null/empty.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request to the LLM endpoint fails.</exception>
        /// <exception cref="JsonException">Thrown if the JSON response cannot be parsed.</exception>
        /// <exception cref="Exception">Thrown for API-specific errors indicated in the response body.</exception>
        public async Task<string> AnalyzeImage(
            string model,
            ByteString imageBytes, // Accepts ByteString data
            string textPrompt, // Base prompt (user + default)
            string? predictedAircraft, // CNN data (expected from CNN workflow)
            double? probability,       // CNN data (expected from CNN workflow)
            double temperature)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                // _logger.LogError("AnalyzeImage (Bytes) called with null or empty model.");
                Console.WriteLine("Error: AnalyzeImage (Bytes) called with null or empty model."); // Dev logging
                throw new ArgumentNullException(nameof(model), "Model cannot be null or empty.");
            }
            if (imageBytes == null || imageBytes.Length == 0)
            {
                // _logger.LogError("AnalyzeImage (Bytes) called with null or empty imageBytes.");
                Console.WriteLine("Error: AnalyzeImage (Bytes) called with null or empty imageBytes."); // Dev logging
                throw new ArgumentNullException(nameof(imageBytes), "Image bytes cannot be null or empty.");
            }
            // Note: textPrompt, predictedAircraft, probability can be null depending on workflow/client input.
            // BuildCompositePrompt handles null/empty values for CNN parts.

            string compositePrompt = BuildCompositePrompt(textPrompt, predictedAircraft, probability); // Builds prompt including CNN data

            string base64Image = imageBytes.ToBase64();
            // OpenRouter API image_url content type generally accepts Data URIs
            // Format: data:<mime_type>;base64,<base64_data>
            // You might need a helper to determine MIME type from imageBytes signature or filename if available.
            // For simplicity, assuming png or jpg - adjust based on need and OpenRouter docs.
            // Determine actual MIME type if possible, otherwise default
            // string mimeType = ImageHelper.GetMimeType(imageBytes) ?? "image/jpeg"; // Example helper call
            string mimeType = "image/jpg"; // Default or placeholder
            string dataUri = $"data:{mimeType};base64,{base64Image}";


            var requestData = new
            {
                model,
                temperature,
                max_tokens = 500, // Example value, configure as needed
                messages = new List<object>
                {
                    new {
                        role = "user",
                        content = new List<object>
                        {
                            new { type = "text", text = compositePrompt },
                            // OpenRouter generally prefers 'image_url' with a Data URI for base64
                            new { type = "image_url", image_url = new { url = dataUri } }
                            // Check OpenRouter docs if they have a specific 'image_bytes' type with just base64
                            // new { type = "image_bytes", image_bytes = base64Image } // Use this if OpenRouter documentation specifies this type
                        }
                    }
                }
            };

            return await SendRequest(requestData); // SendRequest is responsible for error handling and throwing
        }

        /// <summary>
        /// Helper method to build the composite prompt including optional CNN context.
        /// This method is used by both AnalyzeImage overloads.
        /// </summary>
        private string BuildCompositePrompt(string? basePrompt, string? predictedAircraft, double? probability)
        {
            string cnnContext = "";
            string NoYapping = "Be specific about aircraft model and variant. Do not mention this context in the alt text.";

            if (!string.IsNullOrWhiteSpace(predictedAircraft) && probability.HasValue)
            {
                cnnContext = $"Preceding analysis context: Identified primary subject as {predictedAircraft} with probability {probability.Value:P1}. ";
            }
            else if (!string.IsNullOrWhiteSpace(predictedAircraft))
            {
                cnnContext = $"Preceding analysis context: Identified primary subject as {predictedAircraft}. ";
            }

            string compositePrompt = cnnContext + NoYapping;

            if (!string.IsNullOrWhiteSpace(basePrompt) && !basePrompt.Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                compositePrompt += ". " + basePrompt;
            }

            return compositePrompt;
        }


        /// <summary>
        /// Sends a request to the OpenRouter API and handles the response.
        /// This method is responsible for serializing data, sending the HTTP request,
        /// checking the status code, deserializing the response, and throwing exceptions on error.
        /// </summary>
        /// <param name="requestData">The request data object to be serialized as JSON.</param>
        /// <returns>The content of the successful response from the API.</returns>
        /// <exception cref="ApplicationException">Thrown if API key or URL is missing.</exception>
        /// <exception cref="HttpRequestException">Thrown if the underlying HTTP request fails.</exception>
        /// <exception cref="JsonException">Thrown if deserialization fails.</exception>
        /// <exception cref="Exception">Thrown for API-specific errors indicated in the response body or unexpected response formats.</exception>
        private async Task<string> SendRequest(object requestData)
        {
            // Get API key and URL from configuration
            var openRouterAPIKey = _configuration["OpenRouter:APIKey"];
            var openRouterAPIUrl = _configuration["OpenRouter:APIUrl"];
            var openRouterReferer = _configuration["OpenRouter:Referer"] ?? "http://localhost"; // Default referer
            var openRouterTitle = _configuration["OpenRouter:Title"] ?? "LLMAPI";       // Default title

            if (string.IsNullOrEmpty(openRouterAPIKey) || string.IsNullOrEmpty(openRouterAPIUrl))
            {
                // Production logging: _logger.LogCritical("OpenRouter API Key or URL is not configured.");
                Console.WriteLine("Error: OpenRouter API Key or URL is not configured."); // Dev logging
                throw new ApplicationException("Service configuration missing for OpenRouter API."); // Throw specific exception
            }

            var client = _httpClientFactory.CreateClient("OpenRouterClient");

            // Configure client headers - Ensure clear works correctly if client factory reuses clients
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openRouterAPIKey}");
            client.DefaultRequestHeaders.Add("HTTP-Referer", openRouterReferer);
            client.DefaultRequestHeaders.Add("X-Title", openRouterTitle);

            // Configure System.Text.Json options - Adjust as needed
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true, // Good default for robust deserialization (though we are parsing manually below)
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Omits null properties when serializing
                // Add other options like converters if needed
                WriteIndented = true // For pretty printing in console log (optional)
            };


            // Serialize request data using System.Text.Json
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestData, jsonOptions); // Pass options here
            // Production logging: _logger.LogInformation("Sending JSON Request Body to {OpenRouterAPIUrl}: {JsonContent}", openRouterAPIUrl, jsonContent);
            Console.WriteLine($">>> Sending JSON Request Body to {openRouterAPIUrl}:\n{jsonContent}"); // Dev logging

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Send POST request
                var response = await client.PostAsync(openRouterAPIUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Production logging: _logger.LogInformation("Received OpenRouter API response: {StatusCode} - {ResponseContent}", response.StatusCode, responseContent);
                Console.WriteLine($"<<< Status code: {response.StatusCode}");
                Console.WriteLine($"<<< Raw API response: {responseContent}"); // Be cautious logging full responses in production


                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Deserialize the response using System.Text.Json to a type that matches the structure
                        // Using JsonDocument for flexible parsing of the expected chat completion structure
                        using var doc = JsonDocument.Parse(responseContent);
                        var root = doc.RootElement;

                        // Check for API-specific errors within the success response body
                        if (root.TryGetProperty("error", out var errorElement))
                        {
                            string errorMessage = errorElement.TryGetProperty("message", out var errorMessageElement) ? errorMessageElement.GetString() ?? errorElement.ToString() : errorElement.ToString();
                            Console.WriteLine($"OpenRouter API Error in success response body: {errorMessage}");
                            throw new Exception($"OpenRouter API Error in success response body: {errorMessage}");
                        }

                        // Extract the generated content from choices -> [0] -> message -> content
                        if (root.TryGetProperty("choices", out var choicesElement) &&
                            choicesElement.EnumerateArray().FirstOrDefault() is JsonElement firstChoice &&
                            firstChoice.TryGetProperty("message", out var messageElement) &&
                            messageElement.TryGetProperty("content", out var contentElement))
                        {
                            return contentElement.GetString() ?? ""; // Return the content string, default to empty if somehow null
                        }

                        // Case: Success status but no expected content structure or API error
                        Console.WriteLine("Warning: Successful response but no valid content found in expected format or API error structure.");
                        throw new Exception("Model returned a successful response, but no content was found in expected format.");

                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON Deserialization Error from OpenRouter (System.Text.Json): {jsonEx.Message}. Response: {responseContent}");
                        throw new Exception($"Failed to parse LLM service response: {jsonEx.Message}", jsonEx);
                    }
                    catch (Exception parseEx) when (parseEx is not HttpRequestException && parseEx is not JsonException)
                    {
                        // Handle other potential structured response parsing errors not covered by JsonException
                        Console.WriteLine($"Unexpected error parsing OpenRouter API success response structure: {parseEx.Message}. Response: {responseContent}"); // Dev logging
                        throw new Exception($"Unexpected format in LLM service success response: {parseEx.Message}", parseEx);
                    }
                }
                // Handle non-success response statuses (e.g., 4xx, 5xx)
                else
                {
                    // Attempt to extract error details from the response body using System.Text.Json
                    string errorDetails = responseContent;
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        var root = errorDoc.RootElement;
                        if (root.TryGetProperty("error", out var errorElement))
                        {
                            // Attempt to get nested message, fallback to error object string itself
                            errorDetails = errorElement.TryGetProperty("message", out var errorMessageElement) ? errorMessageElement.GetString() ?? errorElement.ToString() : errorElement.ToString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore JSON parsing error if the response isn't structured JSON
                        Console.WriteLine("Warning: Failed to parse error response as JSON from OpenRouter.");
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Unexpected error parsing OpenRouter error response body (System.Text.Json): {parseEx.Message}. Response: {responseContent}");
                    }


                    string errorMsg = $"OpenRouter API returned non-success status code {response.StatusCode}. Details: {errorDetails}";
                    Console.WriteLine(errorMsg);
                    // Throw HttpRequestException for non-success status codes
                    response.EnsureSuccessStatusCode(); // This will throw HttpRequestException
                    return null!; // This line is unreachable because EnsureSuccessStatusCode throws
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle fundamental HTTP request errors (network issues, DNS failures, timeouts BEFORE getting a response, etc.)
                Console.WriteLine($"HTTP Request Error sending to OpenRouter: {httpEx.Message}"); // Dev logging
                throw; // Re-throw the exception after logging
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during the process
                Console.WriteLine($"An unexpected error occurred during OpenRouter request: {ex}"); // Dev logging
                throw new Exception("An unexpected error occurred during the LLM service request.", ex); // Wrap and re-throw
            }
        }


        // --- IImageFileService Implementation ---

        /// <summary>
        /// Converts an uploaded image file (<see cref="IFormFile"/>) to a <see cref="ByteString"/>.
        /// This method might not be directly used by your current controllers but is required by the interface.
        /// </summary>
        /// <param name="imageFile">The image file uploaded via HTTP.</param>
        /// <returns>The image content as a ByteString, or null if input is null or empty.</returns>
        public async Task<ByteString?> ConvertImageToByteString(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                // _logger.LogWarning("ConvertImageToByteString called with null or empty IFormFile.");
                Console.WriteLine("Warning: ConvertImageToByteString called with null or empty IFormFile."); // Dev logging
                return null;
            }

            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            return ByteString.CopyFrom(memoryStream.ToArray());
        }

        /// <summary>
        /// Reads an image from a URL and converts it to a <see cref="ByteString"/>.
        /// This method is used by the CNNController to download the image before processing.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <returns>The image content as a ByteString.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the URL is null or empty.</exception>
        /// <exception cref="UriFormatException">Thrown if the URL is not a valid absolute URI.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request to download the image fails.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors during download/read.</exception>
        public async Task<ByteString> ReadImageFileAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Error: ReadImageFileAsync called with null or empty URL.");
                throw new ArgumentNullException(nameof(url), "Image URL cannot be null or empty.");
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                Console.WriteLine($"Invalid URL format provided: {url}");
                throw new UriFormatException($"Invalid URL format provided: {url}");
            }

            try
            {
                using HttpClient client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                Console.WriteLine($"Attempting to download image from URL: {url}");
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                Console.WriteLine($"Successfully downloaded {imageBytes.Length} bytes from URL: {url}");

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    Console.WriteLine($"Warning: Downloaded content is empty for URL: {url}");
                    throw new Exception($"Downloaded image content is empty for URL: {url}");
                }

                return ByteString.CopyFrom(imageBytes);
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"Failed to download image from URL '{url}'. Error: {httpEx.Message}");
                throw;
            }
            catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
            {
                Console.WriteLine($"Timeout downloading image from URL '{url}': {timeoutEx.Message}");
                throw new TimeoutException($"Timeout downloading image from URL: {url}", timeoutEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while reading image from URL '{url}'. Error: {ex.Message}");
                throw new Exception($"An error occurred while downloading/reading image from URL: {url}", ex);
            }

            // Add this line to satisfy the compiler that something is always returned or thrown
            // Given the catch-all Exception, this should theoretically never be reached,
            // but it satisfies the code path analysis.
            throw new InvalidOperationException("Unexpected code path reached in ReadImageFileAsync.");
        }

    }
}