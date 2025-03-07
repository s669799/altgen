using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Google.Protobuf;

namespace LLMAPI.Services.Interfaces
{
    public interface IGoogleService
    {
        Task<string> AnalyzeImageGoogleVision(IFormFile imageFile);
        Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes);
    }
}
