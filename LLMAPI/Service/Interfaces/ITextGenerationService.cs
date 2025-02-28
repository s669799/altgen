using System.Threading.Tasks;

namespace LLMAPI.Services.Interfaces
{
    public interface ITextGenerationService
    {
        Task<string> GenerateText(string prompt);
    }
}
