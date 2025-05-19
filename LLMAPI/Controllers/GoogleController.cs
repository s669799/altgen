using LLMAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using LLMAPI.DTO;
using LLMAPI.Service.Interfaces;

namespace LLMAPI.Controllers
{
    /// <summary>
    /// API controller for accessing Google-specific services, including Google Vision API for image analysis.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [ApiController]
    [Route("api/google")]
    public class GoogleController : ControllerBase
    {
        private readonly IGoogleService _googleService;
        private readonly ITextGenerationService _textService;
        private readonly IImageFileService _imageFileService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleController"/> class.
        /// </summary>
        /// <param name="googleService">Service for interacting with Google-specific APIs, like Google Vision.</param>
        /// <param name="textGenerationService">Service for text generation (though not directly used in this controller, it's kept for potential future text-based Google API interactions).</param>
        /// <param name="imageFileService">Service for handling image files, conversions, and reading.</param>
        public GoogleController(
            IGoogleService googleService,
            ITextGenerationService textGenerationService,
            IImageFileService imageFileService)
        {
            _googleService = googleService;
            _textService = textGenerationService;
            _imageFileService = imageFileService;
        }

        /// <summary>
        /// Analyzes an image from a given URL using Google Vision API to detect labels (objects, concepts, etc.).
        /// Returns a list of detected labels with their confidence scores.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to be analyzed using Google Vision API.</param>
        /// <returns>IActionResult containing a list of labels detected by Google Vision API in the 'AltText' property. Returns BadRequest if imageUrl is not provided, or StatusCode 500 for internal server errors.</returns>
        /// <response code="200">Returns the analysis result from Google Vision API.</response>
        /// <response code="400">Returns if the image URL is missing or invalid.</response>
        /// <response code="500">Returns if there is an internal server error during processing with Google Vision API.</response>
        [HttpPost("analyze-image-google-vision")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> AnalyzeImageGoogleVision([FromQuery] string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return BadRequest("Please provide a valid image URL.");
            }

            try
            {
                var altText = await _googleService.AnalyzeImageGoogleVision(imageUrl);
                return Ok(new { AltText = altText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        ///// <summary>
        ///// Analyzes an uploaded image using Google Vision API and generates a content description.
        ///// </summary>
        ///// <param name="imageFile">The image file to process.</param>
        ///// <returns>AI-generated content description or error message.</returns>
        //[HttpPost("analyze-image")]
        //[Consumes("multipart/form-data")]
        //public async Task<IActionResult> AnalyzeImage(string imageUrl)
        //{
        //    if (string.IsNullOrWhiteSpace(imageUrl))
        //    {
        //        return BadRequest("Please provide a valid image URL.");
        //    }

        //    try
        //    {
        //        // Parameters for the API call
        //        string projectId = "rich-world-450914-e6";
        //        string location = "europe-west4";
        //        string publisher = "google";
        //        string model = "gemini-2.0-flash-001";

        //        // Convert the uploaded image file to a ByteString
        //        var imageBytes = await _imageFileService.ReadImageFileAsync(imageUrl);

        //        // Call the GenerateContent method with necessary parameters
        //        var content = await _googleService.GenerateContent(projectId, location, publisher, model, imageBytes);

        //        return Ok(new { ImageContent = content });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        //// Text generation endpoint using Gemeni model (delegates to OpenRouterService)
        //[HttpPost("generate-text")]
        //public async Task<IActionResult> GenerateText(TextRequest request)
        //{
        //    if (string.IsNullOrEmpty(request?.Prompt))
        //    {
        //        return BadRequest("Prompt cannot be null or empty.");
        //    }

        //    var response = await _textService.GenerateText("google/gemini-flash-1.5-8b", request.Prompt);
        //    return Ok(new { response });
        //}

    }
}
