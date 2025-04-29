// LLMAPI.Controllers/CNNController.cs
using LLMAPI.DTO; // Need CNNWorkflowRequest, CnnPredictResponse
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.IO; // Used for Path.GetFileName

namespace LLMAPI.Controllers
{
    // API controller for the CNN-enhanced LLM workflow.
    [ApiController]
    [Route("api/cnn-llm")] // New route specifically for this workflow
    public class CNNController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly IImageFileService _imageFileService;
        private readonly ICnnPredictionService _cnnPredictionService;

        // Default prompt for the LLM when using CNN context
        private const string DefaultCnnAltTextPrompt = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";

        public CNNController(
            IImageRecognitionService imageRecognitionService,
            IImageFileService imageFileService,
            ICnnPredictionService cnnPredictionService)
        {
            _imageRecognitionService = imageRecognitionService;
            _imageFileService = imageFileService;
            _cnnPredictionService = cnnPredictionService;
        }

        // Processes an image URL through CNN and then LLM.
        // Endpoint: POST /api/cnn-llm/predict-and-analyze
        [DisableRequestSizeLimit] // Allows potentially large image downloads
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)] // Example limit
        [HttpPost("predict-and-analyze")] // Clearer endpoint name
        [ProducesResponseType(typeof(object), 200)] // Success response structure
        [ProducesResponseType(typeof(string), 400)] // Bad Request response
        [ProducesResponseType(typeof(string), 500)] // Internal Server Error response
        public async Task<IActionResult> PredictAndAnalyze([FromBody] CNNRequest request) // Uses the CNNWorkflowRequest DTO
        {
            // Basic validation checks
            if (request == null || string.IsNullOrWhiteSpace(request.ImageUrl) || !ModelState.IsValid)
            {
                Console.WriteLine("Error: Incoming request is null, missing ImageUrl, or ModelState is invalid.");
                return BadRequest("Invalid request payload. Ensure ImageUrl is provided.");
            }

            try
            {
                // 1. Download the image bytes
                Console.WriteLine($"Attempting to download image from URL: {request.ImageUrl}");
                var imageBytesBs = await _imageFileService.ReadImageFileAsync(request.ImageUrl);

                // Handle download failure - ReadImageFileAsync returns null on error
                if (imageBytesBs == null || imageBytesBs.Length == 0)
                {
                    Console.WriteLine($"Error: Failed to download image or received empty data from URL: {request.ImageUrl}");
                    return BadRequest($"Could not download image from URL: {request.ImageUrl}. Ensure the URL is valid and accessible.");
                }
                Console.WriteLine($"Successfully downloaded image from URL: {request.ImageUrl}");

                // Get filename from URL for CNN service
                string filename = Path.GetFileName(request.ImageUrl);
                if (string.IsNullOrWhiteSpace(filename)) filename = "image_from_url.jpg"; // Default name if URL has no filename

                // 2. Send image bytes to the CNN prediction service
                Console.WriteLine($"Sending image to CNN for prediction...");
                CNNResponse cnnPrediction = await _cnnPredictionService.PredictAircraftAsync(imageBytesBs, filename);

                // Handle CNN prediction service failure or negative success flag in response
                if (cnnPrediction == null || !cnnPrediction.Success)
                {
                    string cnnErrorDetail = cnnPrediction?.Detail ?? "CNN prediction service returned null or indicated failure.";
                    Console.WriteLine($"Error: CNN prediction failed. Details: {cnnErrorDetail}");
                    // Decide how to handle CNN failure:
                    // Option A: Break and return error to client
                    return StatusCode(500, $"CNN prediction failed: {cnnErrorDetail}");
                    // Option B: Proceed to LLM without CNN context (requires LLM AnalyzeImage overload that handles nulls)
                    // For simplicity in this response, we'll stick to Option A based on your error handling style.
                }
                Console.WriteLine($"CNN Prediction received: {cnnPrediction.PredictedAircraft} ({cnnPrediction.Probability:P1})");


                // 3. Prepare prompt and call LLM service with image bytes and CNN context
                string modelString = EnumHelper.GetEnumMemberValue(request.Model);
                double temperature = request.Temperature ?? 1.0; // Use default if null
                string basePrompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultCnnAltTextPrompt : request.Prompt;

                Console.WriteLine($"Calling LLM service with image bytes and CNN context...");
                // Call the AnalyzeImage overload that accepts ByteString
                string llmResponse = await _imageRecognitionService.AnalyzeImage(
                    modelString,
                    imageBytesBs, // Pass downloaded image bytes
                    basePrompt,
                    cnnPrediction.PredictedAircraft, // Pass prediction result from CNN
                    cnnPrediction.Probability,     // Pass probability from CNN
                    temperature);

                // Check LLM Response (assuming it returns error strings on failure for brevity)
                if (llmResponse.StartsWith("Error:") || llmResponse.StartsWith("Model returned")) // Match your return patterns
                {
                    Console.WriteLine($"Error: LLM Analysis failed: {llmResponse}");
                    return StatusCode(500, $"LLM analysis failed: {llmResponse}"); // Return LLM error message
                }

                Console.WriteLine($"Successfully received LLM response.");
                // 4. Return LLM response
                return Ok(new { Response = llmResponse });
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors during orchestration
                Console.Error.WriteLine($"[ERROR] Fatal error during CNN-LLM workflow for URL {request.ImageUrl}: {ex.Message}");
                // Return a generic 500 error, matching your style
                return StatusCode(500, "An unexpected internal server error occurred during the CNN-LLM workflow.");
            }
        }
    }
}
