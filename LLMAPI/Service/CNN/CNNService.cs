using LLMAPI.DTO;
using LLMAPI.Service.Interfaces;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace LLMAPI.Services.CnnPrediction
{
    // Service implementation for interacting with the FastAPI CNN prediction endpoint.
    public class CNNPredictionService : ICnnPredictionService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _cnnApiUrl;

        public CNNPredictionService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            // Read the base URL for the CNN API from configuration
            _cnnApiUrl = _configuration["CnnApi:Url"] ?? throw new Exception("CNN API URL is not configured ('CnnApi:Url').");
        }

        // Sends image data to the CNN service and returns prediction results.
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
                        // Deserialize the JSON response into CnnPredictResponse DTO
                        var cnnResponse = JsonConvert.DeserializeObject<CNNResponse>(responseContent);

                        // Check for success flag within the response body if the API returns it
                        if (cnnResponse != null && cnnResponse.Success)
                        {
                            Console.WriteLine($"CNN Prediction Success: {cnnResponse.PredictedAircraft} ({cnnResponse.Probability:P1})");
                            return cnnResponse; // Return the successful prediction DTO
                        }
                        else
                        {
                            // API returned 2xx but reported failure within the body
                            string detail = cnnResponse?.Detail ?? "Unknown error details from CNN API.";
                            Console.WriteLine($"CNN Prediction API reported failure in response body: {detail}");
                            return new CNNResponse { Success = false, Detail = detail }; // Return failure DTO
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON Deserialization Error from CNN API: {jsonEx.Message}. Response: {responseContent}");
                        // Return a response indicating deserialization failure
                        return new CNNResponse { Success = false, Detail = $"Failed to parse CNN response: {jsonEx.Message}" };
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error parsing CNN API success response structure: {parseEx.Message}. Response: {responseContent}");
                        return new CNNResponse { Success = false, Detail = $"Error parsing CNN success response structure: {parseEx.Message}" };
                    }
                }
                else
                {
                    // Handle non-success HTTP status codes (4xx, 5xx)
                    string errorDetails = responseContent;
                    try
                    {
                        // Attempt to parse error details if response is JSON (FastAPI uses 'detail')
                        dynamic? errorJson = JsonConvert.DeserializeObject(responseContent);
                        if (errorJson?.detail != null)
                        {
                            errorDetails = errorJson.detail;
                        }
                    }
                    catch { /* Ignore JSON parse error if not JSON */ }

                    Console.WriteLine($"CNN API Error: {response.StatusCode}. Details: {errorDetails}");
                    // Return a response indicating HTTP error
                    return new CNNResponse { Success = false, Detail = $"CNN API returned status {response.StatusCode}. Details: {errorDetails}" };
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Error communicating with CNN API: {httpEx.Message}");
                // Return a response indicating request failure
                return new CNNResponse { Success = false, Detail = $"Error communicating with CNN service: {httpEx.Message}" };
            }
            catch (TaskCanceledException timeoutEx) when (timeoutEx.InnerException is TimeoutException)
            {
                Console.WriteLine($"Timeout communicating with CNN API: {timeoutEx.Message}");
                return new CNNResponse { Success = false, Detail = $"Timeout communicating with CNN service." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred during CNN API request: {ex}");
                // Return a response indicating unexpected error
                return new CNNResponse { Success = false, Detail = $"An unexpected error occurred contacting CNN service: {ex.Message}" };
            }
        }
    }
}
