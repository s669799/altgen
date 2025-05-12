using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents a request from the client specifically initiating the CNN-enhanced
    /// image analysis workflow. Used by CNNController.
    /// </summary>
    public class CNNRequest
    {
        /// <summary>
        /// Gets or sets the LLM model to use for the final analysis. Defaults to ChatGpt4o.
        /// </summary>
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModelType Model { get; set; }

        /// <summary>
        /// Gets or sets the optional text prompt from the user
        /// to guide the final LLM analysis. This is combined
        /// with a default prompt and the CNN prediction results internally.
        /// </summary>
        public string? Prompt { get; set; }

        /// <summary>
        /// Gets or sets the REQUIRED image URL for analysis.
        /// The image from this URL will be sent to the CNN first.
        /// </summary>
        [Required] // Image URL is mandatory to start this workflow
        public string? ImageUrl { get; set; }


        /// <summary>
        /// Gets or sets the temperature setting [0.0, 2.0] for the LLM (optional). Defaults to 1.0.
        /// </summary>
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)]
        public double? Temperature { get; set; } = 1.0;
    }
}
