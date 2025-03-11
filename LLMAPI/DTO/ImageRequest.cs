using System.ComponentModel.DataAnnotations;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    public class ImageRequest
    {
        [Required]
        public ModelType Model { get; set; }
        [Url]
        [Required]
        public string ImageUrl { get; set; }
        public string TextPrompt { get; set; }
    }
}