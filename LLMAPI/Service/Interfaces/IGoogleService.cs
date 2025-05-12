using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Services.Interfaces
{
    /// <summary>
    /// Interface defining the contract for Google-specific services, primarily focused on image analysis using Google Vision API.
    /// </summary>
    public interface IGoogleService
    {
        /// <summary>
        /// Analyzes an image file uploaded via HTTP using Google Vision API Label Detection.
        /// </summary>
        /// <param name="imageFile">The image file uploaded as <see cref="IFormFile"/>.</param>
        /// <returns>A string describing the labels detected by Google Vision API.</returns>
        Task<string> AnalyzeImageGoogleVision(IFormFile imageFile);

        /// <summary>
        /// Analyzes an image from a given URL using Google Vision API Label Detection.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to be analyzed.</param>
        /// <returns>A string describing the labels detected by Google Vision API.</returns>
        Task<string> AnalyzeImageGoogleVision(string imageUrl);

        //Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes);
    }
}
