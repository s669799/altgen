using LLMAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;

namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/claude")]
    public class ClaudeController : ControllerBase
    {
        private readonly IImageRecognitionService _imageRecognitionService;

        public ClaudeController(IImageRecognitionService OpenRouterService)
        {
            _imageRecognitionService = OpenRouterService;
        }

        /// <summary>
        /// Processes an image from a URL using Claude and generates an alt text description.
        /// </summary>
        [HttpPost("analyze-image-url")]
        public async Task<IActionResult> AnalyzeImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return BadRequest("Please provide a valid image URL.");

            try
            {
                var content = await _imageRecognitionService.AnalyzeImage("google/gemini-2.0-flash-lite-preview-02-05:free", imageUrl);
                return Ok(new { ImageContent = content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

/*         /// <summary>
        /// Processes an uploaded image file using Claude and generates an alt text description.
        /// </summary>
        [HttpPost("analyze-image-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AnalyzeImageFile(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return BadRequest("Please upload a valid image file.");

            try
            {
                var imageBytes = await _imageRecognitionService.ConvertImageToByteString(imageFile);
                var content = await _imageRecognitionService.AnalyzeImage("anthropic/claude-3.5-sonnet", imageBytes);
                return Ok(new { ImageContent = content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        } */
    }
}
