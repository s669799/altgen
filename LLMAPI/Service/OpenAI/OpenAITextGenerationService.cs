using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.OpenAI;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LLMAPI.Services.OpenAI
{
    public class OpenAITextGenerationService : ITextGenerationService
    {
        private readonly OpenRouterService _openRouterService;

        // Constructor receives an instance of OpenRouterService
        public OpenAITextGenerationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        /// <summary>
        /// Generates text using the OpenAI model by delegating to the OpenRouter service.
        /// </summary>
        /// <param name="prompt">The prompt to send to the OpenAI model.</param>
        /// <returns>The AI's response as a string.</returns>
        public async Task<string> GenerateText(string model, string prompt)
        {
            // Call the OpenRouterService's GenerateText method with the OpenAI model identifier
            return await _openRouterService.GenerateText(model, prompt);
        }
    }
}
