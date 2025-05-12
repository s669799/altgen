using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Google.Api.Gax.Grpc;
using LLMAPI.Service.Interfaces;
using System.Linq;

namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/llm")]
    public class LLMController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly ITextGenerationService _textService;
        private readonly IImageFileService _imageFileService;

        private const string DefaultAltTextPrompt2 = "Write an alt text for this image";
        //private const string DefaultAltTextPrompt2 = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";

        public LLMController(
            IImageRecognitionService imageRecognitionService,
            ITextGenerationService textService,
            IImageFileService imageFileService)
        {
            _imageRecognitionService = imageRecognitionService;
            _textService = textService;
            _imageFileService = imageFileService;
        }

        [HttpPost("process-request")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequest(
            [FromQuery] string? prompt,
            [FromQuery] string? imageUrl,
            [FromQuery] ModelType model = ModelType.ChatGpt4_1,
            [FromQuery][Range(0.0, 2.0)] double temperature = 1.0)
        {
            if (string.IsNullOrWhiteSpace(prompt) && string.IsNullOrWhiteSpace(imageUrl))
            {
                Console.WriteLine("Error: Neither prompt nor imageUrl provided in query.");
                return BadRequest("Please provide at least a prompt or an image URL.");
            }

            try
            {
                string responseContent = string.Empty;
                string modelString = EnumHelper.GetEnumMemberValue(model);

                if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    string combinedPrompt = DefaultAltTextPrompt2;
                    if (!string.IsNullOrWhiteSpace(prompt) && !prompt.Equals("string", StringComparison.OrdinalIgnoreCase))
                    {
                        combinedPrompt += ". " + prompt;
                    }

                    responseContent = await _imageRecognitionService.AnalyzeImage(modelString, imageUrl, combinedPrompt, temperature);
                }
                else if (!string.IsNullOrWhiteSpace(prompt))
                {
                    responseContent = await _textService.GenerateText(modelString, prompt);
                }
                else
                {
                    Console.WriteLine("Error: Unexpected state where neither ImageUrl nor Prompt is valid after initial query checks.");
                    return BadRequest("Internal logic error: Neither prompt nor image URL is valid.");
                }

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    responseContent = responseContent.Replace("\\\"", "\"");
                }

                return Ok(new { Response = responseContent });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessRequest: {ex}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("process-request-body")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> ProcessRequestBody([FromBody] LLMRequest request)
        {
            if (request == null)
            {
                Console.WriteLine("Error: Request body is null.");
                return BadRequest("Request body cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(request.Prompt) && string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                Console.WriteLine("Error: Neither Prompt nor ImageUrl provided in request body.");
                return BadRequest("Please provide at least a prompt or an image URL in the request body.");
            }
            if (!ModelState.IsValid)
            {
                Console.WriteLine($"Error: ModelState is invalid: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return BadRequest(ModelState);
            }

            try
            {
                string responseContent;
                string modelString = EnumHelper.GetEnumMemberValue(request.Model);
                double temperature = request.Temperature ?? 1.0;

                string finalPromptForLLM;

                if (!string.IsNullOrWhiteSpace(request.ImageUrl) && !request.ImageUrl.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    finalPromptForLLM = DefaultAltTextPrompt2;

                    if (!string.IsNullOrWhiteSpace(request.Prompt) && !request.Prompt.Equals("string", StringComparison.OrdinalIgnoreCase))
                    {
                        finalPromptForLLM += " " + request.Prompt;
                    }

                    responseContent = await _imageRecognitionService.AnalyzeImage(
                        modelString,
                        request.ImageUrl,
                        finalPromptForLLM,
                        temperature);
                }
                else if (!string.IsNullOrWhiteSpace(request.Prompt))
                {
                    finalPromptForLLM = request.Prompt;
                    responseContent = await _textService.GenerateText(modelString, finalPromptForLLM);
                }
                else
                {
                    Console.WriteLine("Error: Unexpected state where neither ImageUrl nor Prompt is valid after initial body checks.");
                    return BadRequest("Internal logic error: Neither prompt nor image URL is valid.");
                }
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    responseContent = responseContent.Replace("\\\"", "\"");
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
