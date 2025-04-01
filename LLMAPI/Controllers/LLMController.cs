using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
        private const string DefaultAltTextPrompt1 = "Write an alt text for this image.";
        private const string DefaultAltTextPrompt2 = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";


        /// <summary>
        /// Initializes a new instance of the <see cref="LLMController"/> class.
        /// </summary>
        /// <param name="imageRecognitionService">Service for performing image recognition tasks.</param>
        /// <param name="textService">Service for handling text generation requests.</param>
        /// <param name="imageFileService">Service for managing image files (currently not in use in this controller, but could be used for future image file operations).</param>
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
        /// Processes a LLM request using query parameters to specify the model, prompt, and/or image URL.
        /// At least a prompt or an image URL must be provided.
        /// </summary>
        /// <param name="model">The LLM model to use for processing the request. This is selected via a dropdown in Swagger UI.</param>
        /// <param name="prompt">Optional text prompt to send to the LLM for text generation or to guide image analysis. If not provided for image analysis, a default alt text prompt will be used.</param>
        /// <param name="imageUrl">Optional URL of an image to be analyzed by the LLM. If provided, the LLM will perform image recognition and analysis.</param>
        /// <returns>IActionResult containing the LLM's response in the `Response` property. Returns BadRequest if no prompt or imageUrl is provided, or StatusCode 500 for internal server errors.</returns>
        /// <response code="200">Returns the LLM response successfully.</response>
        /// <response code="400">Returns if the request is invalid, e.g., no prompt or image URL provided.</response>
        /// <response code="500">Returns if there is an internal server error during processing.</response>
        [HttpPost("process-request")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequest(
            [FromQuery] ModelType model,
            [FromQuery] string? prompt,
            [FromQuery] string? imageUrl)
        {
            // Validate input: At least one of `prompt` or `imageUrl` must be provided
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
                    string imagePrompt = string.IsNullOrWhiteSpace(prompt) ? DefaultAltTextPrompt1 : DefaultAltTextPrompt1 + prompt;
                    responseContent = await _imageRecognitionService.AnalyzeImage(modelString, imageUrl, imagePrompt);
                }
                else if (!string.IsNullOrWhiteSpace(prompt))
                {
                    responseContent = await _textService.GenerateText(modelString, prompt);
                }

                return Ok(new { Response = responseContent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a LLM request using the request body to send an <see cref="ImageRequest"/> object.
        /// This endpoint is suitable for sending requests via POST with a JSON body.
        /// </summary>
        /// <param name="request">The <see cref="ImageRequest"/> object in the request body. This object should contain the model, prompt, and/or image URL. At least a prompt or an image URL must be provided within the request body.</param>
        /// <returns>IActionResult containing the LLM's response in the `Response` property. Returns BadRequest if the request body is invalid or missing essential data, or StatusCode 500 for internal server errors.</returns>
        /// <response code="200">Returns the LLM response successfully.</response>
        /// <response code="400">Returns if the request body is invalid, e.g., missing prompt or image URL, or if the request object itself is null.</response>
        /// <response code="500">Returns if there is an internal server error during processing.</response>
        [HttpPost("process-request-body")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequestBody([FromBody] ImageRequest request)
        {
            // Validate input
            if (request == null || (string.IsNullOrWhiteSpace(request.Prompt) && string.IsNullOrWhiteSpace(request.ImageUrl)))
            {
                return BadRequest("Please provide at least a prompt or an image URL.");
            }

            try
            {
                string responseContent;
                string modelString = EnumHelper.GetEnumMemberValue(request.Model);

                if (!string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    // Use default prompt if not provided
                    string imagePrompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultAltTextPrompt1 : request.Prompt;
                    responseContent = await _imageRecognitionService.AnalyzeImage(modelString, request.ImageUrl, imagePrompt);
                }
                else
                {
                    responseContent = await _textService.GenerateText(modelString, request.Prompt);
                }

                return Ok(new { Response = responseContent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
