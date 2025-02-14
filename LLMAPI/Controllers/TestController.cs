using LLMAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LLMAPI.Controllers
{
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
        /// <param name="imageFile">Image file uploaded by user</param>
        /// <returns>Alt text (labels) with confidence scores</returns>
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Please upload a valid image.");
            }

            var altText = await _llmService.GetDataFromImageGoogle(imageFile);
            return Ok(new { altText });
        }

        /// <summary>
        /// Simple get method to test the API
        /// </summary>
        /// <returns>A welcome message</returns>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Hello, this is the API!" });
        }

        /// <summary>
        /// Endpoint for google/gemini-flash-1.5-8b model
        /// </summary>
        /// <param name="request">Prompt request body</param>
        /// <returns>Model response</returns>
        [HttpPost("google/gemini-flash-1.5-8b")]
        public async Task<IActionResult> Model1Response([FromBody] PromptRequest request)
        {
            string response = await _llmService.GetDataOpenRouter("google/gemini-flash-1.5-8b", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for openai/gpt-4o-mini model
        /// </summary>
        /// <param name="request">Prompt request body</param>
        /// <returns>Model response</returns>
        [HttpPost("openai/gpt-4o-mini")]
        public async Task<IActionResult> Model2Response([FromBody] PromptRequest request)
        {
            string response = await _llmService.GetDataOpenRouter("openai/gpt-4o-mini", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for meta-llama/llama-3.3-70b-instruct model
        /// </summary>
        /// <param name="request">Prompt request body</param>
        /// <returns>Model response</returns>
        [HttpPost("meta-llama/llama-3.3-70b-instruct")]
        public async Task<IActionResult> Model3Response([FromBody] PromptRequest request)
        {
            string response = await _llmService.GetDataOpenRouter("meta-llama/llama-3.3-70b-instruct", request.Prompt);
            return Ok(new { response });
        }

        /// <summary>
        /// Endpoint for deepseek/deepseek-r1 model
        /// </summary>
        /// <param name="request">Prompt request body</param>
        /// <returns>Model response</returns>
        [HttpPost("deepseek/deepseek-r1")]
        public async Task<IActionResult> Model4Response([FromBody] PromptRequest request)
        {
            string response = await _llmService.GetDataOpenRouter("deepseek/deepseek-r1", request.Prompt);
            return Ok(new { response });
        }

        // DTO class for prompt request
        public class PromptRequest
        {
            public string? Prompt { get; set; }
        }
    }
}