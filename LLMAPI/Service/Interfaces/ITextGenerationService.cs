using System.Threading.Tasks;

namespace LLMAPI.Services.Interfaces
{
    public interface ITextGenerationService
    {
        /// <summary>
        /// Generates text using a specified model and prompt.
        /// </summary>
        /// <param name="model">The model identifier (e.g., "openai/gpt-4o-mini").</param>
        /// <param name="prompt">The text prompt.</param>
        /// <returns>The generated text.</returns>
        Task<string> GenerateText(string model, string prompt);
    }
}
