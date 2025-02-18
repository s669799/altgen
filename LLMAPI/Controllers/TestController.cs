using LLMAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LLMAPI.DTO;

namespace LLMAPI.Controllers
{
    /// <summary>
    /// Controller for handling different LLM models.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ILLMService _llmService;

        public TestController(ILLMService llmService)
        {
            _llmService = llmService;
        }

        /// <summary>
        /// Upload an image to extract alt text (labels) using Google Vision API.
        /// </summary>
        /// <param name="model">The image file uploaded by the user.</param>
        /// <returns>Returns the extracted alt text (labels) with confidence scores.</returns>
        [HttpPost("upload-image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] FileUploadModel model)
        {
            if (model.File == null || model.File.Length == 0)
            {
                return BadRequest("Please upload a valid image.");
            }

            var altText = await _llmService.GetDataFromImageGoogle(model.File);
            return Ok(new { altText });
        }

        /// <summary>
        /// Simple GET method to test the API.
        /// </summary>
        /// <returns>A welcome message.</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Hello, this is the API!" });
        }

        /// <summary>
        /// Endpoint for the google/gemini-flash-1.5-8b model.
        /// </summary>
        /// <param name="request">The prompt request body containing the prompt text.</param>
        /// <returns>The response from the model as a string.</returns>
        [HttpPost("google/gemini-flash-1.5-8b")]
        public async Task<IActionResult> Model1Response([FromBody] PromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty.");
            }

            string response = await _llmService.GetDataOpenRouter("google/gemini-flash-1.5-8b", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for the openai/gpt-4o-mini model.
        /// </summary>
        /// <param name="request">The prompt request body containing the prompt text.</param>
        /// <returns>The response from the model as a string.</returns>
        [HttpPost("openai/gpt-4o-mini")]
        public async Task<IActionResult> Model2Response([FromBody] PromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty.");
            }

            string response = await _llmService.GetDataOpenRouter("openai/gpt-4o-mini", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for the meta-llama/llama-3.3-70b-instruct model.
        /// </summary>
        /// <param name="request">The prompt request body containing the prompt text.</param>
        /// <returns>The response from the model as a string.</returns>
        [HttpPost("meta-llama/llama-3.3-70b-instruct")]
        public async Task<IActionResult> Model3Response([FromBody] PromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty.");
            }

            string response = await _llmService.GetDataOpenRouter("meta-llama/llama-3.3-70b-instruct", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for the deepseek/deepseek-r1 model.
        /// </summary>
        /// <param name="request">The prompt request body containing the prompt text.</param>
        /// <returns>The response from the model as a string.</returns>
        [HttpPost("deepseek/deepseek-r1")]
        public async Task<IActionResult> Model4Response([FromBody] PromptRequest request)
        {
            if (string.IsNullOrEmpty(request?.Prompt))
            {
                return BadRequest("Prompt cannot be null or empty.");
            }

            string response = await _llmService.GetDataOpenRouter("deepseek/deepseek-r1", request.Prompt);
            return Ok(new { response });
        }  
    }
}