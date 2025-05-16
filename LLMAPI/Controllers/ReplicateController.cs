using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using LLMAPI.Service.Interfaces;
using LLMAPI.Services.Interfaces;
using LLMAPI.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Google.Protobuf;
using System.Linq;
using System.ComponentModel.DataAnnotations;


namespace LLMAPI.Controllers
{
    /// <summary>
    /// API controller for interacting with the Replicate API, providing image analysis with an optional CNN cognitive layer.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    [Route("api/replicate")]
    public class ReplicateController : ControllerBase
    {
        private readonly IReplicateService _replicateService;
        private readonly IImageFileService _imageFileService;
        private readonly ICnnPredictionService _cnnPredictionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReplicateController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplicateController"/> class.
        /// </summary>
        /// <param name="replicateService">The service for interacting with the Replicate API.</param>
        /// <param name="imageFileService">The service for handling image files (downloading from URL).</param>
        /// <param name="cnnPredictionService">The service for CNN prediction (cognitive layer).</param>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="logger">The logger for this controller.</param>
        public ReplicateController(
            IReplicateService replicateService,
            IImageFileService imageFileService,
            ICnnPredictionService cnnPredictionService,
            IConfiguration configuration,
            ILogger<ReplicateController> logger)
        {
            _replicateService = replicateService;
            _imageFileService = imageFileService;
            _cnnPredictionService = cnnPredictionService;
            _configuration = configuration;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Runs a specific Replicate model instance, optionally using a CNN prediction as a cognitive layer for the prompt.
        /// Currently configured to use the 'e5caf557dd9e5dcee46442e1315291ef1867f027991ede8ff95e304d4f734200' model version.
        /// </summary>
        /// <param name="input">The request body containing the image URL and optional prompt, temperature, and cognitive layer preference.</param>
        /// <returns>An action result containing the generated output from the Replicate model if successful.</returns>
        /// <response code="200">Returns the successfully generated model output.</response>
        /// <response code="400">If the input is invalid or the image URL is missing.</response>
        /// <response code="500">If an internal server error occurs during communication with services or processing.</response>
        [HttpPost("RunModel")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> RunModel([FromForm] ReplicateRequest input)
        {
            if (input == null || input.Image == null)
            {
                _logger.LogWarning("RunModel called with null input or missing image URL.");
                return BadRequest("Input or Image URL cannot be null.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string modelVersion = "e5caf557dd9e5dcee46442e1315291ef1867f027991ede8ff95e304d4f734200";

            const string basePrompt = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";

            string fileName = input.Image.FileName;

            string cnnContext = "";
            string noYappingInstruction = "Be specific about model and variant of depicted object if applicable. Do not mention this context in the alt text.";

            if (input.EnableCognitiveLayer ?? true)
            {
                ByteString imageBytes;
                try
                {
                    _logger.LogInformation("Cognitive Layer enabled. Downloading image from URL: {ImageUrl}", input.Image);
                    imageBytes = await _imageFileService.ConvertImageToByteString(input.Image);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download image for CNN from {ImageUrl}", input.Image);
                    return StatusCode(500, $"Failed to download image for CNN: {ex.Message}");
                }

                CNNResponse cnnResponse;
                try
                {
                    _logger.LogInformation("Sending image to CNN prediction service.");
                    cnnResponse = await _cnnPredictionService.PredictAircraftAsync(imageBytes, fileName);

                    if (!cnnResponse.Success)
                    {
                        _logger.LogError("CNN prediction failed. Details: {Detail}", cnnResponse.Detail);
                        return StatusCode(500, $"CNN prediction failed: {cnnResponse.Detail}");
                    }
                    _logger.LogInformation("CNN prediction succeeded: {PredictedAircraft} ({Probability:P1})", cnnResponse.PredictedAircraft, cnnResponse.Probability);

                    if (!string.IsNullOrWhiteSpace(cnnResponse.PredictedAircraft) && cnnResponse.Probability.HasValue)
                    {
                        cnnContext = $"Preceding analysis context: Identified primary subject as {cnnResponse.PredictedAircraft} with probability {cnnResponse.Probability.Value:P1}. ";
                    }
                    else if (!string.IsNullOrWhiteSpace(cnnResponse.PredictedAircraft))
                    {
                        cnnContext = $"Preceding analysis context: Identified primary subject as {cnnResponse.PredictedAircraft}. ";
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during CNN prediction.");
                    return StatusCode(500, $"Error during CNN prediction: {ex.Message}");
                }
            }
            else
            {
                _logger.LogInformation("Cognitive Layer disabled. Skipping CNN prediction.");
            }

            string compositePrompt = cnnContext;

            if (!string.IsNullOrWhiteSpace(cnnContext) || !string.IsNullOrWhiteSpace(basePrompt) || !string.IsNullOrWhiteSpace(input.Prompt))
            {
                compositePrompt += noYappingInstruction;
            }

            compositePrompt += " " + basePrompt;

            if (!string.IsNullOrWhiteSpace(input.Prompt) && !input.Prompt.Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(basePrompt))
                {
                    compositePrompt += ". " + input.Prompt;
                }
                else
                {
                    compositePrompt += input.Prompt;
                }
            }

            var replicateInput = new Dictionary<string, object>
            {
                { "image", input.Image },
                { "prompt", compositePrompt },
                { "temperature", input.Temperature ?? 1.0 }
            };

            try
            {
                _logger.LogInformation("Creating prediction on Replicate for model version: {ModelVersion}");
                string predictionId = await _replicateService.CreatePrediction(modelVersion, replicateInput);

                _logger.LogInformation("Polling for Replicate prediction result with ID: {PredictionId}", predictionId);
                var finalOutput = await PollForPredictionResult(predictionId);

                _logger.LogInformation("Replicate prediction completed successfully with output.");
                return Ok(new { finalOutput });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while running Replicate model.");
                return StatusCode(500, $"Internal Server Error running Replicate model: {ex.Message}");
            }
        }

        /// <summary>
        /// Polls the Replicate API for the result of a prediction until it is completed (succeeded or failed).
        /// </summary>
        /// <param name="predictionId">The ID of the prediction to poll.</param>
        /// <returns>The raw JSON output string from the successful prediction.</returns>
        /// <exception cref="Exception">Thrown if the prediction fails or encounters an unexpected status.</exception>
        private async Task<string> PollForPredictionResult(string predictionId)
        {
            while (true)
            {
                _logger.LogInformation("Fetching Replicate prediction result for ID: {PredictionId}");

                var result = await _replicateService.GetPredictionResult(predictionId);

                using (JsonDocument document = JsonDocument.Parse(result))
                {
                    JsonElement root = document.RootElement;

                    if (root.TryGetProperty("status", out JsonElement statusElement))
                    {
                        string status = statusElement.GetString();

                        _logger.LogInformation("Replicate Prediction status: {Status}", status);

                        switch (status)
                        {
                            case "succeeded":
                                if (root.TryGetProperty("output", out JsonElement outputElement))
                                {
                                    _logger.LogInformation("Replicate Prediction succeeded.");
                                    return outputElement.GetRawText();
                                }
                                _logger.LogError("Replicate Prediction completed but no output was found.");
                                throw new Exception("Replicate Prediction succeeded without output.");

                            case "starting":
                            case "processing":
                                _logger.LogInformation("Replicate Prediction in progress (status: {Status}). Waiting before retry.", status);
                                await Task.Delay(2000);
                                break;

                            case "failed":
                                var errorMessage = root.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : "Unknown error.";
                                _logger.LogError("Replicate Prediction failed with message: {ErrorMessage}", errorMessage);
                                throw new Exception($"Replicate Prediction failed: {errorMessage}");

                            default:
                                _logger.LogWarning("Encountered unexpected Replicate status: {Status}", status);
                                throw new Exception($"Replicate Prediction encountered unexpected status: {status}");
                        }
                    }
                    else
                    {
                        _logger.LogError("Invalid Replicate response structure; status property missing.");
                        throw new Exception("Replicate response structure missing 'status' property.");
                    }
                }
            }
        }
    }
}
