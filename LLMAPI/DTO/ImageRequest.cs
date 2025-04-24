using System.ComponentModel;
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

        /// <summary>
        /// Gets or sets the predicted aircraft type from a preceding CNN analysis (optional).
        /// Populated from the `predicted_aircraft` field in the input JSON.
        /// </summary>
        [JsonPropertyName("predicted_aircraft")] // Maps from JSON key
        public string? PredictedAircraft { get; set; }

        /// <summary>
        /// Gets or sets the probability associated with the CNN prediction (optional).
        /// Populated from the `probability` field in the input JSON.
        /// </summary>
        [JsonPropertyName("probability")] // Maps from JSON key
        public double? Probability { get; set; }

        /// <summary>
        /// Gets or sets the temperature of the request [0.0, 2.0] (optional)
        /// </summary>
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)]
        public double? Temperature { get; set; } = 1.0;
    }
}
