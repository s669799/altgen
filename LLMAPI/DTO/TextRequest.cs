using System.ComponentModel.DataAnnotations;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents a request for text generation.
    /// </summary>
    public class TextRequest
    {
        /// <summary>
        /// Gets or sets the LLM model to be used for text generation. This is a required field.
        /// </summary>
        [Required]
        public ModelType Model { get; set; }
        /// <summary>
        /// Gets or sets the prompt text for text generation. This is a required field.
        /// </summary>
        [Required]
        public string Prompt { get; set; }
    }
}
