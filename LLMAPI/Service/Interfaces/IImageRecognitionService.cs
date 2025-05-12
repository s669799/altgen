using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Services.Interfaces
{
    /// <summary>
    /// Interface defining the contract for image recognition services.
    /// </summary>
    public interface IImageRecognitionService
    {
        /// <summary>
        /// Analyzes an image given its URL, without pre-calculated CNN context.
        /// </summary>
        /// <param name="model">The model identifier (e.g., "openai/gpt-4o").</param>
        /// <param name="imageUrl">The image URL.</param>
        /// <param name="textPrompt">Base text prompt to guide image analysis.</param>
        /// <param name="temperature">Optional temperature parameter [0.0, 2.0].</param>
        /// <returns>The image description returned by the model.</returns>
        Task<string> AnalyzeImage(
            string model,
            string imageUrl,
            string textPrompt,
            double temperature = 1.0);

        /// <summary>
        /// Analyzes an image given its URL, potentially using context from a CNN prediction.
        /// </summary>
        /// <param name="model">The model identifier (e.g., "openai/gpt-4o").</param>
        /// <param name="imageUrl">The image URL.</param>
        /// <param name="textPrompt">Base text prompt to guide image analysis.</param>
        /// <param name="predictedAircraft">Optional predicted aircraft type from CNN.</param>
        /// <param name="probability">Optional probability of the CNN prediction.</param>
        /// <param name="temperature">Optional temperature parameter [0.0, 2.0].</param>
        /// <returns>The image description returned by the model.</returns>
        Task<string> AnalyzeImage(
            string model,
            string imageUrl,
            string textPrompt,
            string? predictedAircraft,
            double? probability,
            double temperature = 1.0);

        /// <summary>
        /// Analyzes an image given its file content as ByteString, potentially using context from a CNN prediction.
        /// </summary>
        /// <param name="model">The model identifier.</param>
        /// <param name="imageBytes">The image content in ByteString format.</param>
        /// <param name="textPrompt">Base text prompt to guide image analysis.</param>
        /// <param name="predictedAircraft">Optional predicted aircraft type from CNN.</param>
        /// <param name="probability">Optional probability of the CNN prediction.</param>
        /// <param name="temperature">Optional temperature parameter [0.0, 2.0].</param>
        /// <returns>The image description returned by the model.</returns>
        Task<string> AnalyzeImage(
            string model,
            ByteString imageBytes,
            string textPrompt,
            string? predictedAircraft,
            double? probability,
            double temperature = 1.0);
    }
}
