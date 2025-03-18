using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LLMAPI.Service.Interfaces;

namespace LLMAPI.Service.Replicate
{
    public class ReplicateService : IReplicateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _replicateApiKey;
        private const string _replicateApiUrl = "https://api.replicate.com/v1/predictions";
        private readonly ILogger<ReplicateService> _logger;

        public ReplicateService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ReplicateService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _replicateApiKey = configuration["Replicate:APIKey"] ?? throw new ArgumentNullException("Replicate:APIKey", "Replicate:APIKey is missing from configuration.");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> CreatePrediction(string modelVersion, Dictionary<string, object> input)
        {
            if (string.IsNullOrEmpty(modelVersion))
            {
                throw new ArgumentException("Model version cannot be null or empty.", nameof(modelVersion));
            }

            if (input == null || input.Count == 0)
            {
                throw new ArgumentException("Input dictionary cannot be null or empty.", nameof(input));
            }

            // Add default parameters to ensure they are always present.
            if (!input.ContainsKey("top_p"))
                input["top_p"] = 0.9;
            if (!input.ContainsKey("temperature"))
                input["temperature"] = 0.1;
            if (!input.ContainsKey("max_length_tokens"))
                input["max_length_tokens"] = 2048;
            if (!input.ContainsKey("repetition_penalty"))
                input["repetition_penalty"] = 1.1;

            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _replicateApiKey);

                _logger.LogInformation($"Creating prediction with model version: {modelVersion}");

                var requestData = new
                {
                    version = modelVersion,
                    input = input
                };

                string requestBody = JsonSerializer.Serialize(requestData);
                _logger.LogDebug($"Request Body: {requestBody}");

                using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                using var response = await client.PostAsync(_replicateApiUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Received response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var document = JsonDocument.Parse(responseContent);
                        var root = document.RootElement;

                        if (root.TryGetProperty("id", out var idElement))
                        {
                            return idElement.GetString();
                        }

                        _logger.LogError("Prediction ID not found in the response. Response may be malformed.");
                        throw new Exception("Malformed response: Prediction ID missing.");
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(jsonEx, "Invalid JSON format in response.");
                        throw new Exception("Failed to parse JSON response.");
                    }
                }

                _logger.LogError($"Unexpected response. Status: {response.StatusCode}, Content: {responseContent}");
                throw new Exception($"Error: {response.StatusCode} - {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prediction");
                throw;
            }
        }

        public async Task<string> GetPredictionResult(string predictionId)
        {
            if (string.IsNullOrEmpty(predictionId))
            {
                throw new ArgumentException("Prediction ID cannot be null or empty.", nameof(predictionId));
            }

            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _replicateApiKey);

                string apiUrl = $"{_replicateApiUrl}/{predictionId}";
                _logger.LogInformation($"Getting prediction result for ID: {predictionId}");

                using var response = await client.GetAsync(apiUrl);
                string responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Full Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    // Return the response content directly.
                    return responseContent;
                }

                _logger.LogError($"Unexpected response. Status: {response.StatusCode}, Content: {responseContent}");
                throw new Exception($"Error: {response.StatusCode} - {responseContent}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prediction");
                throw;
            }
        }

        public async Task<string> TestAccountAccess()
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _replicateApiKey);

                HttpResponseMessage response = await client.GetAsync("https://api.replicate.com/v1/account");
                string responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Account Access Response: Status Code: {response.StatusCode}, Body: {responseContent}");

                response.EnsureSuccessStatusCode();

                return responseContent;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error accessing account");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Internal Server Error");
                throw new Exception($"Internal Server Error: {ex.Message}");
            }
        }
    }
}
