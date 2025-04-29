// LLMAPI.DTO/CnnPredictResponse.cs
using System.Text.Json.Serialization;

namespace LLMAPI.DTO
{
    // Represents the expected JSON response structure from your FastAPI CNN prediction endpoint.
    public class CNNResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("predicted_aircraft")]
        public string? PredictedAircraft { get; set; }

        [JsonPropertyName("probability")]
        public double? Probability { get; set; }

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        // Optional: Add an error message property if your FastAPI returns it even on success: false
        [JsonPropertyName("detail")] // Assuming 'detail' is used for error messages in FastAPI HTTP 500
        public string? Detail { get; set; }
    }
}
