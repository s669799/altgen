using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces; // Removed duplicate if present
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations; // Keep for Range attribute

namespace LLMAPI.Controllers
{
    /// <summary>
    /// API controller for handling LLM requests, including text generation and image analysis.
    /// </summary>
    [ApiController]
    [Route("api/llm")]
    public class LLMController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly ITextGenerationService _textService;
        private readonly IImageFileService _imageFileService;

        /// <summary>
        /// Default prompt used for image analysis when no specific prompt is provided in the request.
        /// This prompt is designed to generate accessible and informative alt text descriptions for images, following web accessibility best practices.
        /// </summary>
        // private const string DefaultAltTextPrompt1 = "Write an alt text for this image."; // No longer primary
        private const string DefaultAltTextPrompt2 = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";


        /// <summary>
        /// Initializes a new instance of the <see cref="LLMController"/> class.
        /// </summary>
        /// <param name="imageRecognitionService">Service for performing image recognition tasks.</param>
        /// <param name="textService">Service for handling text generation requests.</param>
        /// <param name="imageFileService">Service for managing image files.</param>
        public LLMController(
            IImageRecognitionService imageRecognitionService,
            ITextGenerationService textService,
            IImageFileService imageFileService)
        {
            _imageRecognitionService = imageRecognitionService;
            _textService = textService;
            _imageFileService = imageFileService;
        }

        /// <summary>
        /// Processes a LLM request using query parameters. Primarily for text generation or simple image URL analysis without CNN context.
        /// </summary>
        /// <param name="prompt">Optional text prompt. If analyzing an image, this is combined with the default prompt.</param>
        /// <param name="imageUrl">Optional URL of an image to be analyzed.</param>
        /// <param name="model">The LLM model to use.</param>
        /// <param name="temperature">Optional temperature setting (0.0-2.0).</param>
        /// <returns>IActionResult containing the LLM's response.</returns>
        [HttpPost("process-request")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequest(
            [FromQuery] string? prompt,
            [FromQuery] string? imageUrl,
            [FromQuery] ModelType model = ModelType.ChatGpt4o,
            [FromQuery][Range(0.0, 2.0)] double temperature = 1.0)
        {
            if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest("Please provide at least a prompt or an image URL.");
            }

            try
            {
                string responseContent = string.Empty;
                string modelString = EnumHelper.GetEnumMemberValue(model);

                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    string imagePrompt = DefaultAltTextPrompt2 + (string.IsNullOrWhiteSpace(prompt) ? "" : " User instruction: " + prompt);
                    responseContent = await _imageRecognitionService.AnalyzeImage(modelString, imageUrl, imagePrompt, null, null, temperature);
                }
                else if (!string.IsNullOrWhiteSpace(prompt))
                {
                    responseContent = await _textService.GenerateText(modelString, prompt);
                }

                return Ok(new { Response = responseContent });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessRequest: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a LLM request using the request body, accepting <see cref="ImageRequest"/> JSON which can include CNN predictions.
        /// </summary>
        /// <param name="request">The <see cref="ImageRequest"/> object containing model, prompt, image URL, temperature, and optional CNN predictions.</param>
        /// <returns>IActionResult containing the LLM's response.</returns>
        [HttpPost("process-request-body")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequestBody([FromBody] ImageRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(request.Prompt) && string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                return BadRequest("Please provide at least a prompt or an image URL in the request body.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            try
            {
                string responseContent;
                string modelString = EnumHelper.GetEnumMemberValue(request.Model);
                double temperature = request.Temperature ?? 1.0;

                if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    string baseImagePrompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultAltTextPrompt2: request.Prompt;

                    responseContent = await _imageRecognitionService.AnalyzeImage(
                        modelString,
                        request.ImageUrl,
                        baseImagePrompt,
                        request.PredictedAircraft,
                        request.Probability,
                        temperature);
                }
                else
                {
                    responseContent = await _textService.GenerateText(modelString, request.Prompt!);
                }

                return Ok(new { Response = responseContent });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessRequestBody: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
