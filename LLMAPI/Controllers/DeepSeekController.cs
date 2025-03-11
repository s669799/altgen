//using LLMAPI.Services.Interfaces;
//using Microsoft.AspNetCore.Mvc;
//using System.Threading.Tasks;
//using Google.Protobuf;
//using Microsoft.AspNetCore.Http;
//using LLMAPI.Services.OpenRouter;
//using LLMAPI.Service.Interfaces;

//namespace LLMAPI.Controllers
//{
//    [ApiController]
//    [Route("api/deepseek")]
//    public class DeepSeekController : ControllerBase
//    {
//        private readonly IImageRecognitionService _imageRecognitionService;
//        private readonly ITextGenerationService _textService;
//        private readonly IImageFileService _imageFileService;

//        public DeepSeekController(
//            IImageRecognitionService imageRecognitionService,
//            ITextGenerationService textService,
//            IImageFileService imageFileService)
//        {
//            _imageRecognitionService = imageRecognitionService;
//            _textService = textService;
//            _imageFileService = imageFileService;
//        }

//        /// <summary>
//        /// Processes an image from a URL using DeepSeek and generates an alt text description.
//        /// </summary>
//        [HttpPost("analyze-image-url")]
//        public async Task<IActionResult> AnalyzeImageUrl([FromBody] ImageRequest request)
//        {
//            if (string.IsNullOrWhiteSpace(request?.ImageUrl))
//                return BadRequest("Please provide a valid image URL.");

//            try
//            {
//                var content = await _imageRecognitionService.AnalyzeImage("deepseek/deepseek-r1:free", request.ImageUrl);
//                return Ok(new { ImageContent = content });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, $"Internal server error: {ex.Message}");
//            }
//        }


//        ///// <summary>
//        ///// Processes an uploaded image file using DeepSeek and generates an alt text description.
//        ///// </summary>
//        //[HttpPost("analyze-image-file")]
//        //[Consumes("multipart/form-data")]
//        //public async Task<IActionResult> AnalyzeImageFile(IFormFile imageFile)
//        //{
//        //    if (imageFile == null || imageFile.Length == 0)
//        //        return BadRequest("Please upload a valid image file.");

//        //    try
//        //    {
//        //        var imageBytes = await _imageFileService.ConvertImageToByteString(imageFile);
//        //        var content = await _imageRecognitionService.AnalyzeImage("deepseek/deepseek-r1:free", imageBytes);
//        //        return Ok(new { ImageContent = content });
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return StatusCode(500, $"Internal server error: {ex.Message}");
//        //    }
//        //}


//        // Text generation endpoint using DeepSeek model (delegates to OpenRouterService)
//        [HttpPost("generate-text")]
//        public async Task<IActionResult> GenerateText(TextRequest request)
//        {
//            if (string.IsNullOrEmpty(request?.Prompt))
//            {
//                return BadRequest("Prompt cannot be null or empty.");
//            }

//            var response = await _textService.GenerateText("deepseek/deepseek-r1:free", request.Prompt);
//            return Ok(new { response });
//        }
//    }
//}
