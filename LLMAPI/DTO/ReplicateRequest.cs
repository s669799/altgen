using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LLMAPI.DTO
{
    public class ReplicateRequest
    {
        public string Image { get; set; }
        public string Prompt { get; set; }

        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)] // Replicate's default seems to be around 0.1 for this model
        public double? Temperature { get; set; } = 1.0; // Set default value

        // New property to control CNN layer
        [JsonPropertyName("enable_cognitive_layer")] // Use snake_case for consistency
        [DefaultValue(true)] // Default to true (CNN is on by default)
        public bool? EnableCognitiveLayer { get; set; } = true; // Set default value
    }
}
