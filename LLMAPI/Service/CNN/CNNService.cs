// LLMAPI.Services/CnnPrediction/CNNPredictionService.cs
// Make sure your project dependencies include System.Text.Json (usually included in .NET Core/.Net 5+)
// and Google.Protobuf (for ByteString)

using LLMAPI.DTO; // Assuming CNNResponse is defined here with System.Text.Json.Serialization attributes
using LLMAPI.Service.Interfaces; // Assuming ICnnPredictionService is defined here
using Google.Protobuf; // For ByteString
using Microsoft.Extensions.Configuration; // For configuration access
using System;
using System.IO; // For MemoryStream and Path
using System.Net.Http; // For HttpClient
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using System.Text.Json; // Core System.Text.Json namespace
using System.Text.Json.Nodes; // For JsonDocument/JsonNode parsing (useful for flexible responses)
using System.Text.Json.Serialization; // For JsonSerializerOptions if needed
using System.Threading.Tasks; // For Task and TaskCanceledException

namespace LLMAPI.Services.CnnPrediction
{
    /// <summary>
    /// Service implementation for interacting with the FastAPI CNN prediction endpoint.
    /// Uses System.Text.Json for deserialization.
    /// </summary>
    public class CNNPredictionService : ICnnPredictionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _cnnApiUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="CNNPredictionService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
        /// <param name="configuration">Application configuration provider.</param>
        public CNNPredictionService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // Read the base URL for the CNN API from configuration
            _cnnApiUrl = _configuration["CnnApi:Url"] ?? throw new Exception("CNN API URL is not configured ('CnnApi:Url').");
        }

        /// <summary>
        /// Sends image data to the CNN service and returns prediction results.
        /// </summary>
        /// <param name="imageBytes">The image data as a ByteString.</param>
        /// <param name="fileName">The filename associated with the image (used by FastAPI).</param>
        /// <returns>A <see cref="CNNResponse"/> object containing the prediction results.</returns>
        public async Task<CNNResponse> PredictAircraftAsync(ByteString imageBytes, string fileName)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Console.WriteLine("Error: PredictAircraftAsync called with null or empty imageBytes.");
                // Return a response indicating failure, similar to how your original services handle errors
                return new CNNResponse { Success = false, Detail = "Image data is empty." };
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine("Warning: PredictAircraftAsync called with null or empty filename.");
                // Use a default filename if none provided
                fileName = "image.jpg"; // Common placeholder
            }

            // Construct the full API endpoint URL
            var endpointUrl = $"{_cnnApiUrl.TrimEnd('/')}/predict"; // Assumes FastAPI is hosted at _cnnApiUrl and has /predict endpoint

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60); // Set a reasonable timeout for CNN processing

            // Configure System.Text.Json options for deserialization if needed.
            // For this DTO with JsonPropertyName attributes, basic deserialization should work,
            // but options like PropertyNameCaseInsensitive can add robustness.
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Good practice if your JSON might have unexpected casing
                                                   // Add other options if needed, e.g., for handling comments, nulls, etc.
            };


            try
            {
                // Use MultipartFormDataContent to simulate a file upload via a form
                using var formData = new MultipartFormDataContent();

                // Create a ByteArrayContent from the image bytes
                var fileContent = new ByteArrayContent(imageBytes.ToArray());
                // Set content type header - attempt to infer it if possible, or use a common default
                // You might need a helper here to determine MIME type from file extension or byte signature
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // Common type, adjust if needed

                // Add the file content to the form data, using the 'file' parameter name which matches your FastAPI endpoint
                formData.Add(fileContent, name: "file", fileName: fileName);

                Console.WriteLine($">>> Sending image to CNN API at {endpointUrl}");

                var response = await client.PostAsync(endpointUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"<<< CNN API status code: {response.StatusCode}");
                Console.WriteLine($"<<< Raw CNN API response: {responseContent}");

                // Check for successful HTTP status codes (2xx)
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Deserialize the JSON response into CnnPredictResponse DTO using System.Text.Json
                        // The [JsonPropertyName] attribute should now be read correctly.
                        CNNResponse? cnnResponse = System.Text.Json.JsonSerializer.Deserialize<CNNResponse>(responseContent, jsonOptions);

                        if (cnnResponse != null && cnnResponse.Success)
                        {
                            // Successfully deserialized and the API success flag is true
                            Console.WriteLine($"CNN Prediction Success: {cnnResponse.PredictedAircraft} ({cnnResponse.Probability:P1})");
                            return cnnResponse; // Return the successful prediction DTO
                        }
                        else
                        {
                            // API returned 2xx but reported failure within the body, or deserialization yielded null
                            string detail = cnnResponse?.Detail ?? "Unknown error details from CNN API response.";
                            Console.WriteLine($"CNN Prediction API reported failure in response body or empty response: {detail}");
                            return new CNNResponse { Success = false, Detail = detail }; // Return failure DTO
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON Deserialization Error from CNN API (System.Text.Json): {jsonEx.Message}. Response: {responseContent}");
                        // Return a response indicating deserialization failure
                        return new CNNResponse { Success = false, Detail = $"Failed to parse CNN response: {jsonEx.Message}" };
                    }
                    catch (Exception parseEx)
                    {
                        // Catch other potential errors during response processing after successful HTTP status
                        Console.WriteLine($"Error processing CNN API success response: {parseEx.Message}. Response: {responseContent}");
                        return new CNNResponse { Success = false, Detail = $"Error processing CNN success response: {parseEx.Message}" };
                    }
                }
                else
                {
                    // Handle non-success HTTP status codes (4xx, 5xx)
                    string errorDetails = responseContent;
                    try
                    {
                        // Attempt to parse error details from the response body using System.Text.Json.JsonDocument
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        var root = errorDoc.RootElement;
                        // Assuming FastAPI error structure matches 'detail' string or array
                        if (root.TryGetProperty("detail", out var detailElement))
                        {
                            if (detailElement.ValueKind == JsonValueKind.String)
                            {
                                errorDetails = detailElement.GetString() ?? responseContent;
                            }
                            else // Handle array or other unexpected detail structures
                            {
                                errorDetails = detailElement.ToString(); // Fallback to string representation
                            }
                        }
                        // Optional: Check for other common error structures if your FastAPI might return them
                        // e.g., if (root.TryGetProperty("message", out var messageElement)) { errorDetails = messageElement.GetString() ?? errorDetails; }

                    }
                    catch (JsonException)
                    {
                        // Ignore JSON parse error if the response is not structured JSON
                        Console.WriteLine("Warning: Failed to parse CNN error response as JSON.");
                    }
                    catch (Exception parseEx)
                    {
                        // Catch errors during the error JSON parsing
                        Console.WriteLine($"Unexpected error parsing CNN error response body (System.Text.Json): {parseEx.Message}. Response: {responseContent}");
                    }


                    Console.WriteLine($"CNN API Error: {response.StatusCode}. Details: {errorDetails}");
                    // Return a response indicating HTTP error status
                    return new CNNResponse { Success = false, Detail = $"CNN API returned status {response.StatusCode}. Details: {errorDetails}" };
                }
            }
            catch (HttpRequestException httpEx)
            {
                // Handle fundamental HTTP request errors (network issues, connection refused, DNS, etc. BEFORE receiving a status code)
                Console.WriteLine($"HTTP Request Error communicating with CNN API: {httpEx.Message}");
                // Return a response indicating request failure
                return new CNNResponse { Success = false, Detail = $"Error communicating with CNN service: {httpEx.Message}" };
            }
            catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
            {
                // Handle timeout specifically
                Console.WriteLine($"Timeout communicating with CNN API: {timeoutEx.Message}");
                return new CNNResponse { Success = false, Detail = $"Timeout communicating with CNN service." };
            }
            catch (Exception ex)
            {
                // Catch any other unexpected errors during the process
                Console.WriteLine($"An unexpected error occurred during CNN API request: {ex}");
                // Return a response indicating unexpected error
                return new CNNResponse { Success = false, Detail = $"An unexpected error occurred contacting CNN service: {ex.Message}" };
            }
        }
    }
}
