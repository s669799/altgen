using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents the request body for initiating a Replicate model run.
    /// </summary>
    public class ReplicateRequest
    {
        /// <summary>
        /// The URL of the image to be processed by the Replicate vision model.
        /// </summary>
        [Required(ErrorMessage = "Image URL is required.")]
        public string Image { get; set; }

        /// <summary>
        /// The text prompt to guide the Replicate model's output.
        /// </summary>
        public string? Prompt { get; set; }

        /// <summary>
        /// The temperature sampling value for the model's generation. Range is [0.0, 2.0].
        /// </summary>
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)]
        public double? Temperature { get; set; } = 1.0;

        /// <summary>
        /// Enables or disables the pre-processing CNN cognitive layer to extract aircraft data before sending to Replicate. Defaults to true.
        /// </summary>
        [JsonPropertyName("enable_cognitive_layer")]
        [DefaultValue(true)]
        public bool? EnableCognitiveLayer { get; set; } = true;
    }
}
