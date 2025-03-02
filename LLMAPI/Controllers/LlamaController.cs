using LLMAPI.DTO;
using LLMAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace LLMAPI.Controllers
{
    [ApiController]
    [Route("api/llama")]
    public class LlamaController : ControllerBase
    {
        private readonly ITextGenerationService _textService;

        public LlamaController(ITextGenerationService LlamaTextGenerationService)
        {
            _textService = LlamaTextGenerationService;
        }

        // Image recognition endpoint (currently not implemented)
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(FileUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Please upload a valid image.");
            }

            // Placeholder: Implement Meta Llama-based image recognition (not implemented)
            var altText = "Image recognition not implemented for Meta Llama";
            return Ok(new { altText });
        }

        // Text generation endpoint using Meta Llama model (delegates to OpenRouterService)
        [HttpPost("generate-text")]
        public async Task<IActionResult> GenerateText(PromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty.");
            }

            var response = await _textService.GenerateText("meta-llama/llama-3.3-70b-instruct", request.Prompt);
            return Ok(new { response });
        }
    }
}
