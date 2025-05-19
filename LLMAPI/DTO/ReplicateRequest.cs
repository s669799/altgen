using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace LLMAPI.DTO
{
    public class ReplicateRequest
    {
        [Required(ErrorMessage = "Image file is required.")]
        public IFormFile Image { get; set; }

        public string? Prompt { get; set; }

        [Range(0.0, 2.0, ErrorMessage = "Temperature must be between 0.0 and 2.0.")]
        [DefaultValue(1.0)]
        public double? Temperature { get; set; } = 1.0;

        [JsonPropertyName("enable_cognitive_layer")]
        [DefaultValue(true)]
        public bool? EnableCognitiveLayer { get; set; } = true;

        [Required(ErrorMessage = "Model identifier is required.")]
        public string Model { get; set; }
    }
}

