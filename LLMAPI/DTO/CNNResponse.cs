// LLMAPI.DTO/CnnPredictResponse.cs
using System.Text.Json.Serialization;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents the expected JSON response structure from your FastAPI CNN prediction endpoint.
    /// </summary>
    public class CNNResponse
    {
        /// <summary>
        /// Indicates if the CNN prediction was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// The predicted type of aircraft detected in the image.
        /// </summary>
        [JsonPropertyName("predicted_aircraft")]
        public string? PredictedAircraft { get; set; }

        /// <summary>
        /// The probability score associated with the predicted aircraft type.
        /// </summary>
        [JsonPropertyName("probability")]
        public double? Probability { get; set; }

        /// <summary>
        /// The original filename of the image that was processed by the CNN.
        /// </summary>
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        /// <summary>
        /// Optional: An error message detail provided by the CNN service in case of failure.
        /// </summary>
        [JsonPropertyName("detail")] // Assuming 'detail' is used for error messages in FastAPI HTTP 500
        public string? Detail { get; set; }
    }
}
