using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.Google;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LLMAPI.Services.Google
{
    /// <summary>
    /// <para>
    /// **Deprecated:** This service is obsolete and should not be used. It will be removed in a future update.
    /// </para>
    /// Provides text generation capabilities using Google models but via delegation to <see cref="OpenRouterService"/>.
    /// This redirection is likely due to changes in preferred API access or service architecture.
    /// </summary>
    [Obsolete("This service is deprecated and should not be used. It will be removed in a future update.")]
    public class GoogleTextGenerationService : ITextGenerationService
    {
        private readonly OpenRouterService _openRouterService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleTextGenerationService"/>, injecting an <see cref="OpenRouterService"/> dependency.
        /// </summary>
        /// <param name="openRouterService">The <see cref="OpenRouterService"/> to delegate text generation requests to.</param>
        public GoogleTextGenerationService(OpenRouterService openRouterService)
        {
            _openRouterService = openRouterService;
        }

        /// <summary>
        /// Generates text using a specified model and prompt. 
        /// **Note:** This implementation is deprecated and delegates the actual text generation to the <see cref="OpenRouterService"/>.
        /// It's recommended to use <see cref="OpenRouterService"/> directly or migrate to a non-deprecated service.
        /// </summary>
        /// <param name="model">The model identifier (e.g., "google/gemini-flash-1.5-8b"). Despite the parameter name, the actual model handling might be determined by <see cref="OpenRouterService"/> configuration.</param>
        /// <param name="prompt">The text prompt.</param>
        /// <returns>The generated text, obtained via delegation to <see cref="OpenRouterService"/>.</returns>
        public async Task<string> GenerateText(string model, string prompt)
        {
            return await _openRouterService.GenerateText(model, prompt);
        }
    }
}
