using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LLMAPI.Services.DeepSeek
{
    public class DeepSeekTextGenerationService : ITextGenerationService
    {
        private readonly OpenRouterService _openRouterService;

        public DeepSeekTextGenerationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        public async Task<string> GenerateText(string prompt)
        {
            // Call OpenRouterService to generate text using the DeepSeek model
            return await _openRouterService.GenerateText(prompt);
        }
    }
}
