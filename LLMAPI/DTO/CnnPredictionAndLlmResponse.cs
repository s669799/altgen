using System.Text.Json.Serialization;

namespace LLMAPI.DTO
{
    public class CnnPredictionAndLlmResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("predictedAircraft")]
        public string? PredictedAircraft { get; set; }

        [JsonPropertyName("probability")]
        public double? Probability { get; set; }
    }
}
