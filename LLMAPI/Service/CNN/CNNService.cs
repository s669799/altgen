using LLMAPI.DTO;
using LLMAPI.Service.Interfaces;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
            _cnnApiUrl = _configuration["CnnApi:Url"] ?? throw new Exception("CNN API URL is not configured ('CnnApi:Url').");
        }

        /// <summary>
        /// Sends image data to the CNN service and returns prediction results.
        /// </summary>
        /// <param name="imageBytes">The image data as a ByteString.</param>
        /// <param name="fileName">The filename associated with the image (used by FastAPI).</param>
        /// <returns>A <see cref="CNNResponse"/> object containing the prediction results.</returns>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request to the CNN endpoint fails.</exception>
        /// <exception cref="JsonException">Thrown if deserialization of the CNN response fails.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors.</exception>
        public async Task<CNNResponse> PredictAircraftAsync(ByteString imageBytes, string fileName)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                Console.WriteLine("Error: PredictAircraftAsync called with null or empty imageBytes.");
                return new CNNResponse { Success = false, Detail = "Image data is empty." };
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Console.WriteLine("Warning: PredictAircraftAsync called with null or empty filename.");
                fileName = "image.jpg";
            }

            var endpointUrl = $"{_cnnApiUrl.TrimEnd('/')}/predict";

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                using var formData = new MultipartFormDataContent();

                var fileContent = new ByteArrayContent(imageBytes.ToArray());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                formData.Add(fileContent, name: "file", fileName: fileName);

                Console.WriteLine($">>> Sending image to CNN API at {endpointUrl}");

                var response = await client.PostAsync(endpointUrl, formData);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"<<< CNN API status code: {response.StatusCode}");
                Console.WriteLine($"<<< Raw CNN API response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        CNNResponse? cnnResponse = System.Text.Json.JsonSerializer.Deserialize<CNNResponse>(responseContent, jsonOptions);

                        if (cnnResponse != null && cnnResponse.Success)
                        {
                            Console.WriteLine($"CNN Prediction Success: {cnnResponse.PredictedAircraft} ({cnnResponse.Probability:P1})");
                            return cnnResponse;
                        }
                        else
                        {
                            string detail = cnnResponse?.Detail ?? "Unknown error details from CNN API response.";
                            Console.WriteLine($"CNN Prediction API reported failure in response body or empty response: {detail}");
                            return new CNNResponse { Success = false, Detail = detail };
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"JSON Deserialization Error from CNN API (System.Text.Json): {jsonEx.Message}. Response: {responseContent}");
                        return new CNNResponse { Success = false, Detail = $"Failed to parse CNN response: {jsonEx.Message}" };
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Error processing CNN API success response: {parseEx.Message}. Response: {responseContent}");
                        return new CNNResponse { Success = false, Detail = $"Error processing CNN success response: {parseEx.Message}" };
                    }
                }
                else
                {
                    string errorDetails = responseContent;
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(responseContent);
                        var root = errorDoc.RootElement;
                        if (root.TryGetProperty("detail", out var detailElement))
                        {
                            if (detailElement.ValueKind == JsonValueKind.String)
                            {
                                errorDetails = detailElement.GetString() ?? responseContent;
                            }
                            else
                            {
                                errorDetails = detailElement.ToString();
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine("Warning: Failed to parse CNN error response as JSON.");
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"Unexpected error parsing CNN error response body (System.Text.Json): {parseEx.Message}. Response: {responseContent}");
                    }

                    Console.WriteLine($"CNN API Error: {response.StatusCode}. Details: {errorDetails}");
                    return new CNNResponse { Success = false, Detail = $"CNN API returned status {response.StatusCode}. Details: {errorDetails}" };
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request Error communicating with CNN API: {httpEx.Message}");
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
                return new CNNResponse { Success = false, Detail = $"An unexpected error occurred contacting CNN service: {ex.Message}" };
            }
        }
    }
}
