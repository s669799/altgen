using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Services.Interfaces
{
    public interface IImageRecognitionService
    {
        /// <summary>
        /// Analyzes an image given its URL.
        /// </summary>
        /// <param name="model">The model identifier.</param>
        /// <param name="imageUrl">The image URL.</param>
        /// <returns>The image description returned by the model.</returns>
        Task<string> AnalyzeImage(string model, string imageUrl, string textPrompt = null);


        ///// <summary>
        ///// Analyzes an image given its file content as ByteString.
        ///// </summary>
        ///// <param name="model">The model identifier.</param>
        ///// <param name="imageBytes">The image content in ByteString format.</param>
        ///// <returns>The image description returned by the model.</returns>
        //Task<string> AnalyzeImage(string model, ByteString imageBytes);

        ///// <summary>
        ///// Analyzes an image by converting a URL to a base64-encoded string and using it in the payload.
        ///// </summary>
        ///// <param name="model">The model identifier.</param>
        ///// <param name="imageUrl">The image URL.</param>
        ///// <returns>The image description returned by the model.</returns>
        //Task<string> AnalyzeImageBase64(string model, string imageUrl);
    }

}
