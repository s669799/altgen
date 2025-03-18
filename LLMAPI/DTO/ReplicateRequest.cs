using System.ComponentModel.DataAnnotations;

namespace LLMAPI.DTO
{
    public class ReplicateRequest
    {
        public string Image { get; set; }
        public string Prompt { get; set; }
    }
}
