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

namespace LLMAPI.Services.Google
{
    public class GoogleImageRecognitionService : IImageRecognitionService
    {
        private readonly IConfiguration _configuration;

        public GoogleImageRecognitionService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> AnalyzeImage(string model, string imgUrl)
        {
            return null;
        }
        
        public async Task<string> AnalyzeImageBase64(string model, string imgUrl)
        {
            return null;
        }

        public async Task<string> AnalyzeImage(IFormFile imageFile)
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

        public async Task<string> GenerateContent(string projectId, string location, string publisher, string model, ByteString imageBytes)
        {
            var predictionServiceClient = new PredictionServiceClientBuilder
            {
                Endpoint = $"{location}-aiplatform.googleapis.com"
            }.Build();

/*             // Fetch the image from the URL
            ByteString imageByteData = await ReadImageFileAsync(imageUrl); */

            var generateContentRequest = new GenerateContentRequest
            {
                Model = $"projects/{projectId}/locations/{location}/publishers/{publisher}/models/{model}",
                Contents =
                {
                    new Content
                    {
                        Role = "USER",
                        Parts =
                        {
                            new Part { Text = "Write a brief, one to two sentence alt text description for this image that captures the main subjects, action, and setting." },
                            new Part { InlineData = new() { MimeType = "image/png", Data = imageBytes } }
                        }
                    }
                }
            };

            using PredictionServiceClient.StreamGenerateContentStream response = predictionServiceClient.StreamGenerateContent(generateContentRequest);

            StringBuilder fullText = new();

            // Explicitly getting the response stream
            AsyncResponseStream<GenerateContentResponse> responseStream = response.GetResponseStream();
            await foreach (GenerateContentResponse responseItem in responseStream)
            {
                fullText.Append(responseItem.Candidates[0].Content.Parts[0].Text);
            }

            return fullText.ToString();
        }

        public async Task<ByteString> ReadImageFileAsync(string url)
        {
            using HttpClient client = new();
            using var response = await client.GetAsync(url);
            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
            return ByteString.CopyFrom(imageBytes);
        }

    }
}
