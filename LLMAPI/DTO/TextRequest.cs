using System.ComponentModel.DataAnnotations;
using LLMAPI.Enums;

namespace LLMAPI.DTO
{
    public class TextRequest
    {
        [Required]
        public ModelType Model { get; set; }
        [Required]
        public string Prompt { get; set; }
    }
}