using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents a request for text generation or image analysis.
    /// </summary>
    public class ImageRequest
    {
        /// <summary>
        /// Gets or sets the model to use for processing.
        /// </summary>
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModelType Model { get; set; }

        /// <summary>
        /// Gets or sets the prompt for text generation or as additional context for image analysis (optional).
        /// </summary>
        public string? Prompt { get; set; }

        /// <summary>
        /// Gets or sets the image URL for image analysis (optional).
        /// </summary>
        public string? ImageUrl { get; set; }
    }
}
