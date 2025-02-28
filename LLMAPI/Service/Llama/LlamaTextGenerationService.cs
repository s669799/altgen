using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.Llama;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LLMAPI.Services.Llama
{
    public class LlamaTextGenerationService : ITextGenerationService
    {
        private readonly OpenRouterService _openRouterService;

        public LlamaTextGenerationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        public async Task<string> GenerateText(string prompt)
        {
            // Call OpenRouterService to generate text using the Meta Llama model
            return await _openRouterService.GenerateText(prompt);
        }
    }
}
