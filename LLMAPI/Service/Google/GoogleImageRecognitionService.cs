using global::Google.Cloud.Vision.V1;
using global::Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Api.Gax.Grpc.Rest;
using Google.Api.Gax.Grpc;
using LLMAPI.Services.Interfaces;
using LLMAPI.Service.Interfaces;

namespace LLMAPI.Services.Google
{
    /// <summary>
    /// Service to handle image recognition tasks using Google Cloud Vision API.
    /// Implements interfaces for both Google-specific services and image file handling.
    /// </summary>
    public class GoogleImageRecognitionService : IGoogleService, IImageFileService
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleImageRecognitionService"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration provider.</param>
        public GoogleImageRecognitionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Analyzes an image file uploaded via HTTP using Google Vision API Label Detection.
        /// </summary>
        /// <param name="imageFile">The image file uploaded as <see cref="IFormFile"/>.</param>
        /// <returns>A string containing labels detected by Google Vision API, along with their confidence scores. Returns "No labels detected." if no labels are found.</returns>
        public async Task<string> AnalyzeImageGoogleVision(IFormFile imageFile)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"keys/rich-world-450914-e6-b6ee1b4424e9.json");

            using var stream = imageFile.OpenReadStream();
            var image = global::Google.Cloud.Vision.V1.Image.FromStream(stream);

            var client = new ImageAnnotatorClientBuilder
            {
                GrpcAdapter = global::Google.Api.Gax.Grpc.Rest.RestGrpcAdapter.Default
            }.Build();


            var labels = await client.DetectLabelsAsync(image);

            if (labels == null || labels.Count() == 0)
                return "No labels detected.";

            var altText = new StringBuilder();
            foreach (var label in labels)
            {
                altText.AppendLine($"{label.Description} (Confidence: {label.Score:F2})");
            }

            return altText.ToString();
        }

        /// <summary>
        /// Analyzes an image from a given URL using Google Vision API Label Detection.
        /// </summary>
        /// <param name="imageUrl">The URL of the image to be analyzed.</param>
        /// <returns>A string containing labels detected by Google Vision API, along with their confidence scores. Returns "No labels detected." if no labels are found, or an error message if processing fails.</returns>
        public async Task<string> AnalyzeImageGoogleVision(string imageUrl)
        {
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"keys/rich-world-450914-e6-b6ee1b4424e9.json");

                ByteString imageBytes = await ReadImageFileAsync(imageUrl);
                var image = global::Google.Cloud.Vision.V1.Image.FromBytes(imageBytes.ToByteArray());

                var client = new ImageAnnotatorClientBuilder
                {
                    GrpcAdapter = global::Google.Api.Gax.Grpc.Rest.RestGrpcAdapter.Default
                }.Build();

                var labels = await client.DetectLabelsAsync(image);

                if (labels == null || labels.Count() == 0)
                    return "No labels detected.";

                var altText = new StringBuilder();
                foreach (var label in labels)
                {
                    altText.AppendLine($"{label.Description} (Confidence: {label.Score:F2})");
                }

                return altText.ToString();
            }
            catch (Exception ex)
            {
                return $"Error processing image: {ex.Message}";
            }
        }


        //        public async Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes)
        //        {
        //            var predictionServiceClient = new PredictionServiceClientBuilder
        //            {
        //                Endpoint = $"{location}-aiplatform.googleapis.com"
        //            }.Build();

        ///*             // Fetch the image from the URL
        //            ByteString imageByteData = await ReadImageFileAsync(imageUrl); */

        //            var generateContentRequest = new GenerateContentRequest
        //            {
        //                Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
        //                Contents =
        //                {
        //                    new Content
        //                    {
        //                        Role = "USER",
        //                        Parts =
        //                        {
        //                            new Part { Text = "Write a brief, one to two sentence alt text description for this image that captures the main subjects, action, and setting." },
        //                            new Part { InlineData = new() { MimeType = "image/png", Data = imageBytes } }
        //                        }
        //                    }
        //                }
        //            };

        //            using PredictionServiceClient.StreamGenerateContentStream response = predictionServiceClient.StreamGenerateContent(generateContentRequest);

        //            StringBuilder fullText = new();

        //            // Explicitly getting the response stream
        //            AsyncResponseStream<GenerateContentResponse> responseStream = response.GetResponseStream();
        //            await foreach (GenerateContentResponse responseItem in responseStream)
        //            {
        //                fullText.Append(responseItem.Candidates[0].Content.Parts[0].Text);
        //            }

        //            return fullText.ToString();
        //        }

        /// <summary>
        /// Converts an <see cref="IFormFile"/> image to a <see cref="ByteString"/>.
        /// </summary>
        /// <param name="imageFile">The image file uploaded via HTTP.</param>
        /// <returns>The image content as a ByteString.</returns>
        public async Task<ByteString> ConvertImageToByteString(IFormFile imageFile)
        {
            using var memoryStream = new MemoryStream();
            await imageFile.CopyToAsync(memoryStream);
            return ByteString.CopyFrom(memoryStream.ToArray());
        }

        /// <summary>
        /// Reads an image from a URL and converts it to a <see cref="ByteString"/>.
        /// </summary>
        /// <param name="url">The URL of the image.</param>
        /// <returns>The image content as a ByteString.</returns>
        public async Task<ByteString> ReadImageFileAsync(string url)
        {
            using HttpClient client = new();
            using var response = await client.GetAsync(url);
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            return ByteString.CopyFrom(imageBytes);
        }
    }
}
