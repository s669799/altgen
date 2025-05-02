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

        //private const string DefaultCnnAltTextPrompt = "Write an alt text for this image";
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


        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
        [HttpPost("predict")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> PredictAndAnalyze([FromBody] CNNRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImageUrl) || !ModelState.IsValid)
            {
                Console.WriteLine("Error: Incoming request is null, missing ImageUrl, or ModelState is invalid.");
                return BadRequest("Invalid request payload. Ensure ImageUrl is provided.");
            }

            try
            {
                Console.WriteLine($"Attempting to download image from URL: {request.ImageUrl}");
                var imageBytesBs = await _imageFileService.ReadImageFileAsync(request.ImageUrl);

                if (imageBytesBs == null || imageBytesBs.Length == 0)
                {
                    Console.WriteLine($"Error: Failed to download image or received empty data from URL: {request.ImageUrl}");
                    return BadRequest($"Could not download image from URL: {request.ImageUrl}. Ensure the URL is valid and accessible.");
                }
                Console.WriteLine($"Successfully downloaded image from URL: {request.ImageUrl}");

                string filename = Path.GetFileName(request.ImageUrl);
                if (string.IsNullOrWhiteSpace(filename)) filename = "image_from_url.jpg";

                Console.WriteLine($"Sending image to CNN for prediction...");
                CNNResponse cnnPrediction = await _cnnPredictionService.PredictAircraftAsync(imageBytesBs, filename);

                if (cnnPrediction == null || !cnnPrediction.Success)
                {
                    string cnnErrorDetail = cnnPrediction?.Detail ?? "CNN prediction service returned null or indicated failure.";
                    Console.WriteLine($"Error: CNN prediction failed. Details: {cnnErrorDetail}");

                    return StatusCode(500, $"CNN prediction failed: {cnnErrorDetail}");
       
                }
                Console.WriteLine($"CNN Prediction received: {cnnPrediction.PredictedAircraft} ({cnnPrediction.Probability:P1})");


                string modelString = EnumHelper.GetEnumMemberValue(request.Model);
                double temperature = request.Temperature ?? 1.0;
                string basePrompt = DefaultCnnAltTextPrompt;

                if (!string.IsNullOrWhiteSpace(request.Prompt) && !request.Prompt.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    basePrompt += ". " + request.Prompt;
                }

                Console.WriteLine($"Calling LLM service with image bytes and CNN context...");
                string llmResponse = await _imageRecognitionService.AnalyzeImage(
                    modelString,
                    imageBytesBs,
                    basePrompt,
                    cnnPrediction.PredictedAircraft,
                    cnnPrediction.Probability,
                    temperature);

                if (llmResponse.StartsWith("Error:") || llmResponse.StartsWith("Model returned"))
                {
                    Console.WriteLine($"Error: LLM Analysis failed: {llmResponse}");
                    return StatusCode(500, $"LLM analysis failed: {llmResponse}");
                }

                Console.WriteLine($"Successfully received LLM response.");
                return Ok(new { Response = llmResponse });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Fatal error during CNN-LLM workflow for URL {request.ImageUrl}: {ex.Message}");
                return StatusCode(500, "An unexpected internal server error occurred during the CNN-LLM workflow.");
            }
        }
    }
}
