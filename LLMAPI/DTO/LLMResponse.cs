using LLMAPI.DTO;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents the result of processing a prompt for an image analysis task.
    /// </summary>
    public class LLMResponse
    {
        /// <summary>
        /// Gets or sets the prompt text that was used.
        /// </summary>
        public string Prompt { get; set; }
        /// <summary>
        /// Gets or sets the LLM's response to the prompt.
        /// </summary>
        public string Response { get; set; }
    }

    /// <summary>
    /// Represents the results of image analysis for a single image URL.
    /// </summary>
    public class ImageAnalysisResult
    {
        /// <summary>
        /// Gets or sets the URL of the image that was analyzed.
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Gets or sets a list of <see cref="LLMResponse"/> objects, each representing the response to a different prompt for this image.
        /// </summary>
        public List<LLMResponse> Results { get; set; } = new();
    }

    /// <summary>
    /// Represents a record for CSV output, containing the model, image URL, prompt, and response.
    /// </summary>
    public class CsvOutputRecord
    {
        /// <summary>
        /// Gets or sets the LLM model used to generate the response.
        /// </summary>
        public ModelType Model { get; set; }
        /// <summary>
        /// Gets or sets the URL of the image that was analyzed.
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Gets or sets the prompt that was used to generate the response.
        /// </summary>
        public string Prompt { get; set; }
        /// <summary>
        /// Gets or sets the LLM's response to the prompt.
        /// </summary>
        public string Response { get; set; }
    }
}
