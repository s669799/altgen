//using LLMAPI.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;
//using LLMAPI.DTO;
//using LLMAPI.Service.Interfaces;


//namespace LLMAPI.Controllers
//{
//    [ApiController]
//    [Route("api/google")]
//    public class OLDGoogleController : ControllerBase
//    {
//        private readonly IGoogleService _googleService;
//        private readonly ITextGenerationService _textService;
//        private readonly IImageFileService _imageFileService;

//        public OLDGoogleController(
//            IGoogleService googleService, 
//            ITextGenerationService textGenerationService, 
//            IImageFileService imageFileService)
//        {
//            _googleService = googleService;
//            _textService = textGenerationService;
//            _imageFileService = imageFileService;
//        }

//        /// <summary>
//        /// Analyzes an uploaded image using Google Vision API and generates a content description.
//        /// </summary>
//        /// <param name="imageFile">The image file to process.</param>
//        /// <returns>AI-generated content description or error message.</returns>
//        [HttpPost("analyze-image")]
//        [Consumes("multipart/form-data")]
//        public async Task<IActionResult> AnalyzeImage(string imageUrl)
//        {
//            if (string.IsNullOrWhiteSpace(imageUrl))
//            {
//            return BadRequest("Please provide a valid image URL.");
//            }

//            try
//            {   
//                // Parameters for the API call
//                string projectId = "rich-world-450914-e6";
//                string location = "europe-west4";
//                string publisher = "google";
//                string model = "gemini-2.0-flash-001";

//                // Convert the uploaded image file to a ByteString
//                var imageBytes = await _imageFileService.ReadImageFileAsync(imageUrl);

//                // Call the GenerateContent method with necessary parameters
//                var content = await _googleService.GenerateContent(projectId, location, publisher, model, imageBytes);

//                return Ok(new { ImageContent = content });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }

//        // Text generation endpoint using Gemeni model (delegates to OpenRouterService)
//        [HttpPost("generate-text")]
//        public async Task<IActionResult> GenerateText(TextRequest request)
//        {
//            if (string.IsNullOrEmpty(request?.Prompt))
//            {
//                return BadRequest("Prompt cannot be null or empty.");
//            }

//            var response = await _textService.GenerateText("google/gemini-flash-1.5-8b", request.Prompt);
//            return Ok(new { response });
//        }

//    }
//}
