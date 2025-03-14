using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.Google;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LLMAPI.Services.Google
{
    [Obsolete("This service is deprecated and should not be used. It will be removed in a future update.")]
    public class GoogleTextGenerationService : ITextGenerationService
    {
        private readonly OpenRouterService _openRouterService;

        public GoogleTextGenerationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        public async Task<string> GenerateText(string model, string prompt)
        {
            // Call OpenRouterService to generate text using the Google model
            return await _openRouterService.GenerateText(model, prompt);
        }
    }
}
