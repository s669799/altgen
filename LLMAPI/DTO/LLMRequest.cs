using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LLMAPI.DTO
{
    public class LLMFormRequest
    {
        [Required]
        public IFormFile Image { get; set; }

        [Required]
        public string Model { get; set; }

        public string? Prompt { get; set; }

        public double? Temperature { get; set; }
    }
}

