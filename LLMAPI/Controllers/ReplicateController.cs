using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LLMAPI.Service.Interfaces;
using LLMAPI.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/replicate")]
    public class ReplicateController : ControllerBase
    {
        private readonly IReplicateService _replicateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReplicateController> _logger;

        public ReplicateController(IReplicateService replicateService, IConfiguration configuration, ILogger<ReplicateController> logger)
        {
            _replicateService = replicateService;
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("RunModel")]
        public async Task<IActionResult> RunModel([FromBody] ReplicateRequest input)
        {
            if (input == null)
            {
                _logger.LogWarning("RunModel called with null input");
                return BadRequest("Input cannot be null.");
            }

            string modelVersion = "e5caf557dd9e5dcee46442e1315291ef1867f027991ede8ff95e304d4f734200";

            // Hard-coded default prompt that includes the necessary token.
            const string defaultPrompt = "Write a brief, one to two sentence alt text description for this <image>, Harvard style, that captures the main subjects, action, and setting. This is an alt text for an end user.";

            // If the user supplies supplemental text, append it.
            // Otherwise, only the default prompt will be used.
            string finalPrompt = string.IsNullOrWhiteSpace(input.Prompt)
                                 ? defaultPrompt
                                 : $"{defaultPrompt} {input.Prompt}";

            var replicateInput = new Dictionary<string, object>
    {
        { "image", input.Image },
        { "prompt", finalPrompt }
    };

            try
            {
                _logger.LogInformation("Creating prediction for model version: {ModelVersion}", modelVersion);
                string predictionId = await _replicateService.CreatePrediction(modelVersion, replicateInput);

                _logger.LogInformation("Polling for prediction result with ID: {PredictionId}", predictionId);
                var finalOutput = await PollForPredictionResult(predictionId);

                _logger.LogInformation("Prediction completed successfully with output.");
                return Ok(new { finalOutput });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running model.");
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        private async Task<string> PollForPredictionResult(string predictionId)
        {
            while (true)
            {
                // Log the start of the HTTP request for fetching prediction result.
                _logger.LogInformation("Fetching prediction result for ID: {PredictionId}", predictionId);

                var result = await _replicateService.GetPredictionResult(predictionId);

                using (JsonDocument document = JsonDocument.Parse(result))
                {
                    JsonElement root = document.RootElement;

                    // Check if the status property exists
                    if (root.TryGetProperty("status", out JsonElement statusElement))
                    {
                        string status = statusElement.GetString();

                        // Log the current status of the prediction
                        _logger.LogInformation("Prediction status: {Status}", status);

                        switch (status)
                        {
                            case "succeeded":
                                if (root.TryGetProperty("output", out JsonElement outputElement))
                                {
                                    // Log successful completion
                                    _logger.LogInformation("Prediction succeeded.");
                                    return outputElement.GetRawText();
                                }
                                // Log error for missing output
                                _logger.LogError("Prediction completed but no output was found.");
                                throw new Exception("Prediction succeeded without output.");

                            case "starting":
                            case "processing":
                                // Log that the prediction is still in progress
                                _logger.LogInformation("Prediction in progress (status: {Status}). Waiting before retry.", status);
                                await Task.Delay(2000);  // Adjust delay between checks as needed
                                break;

                            case "failed":
                                var errorMessage = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : "Unknown error.";
                                // Log error message if prediction fails
                                _logger.LogError("Prediction failed with message: {ErrorMessage}", errorMessage);
                                throw new Exception($"Prediction failed: {errorMessage}");

                            default:
                                // Log any unexpected status
                                _logger.LogWarning("Encountered unexpected status: {Status}", status);
                                break;
                        }
                    }
                    else
                    {
                        // Log error if the response structure is missing expected properties
                        _logger.LogError("Invalid response structure; status property missing.");
                        throw new Exception("Response structure missing 'status' property.");
                    }
                }
            }
        }

    }
}
