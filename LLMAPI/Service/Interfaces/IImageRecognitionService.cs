using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Services.Interfaces
{
    public interface IImageRecognitionService
    {
        Task<string> AnalyzeImage(IFormFile imageUrl);

        Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes);

        Task<ByteString> ReadImageFileAsync(string url); 
    }
}
