using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Helpers;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/llm")]
    public class LLMController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly ITextGenerationService _textService;
        private readonly IImageFileService _imageFileService;

        private const string DefaultAltTextPrompt = "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar.";

        public LLMController(
            IImageRecognitionService imageRecognitionService,
            ITextGenerationService textService,
            IImageFileService imageFileService)
        {
            _imageRecognitionService = imageRecognitionService;
            _textService = textService;
            _imageFileService = imageFileService;
        }

        [HttpPost("process-form")]
[DisableRequestSizeLimit]
[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
[ProducesResponseType(typeof(object), 200)]
[ProducesResponseType(typeof(string), 400)]
[ProducesResponseType(typeof(string), 500)]
public async Task<IActionResult> ProcessForm([FromForm] LLMFormRequest request)
{
    if (request.Image == null || request.Image.Length == 0)
        return BadRequest("No image provided.");

    var imageBytes = await _imageFileService.ConvertImageToByteString(request.Image);
    if (imageBytes == null || imageBytes.Length == 0)
        return BadRequest("Could not convert image to bytes.");

    string modelString = EnumHelper.GetEnumMemberValue(Enum.Parse<ModelType>(request.Model));
    string finalPrompt = DefaultAltTextPrompt;
    if (!string.IsNullOrWhiteSpace(request.Prompt))
        finalPrompt += ". " + request.Prompt;

    var result = await _imageRecognitionService.AnalyzeImage(
        modelString,
        imageBytes,
        finalPrompt,
        null,
        null,
        request.Temperature ?? 1.0
    );

    return Ok(new { response = result });
}
    }
}
