using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Service.Interfaces
{
    /// <summary>
    /// Interface defining the contract for image file services, handling image file conversions and reading.
    /// </summary>
    public interface IImageFileService
    {
        /// <summary>
        /// Converts an uploaded image (<see cref="IFormFile"/>) to a <see cref="ByteString"/>.
        /// </summary>
        /// <param name="imageFile">The uploaded image file.</param>
        /// <returns>The image content as a <see cref="ByteString"/>.</returns>
        Task<ByteString> ConvertImageToByteString(IFormFile imageFile);

        /// <summary>
        /// Reads an image from a URL and converts it to a <see cref="ByteString"/>.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <returns>The image content as a <see cref="ByteString"/>.</returns>
        Task<ByteString> ReadImageFileAsync(string url);
    }
}
