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
    /// <summary>
    /// API controller for interacting with the Replicate service to run machine learning models.
    /// This controller allows running models hosted on Replicate by sending image and text prompts.
    /// </summary>
    [ApiController]
    [Route("api/replicate")]
    public class ReplicateController : ControllerBase
    {
        private readonly IReplicateService _replicateService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReplicateController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicateController"/> class.
        /// </summary>
        /// <param name="replicateService">Service for interacting with the Replicate API.</param>
        /// <param name="configuration">Application configuration for accessing settings like Replicate API key.</param>
        /// <param name="logger">Logger for logging controller actions and errors.</param>
        public ReplicateController(IReplicateService replicateService, IConfiguration configuration, ILogger<ReplicateController> logger)
        {
            _replicateService = replicateService;
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs a specified machine learning model on Replicate with provided image and prompt.
        /// This endpoint creates a prediction on Replicate and polls for its result.
        /// </summary>
        /// <param name="input">A <see cref="ReplicateRequest"/> object containing the image URL and optional prompt for the model.</param>
        /// <returns>IActionResult containing the final output from the Replicate model in the 'finalOutput' property. Returns BadRequest if input is null, or StatusCode 500 for internal server errors or Replicate API failures.</returns>
        /// <response code="200">Returns the output from the Replicate model.</response>
        /// <response code="400">Returns if the input request is null or invalid.</response>
        /// <response code="500">Returns if there is an internal server error or an error communicating with the Replicate API.</response>
        [HttpPost("RunModel")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
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

        /// <summary>
        /// Polls the Replicate API for the result of a prediction until it is completed, failed, or an error occurs.
        /// </summary>
        /// <param name="predictionId">The ID of the prediction to poll for.</param>
        /// <returns>A string representing the final output of the prediction if successful.</returns>
        /// <exception cref="Exception">Thrown if the prediction fails, times out, or encounters an unexpected status or response structure.</exception>
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
