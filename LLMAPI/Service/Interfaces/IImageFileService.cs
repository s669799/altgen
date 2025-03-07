using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;


namespace LLMAPI.Service.Interfaces
{
    public interface IImageFileService
    {
        /// <summary>
        /// Converts an uploaded image (IFormFile) to a ByteString.
        /// </summary>
        /// <param name="imageFile">The uploaded image file.</param>
        /// <returns>The image content as a ByteString.</returns>
        Task<ByteString> ConvertImageToByteString(IFormFile imageFile);

        /// <summary>
        /// Reads an image from a URL and converts it to a ByteString.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <returns>The image content as a ByteString.</returns>
        Task<ByteString> ReadImageFileAsync(string url);
    }
}
