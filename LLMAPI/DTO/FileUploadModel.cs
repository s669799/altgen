namespace LLMAPI.DTO
{
    /// <summary>
    /// Represents the model used for uploading a file.
    /// </summary>
    public class FileUploadModel
    {
        /// <summary>
        /// Gets or sets the file to be uploaded.
        /// </summary>
        public IFormFile? File { get; set; }
    }
}