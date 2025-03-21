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
    [ApiController]
    [Route("api/llm")]
    public class LLMController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly ITextGenerationService _textService;
        private readonly IImageFileService _imageFileService;

        // Default prompt for image analysis.
        private const string DefaultAltTextPrompt = "Write a brief, one to two sentence alt text description for this image, Harvard style, that captures the main subjects, action, and setting. This is an alt text for an end user.";

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
        /// Processes a text or image request based on the provided model, prompt, and image URL.
        /// </summary>
        [HttpPost("process-request")]
        public async Task<IActionResult> ProcessRequest(
            [FromQuery] ModelType model,  // Dropdown in Swagger
            [FromQuery] string? prompt,   // Optional text prompt
            [FromQuery] string? imageUrl) // Optional image URL
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
                    // Use default prompt if user didn't provide one
                    string imagePrompt = string.IsNullOrWhiteSpace(prompt) ? DefaultAltTextPrompt : prompt;
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

        [HttpPost("process-request-body")]
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
                    string imagePrompt = string.IsNullOrWhiteSpace(request.Prompt) ? DefaultAltTextPrompt : request.Prompt;
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
