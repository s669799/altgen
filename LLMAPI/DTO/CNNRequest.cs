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
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ModelType Model { get; set; }

    public string? Prompt { get; set; }

    [Range(0.0, 2.0)]
    [DefaultValue(1.0)]
    public double? Temperature { get; set; } = 1.0;

    [Required]
    public IFormFile Image { get; set; } = null!;
}


}
