using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents a request for the general LLM endpoints (text or image analysis),
    /// allowing optional inclusion of pre-calculated CNN predictions.
    /// Used by LLMController.
    /// </summary>
    public class LLMRequest
    {
        /// <summary>
        /// Gets or sets the LLM model to use for processing. Defaults to ChatGpt4o.
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
        /// Must be provided if requesting image analysis.
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the predicted aircraft type from a preceding CNN analysis (optional).
        /// This field is intended to be provided BY THE CLIENT if they have already run the CNN.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("predicted_aircraft")]
        [DefaultValue(null)]
        public string? PredictedAircraft { get; set; } = null;

        /// <summary>
        /// Gets or sets the probability associated with the CNN prediction (optional).
        /// This field is intended to be provided BY THE CLIENT if they have already run the CNN.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("probability")]
        [DefaultValue(null)]
        public double? Probability { get; set; } = null;

        /// <summary>
        /// Gets or sets the temperature of the request [0.0, 2.0] (optional). Defaults to 1.0.
        /// </summary>
        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)]
        public double? Temperature { get; set; } = 1.0;
    }
}
