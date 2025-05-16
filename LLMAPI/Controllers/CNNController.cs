using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.IO;
using Google.Protobuf;
namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/cnn-llm")]
    public class CNNController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly IImageFileService _imageFileService;
        private readonly ICnnPredictionService _cnnPredictionService;

        //private const string DefaultCnnAltTextPrompt = "Write an alt text for this image";
        private const string DefaultCnnAltTextPrompt = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";

        /// <summary>
        /// Initializes a new instance of the <see cref="CNNController"/> class.
        /// </summary>
        /// <param name="imageRecognitionService">The service for image recognition using LLMs.</param>
        /// <param name="imageFileService">The service for handling image files.</param>
        /// <param name="cnnPredictionService">The service for CNN predictions.</param>
        public CNNController(
            IImageRecognitionService imageRecognitionService,
            IImageFileService imageFileService,
            ICnnPredictionService cnnPredictionService)
        {
            _imageRecognitionService = imageRecognitionService;
            _imageFileService = imageFileService;
            _cnnPredictionService = cnnPredictionService;
        }

        /// <summary>
        /// Predicts the aircraft in an image using a CNN and then sends the image and CNN prediction to an LLM for analysis and alt text generation.
        /// </summary>
        /// <param name="request">The request containing the image URL and LLM parameters.</param>
        /// <returns>An action result containing the LLM generated alt text response.</returns>
        /// <response code="200">Returns the successfully generated alt text.</response>
        /// <response code="400">If the request payload is invalid or the image cannot be downloaded.</response>
        /// <response code="500">If an internal server error occurs during the CNN prediction or LLM analysis.</response>
       [DisableRequestSizeLimit]
[RequestFormLimits(MultipartBodyLengthLimit = 209_715_200)]
[HttpPost("predict")]
[ProducesResponseType(typeof(object), 200)]
[ProducesResponseType(typeof(string), 400)]
[ProducesResponseType(typeof(string), 500)]
public async Task<IActionResult> PredictAndAnalyze([FromForm] CNNRequest request)
{
    if (request == null || request.Image == null || request.Image.Length == 0 || !ModelState.IsValid)
    {
        Console.WriteLine("Error: Incoming request is invalid or missing image file.");
        return BadRequest("Invalid request. Ensure an image file is included.");
    }

    try
    {
        ByteString imageBytesBs;
        using (var ms = new MemoryStream())
        {
            await request.Image.CopyToAsync(ms);
            imageBytesBs = ByteString.CopyFrom(ms.ToArray());
        }

        if (imageBytesBs == null || imageBytesBs.Length == 0)
        {
            Console.WriteLine("Error: Received empty image data.");
            return BadRequest("Image data is empty or could not be processed.");
        }

        string filename = request.Image.FileName ?? "uploaded_image.jpg";
        Console.WriteLine($"Received image file: {filename}");

        // CNN prediction
        Console.WriteLine("Sending image to CNN for prediction...");
        CNNResponse cnnPrediction = await _cnnPredictionService.PredictAircraftAsync(imageBytesBs, filename);

        if (cnnPrediction == null || !cnnPrediction.Success)
        {
            string cnnErrorDetail = cnnPrediction?.Detail ?? "CNN prediction failed with no additional details.";
            Console.WriteLine($"Error: CNN prediction failed. Details: {cnnErrorDetail}");
            return StatusCode(500, $"CNN prediction failed: {cnnErrorDetail}");
        }

        Console.WriteLine($"CNN Prediction received: {cnnPrediction.PredictedAircraft} ({cnnPrediction.Probability:P1})");

        // LLM prompt composition
        string modelString = EnumHelper.GetEnumMemberValue(request.Model);
        double temperature = request.Temperature ?? 1.0;
        string basePrompt = DefaultCnnAltTextPrompt;

        if (!string.IsNullOrWhiteSpace(request.Prompt) && request.Prompt.ToLowerInvariant() != "string")
        {
            basePrompt += ". " + request.Prompt;
        }

        Console.WriteLine("Calling LLM service...");
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

        if (!string.IsNullOrWhiteSpace(llmResponse))
        {
            llmResponse = llmResponse.Replace("\\\"", "\"");
        }

        Console.WriteLine("Alt text generated successfully.");
        return Ok(new
        {
            Response = llmResponse,
            Aircraft = cnnPrediction.PredictedAircraft,
            Confidence = cnnPrediction.Probability
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[ERROR] Fatal error in CNN-LLM workflow: {ex}");
        return StatusCode(500, "An internal server error occurred during image processing.");
    }
}
    }
}
