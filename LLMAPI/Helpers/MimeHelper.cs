// LLMAPI.Helpers/MimeHelper.cs
using System.IO; // Required for Path.GetExtension
using System.Runtime.InteropServices; // Required for RuntimeInformation
using Microsoft.Win32; // Required for Registry key lookup (Windows only)
using System; // Required for Exception, UriKind

namespace LLMAPI.Helpers // Use a suitable namespace for helpers
{
    /// <summary>
    /// Helper class to determine MIME types from file extensions.
    /// </summary>
    public static class MimeHelper // Make it static as there's no state
    {
        /// <summary>
        /// Gets the MIME type for a given file name based on its extension.
        /// </summary>
        /// <param name="fileName">The file name (e.g., "document.pdf", "image.jpg").</param>
        /// <returns>The corresponding MIME type string, or "application/octet-stream" if unknown.</returns>
        public static string GetMimeType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "application/octet-stream";
            }

            string mimeType = "application/octet-stream";
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            if (!string.IsNullOrEmpty(ext) && ext[0] != '.')
            {
                ext = "." + ext;
            }


            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    try
                    {
                        using (RegistryKey? regKey = Registry.ClassesRoot.OpenSubKey(ext))
                        {
                            if (regKey != null && regKey.GetValue("Content Type") != null)
                            {
                                string? contentType = regKey.GetValue("Content Type")!.ToString();
                                if (!string.IsNullOrEmpty(contentType))
                                {
                                    return contentType;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error accessing Windows Registry for MIME type for extension {ext}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred in MimeHelper for extension {ext}: {ex.Message}");

            }


            switch (ext)
            {
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".pdf": return "application/pdf";
                default: return "application/octet-stream";
            }
        }
    }
}
