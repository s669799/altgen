using LLMAPI.DTO;
using Google.Protobuf;
using System.Threading.Tasks;

namespace LLMAPI.Service.Interfaces
{
    /// <summary>
    /// Interface for services that interact with a CNN prediction endpoint (like Gradio).
    /// </summary>
    public interface ICnnPredictionService
    {
        /// <summary>
        /// Sends image data to the CNN prediction service and returns the prediction result.
        /// </summary>
        /// <param name="imageBytes">The image content as a ByteString.</param>
        /// <param name="fileName">The original filename of the image (used for content type/filename in request).</param>
        /// <returns>A <see cref="CnnPredictionResponse"/> containing the prediction results.</returns>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request to the CNN endpoint fails.</exception>
        /// <exception cref="JsonException">Thrown if deserialization of the CNN response fails.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors.</exception>
        Task<CNNResponse> PredictAircraftAsync(ByteString imageBytes, string fileName);
    }
}
