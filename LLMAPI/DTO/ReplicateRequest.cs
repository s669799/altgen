using System.ComponentModel.DataAnnotations;

namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents a request to run a model on Replicate.
    /// </summary>
    public class ReplicateRequest
    {
        /// <summary>
        /// Gets or sets the URL of the image to be processed by the Replicate model. This is a required input for image-based models.
        /// </summary>
        public string Image { get; set; }
        /// <summary>
        /// Gets or sets an optional text prompt to guide the Replicate model. This can be used to provide additional context or instructions.
        /// </summary>
        public string Prompt { get; set; }
    }
}
