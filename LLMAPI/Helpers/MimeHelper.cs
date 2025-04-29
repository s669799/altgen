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
                // Handle cases where no filename is provided gracefully
                return "application/octet-stream";
            }

            string mimeType = "application/octet-stream"; // Default
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            // Handle extensions without a leading dot if necessary, though GetExtension should provide it
            if (!string.IsNullOrEmpty(ext) && ext[0] != '.')
            {
                ext = "." + ext;
            }


            try
            {
                // On Windows, try to get the MIME type from the Registry
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use a try-catch around Registry access, as it might fail due to permissions or environment
                    try
                    {
                        // OpenClassesRoot provides extension mappings
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
                    catch (Exception ex) // Catch specific registry errors if needed, or just general catch
                    {
                        // Log this failure to use Registry, but don't let it stop the process
                        // Using a logger here would be better than Console.Error if implemented
                        Console.Error.WriteLine($"Error accessing Windows Registry for MIME type for extension {ext}: {ex.Message}");
                    }
                }
            }
            // Catch any exception during the entire process if the internal try-catch missed something (less likely with the inner try)
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An unexpected error occurred in MimeHelper for extension {ext}: {ex.Message}");

            }


            // Fallback to hardcoded list if Registry lookup failed or not on Windows
            switch (ext)
            {
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".pdf": return "application/pdf"; // Added PDF as common document type
                // Add more types as needed
                default: return "application/octet-stream"; // Default for unknown types
            }
        }
    }
}
