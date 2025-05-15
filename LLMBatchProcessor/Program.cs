using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LLMAPI.Enums;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Google.Cloud.Storage.V1;
using LLMAPI.DTO;
using System.Text.Encodings.Web;

namespace ImageAnalysisConsole
{
    public class ConsoleLLMResponseOutput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModelType Model { get; set; }
        public string Prompt { get; set; }
        public string? PredictedAircraft { get; set; }
        public double? Probability { get; set; }
        public double Temperature { get; set; }
        public string Response { get; set; }
    }

    public class ConsoleImageAnalysisResultOutput
    {
        public string ImageUrl { get; set; }
        public List<ConsoleLLMResponseOutput> Results { get; set; } = new();
    }

    public sealed class CsvOutputRecord
    {
        public ModelType Model { get; set; }
        public string ImageUrl { get; set; }
        public string Prompt { get; set; }
        public string? PredictedAircraft { get; set; }
        public double? Probability { get; set; }
        public double Temperature { get; set; }
        public string Response { get; set; }
    }

    public class ApiResponseWithPrediction
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("predictedAircraft")]
        public string? PredictedAircraft { get; set; }

        [JsonPropertyName("probability")]
        public double? Probability { get; set; }
    }

    public class ApiErrorResponseDTO
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        // Assuming ApiErrorValidationDTO exists from previous exchanges if needed
    }


    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            string apiBaseUrl = config["ApiBaseUrl"];
            string cnnApiUrl = $"{apiBaseUrl}/api/cnn-llm";
            string openRouterApiKey = config["OpenRouter:APIKey"];

            Console.WriteLine($"ApiBaseUrl from config: {apiBaseUrl}");
            Console.WriteLine($"Full CNN API URL: {cnnApiUrl}");
            Console.WriteLine($"OpenRouter API Key (first 4 chars): {(string.IsNullOrEmpty(openRouterApiKey) ? "N/A" : openRouterApiKey.Substring(0, Math.Min(4, openRouterApiKey.Length)) + "...")}");

            string bucketName = "altgen_dam_bucket";
            List<string> folderPaths = new List<string>()
            {
                //"imagenet-sample-images/imagenet1",
                //"Cifar-10/sample10",
                "airplanes4",
                //"random-images/random1"
            };

            var promptCasesToTest = new List<(string Prompt, string? SystemMessage)>
            {
                //("Write an alt text for this image.", null),
                ("Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar. Here is two examples: 'Group of young college students laugh and walk along a tree-lined pathway.', 'Harvard’s Memorial Church with grand columns and hanging banners displaying Harvard shields.'", null)
            };

            List<ModelType> modelsToTest = new List<ModelType>()
            {
                ModelType.ChatGpt4_1,
                ModelType.ChatGpt4_1Nano,
                ModelType.ChatGpt4o,
                ModelType.Gemini2_0Flash,
                ModelType.Gemini2_0FlashLite,
                ModelType.Gemini2_5FlashPreview,
                ModelType.Gemma3_4B,
                ModelType.Claude3_7Sonnet,
                ModelType.Claude3_5Haiku,
                ModelType.Qwen2_5Vl72bInstruct,
                ModelType.Qwen2_5Vl7bInstruct,
                ModelType.MistralPixtralLarge,
                ModelType.MistralPixtral12b,
                ModelType.AmazonNovaLiteV1,
                ModelType.Grok2Vision1212,
                ModelType.Llama3_2_90bVisionInstruct,
                ModelType.Llama3_2_11bVisionInstruct,
                ModelType.MicrosoftPhi4MultimodalInstruct
            };

            string baseResultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..", "Results");
            baseResultsFolder = Path.GetFullPath(baseResultsFolder);

            if (!Directory.Exists(baseResultsFolder))
            {
                Directory.CreateDirectory(baseResultsFolder);
            }

            foreach (string folderPath in folderPaths)
            {
                Console.WriteLine("\n" + new string('-', 50));
                Console.WriteLine($"--- Processing Folder: {folderPath} ---");
                Console.WriteLine(new string('-', 50) + "\n");

                var imageUrls = await ListImageUrlsFromGcs(bucketName, folderPath);
                Console.WriteLine($"Found {imageUrls.Count} images in gs://{bucketName}/{folderPath}");

                var allResultsForFolder = new List<ConsoleImageAnalysisResultOutput>();
                var csvRecords = new List<CsvOutputRecord>();

                int completedImageCount = 0;
                int totalImages = imageUrls.Count * modelsToTest.Count * promptCasesToTest.Count;

                foreach (var imageUrl in imageUrls)
                {
                    Console.WriteLine($"\nProcessing image: {imageUrl}");
                    var imageResults = new ConsoleImageAnalysisResultOutput { ImageUrl = imageUrl };
                    allResultsForFolder.Add(imageResults);

                    foreach (var model in modelsToTest)
                    {
                        foreach (var promptCase in promptCasesToTest)
                        {
                            var (prompt, systemMessage) = promptCase;
                            Console.Write($"  - Model: {model}, Prompt: '{prompt.Substring(0, Math.Min(50, prompt.Length))}...' ");

                            string llmResponse = "Error: Processing Failed";
                            string? predictedAircraft = null;
                            double? probability = null;

                            try
                            {
                                var cnnRequest = new CNNRequest
                                {
                                    ImageUrl = imageUrl,
                                    Model = model,
                                    Prompt = prompt,
                                    Temperature = 1.0
                                };

                                var response = await ProcessImageRequestWithPrediction(cnnApiUrl + "/predict", cnnRequest);

                                if (response.Success)
                                {
                                    var detailedResponse = response.Result as ApiResponseWithPrediction;
                                    if (detailedResponse != null)
                                    {
                                        llmResponse = detailedResponse.Response ?? "LLM returned empty response.";
                                        predictedAircraft = detailedResponse.PredictedAircraft;
                                        probability = detailedResponse.Probability;
                                        Console.Write(" - CNN Success. ");
                                    }
                                    else
                                    {
                                        llmResponse = "Error: Unexpected success response format.";
                                        Console.Write(" - Error: Unexpected response format. ");
                                    }
                                }
                                else
                                {
                                    llmResponse = $"Error: API call failed with status {response.StatusCode}. Details: {response.Error?.ToString() ?? "No details provided."}";
                                    Console.Write($" - API Error: {response.StatusCode}. ");
                                }
                                Console.WriteLine();
                            }
                            catch (Exception ex)
                            {
                                llmResponse = $"Error: Unhandled exception during API call: {ex.Message}";
                                Console.WriteLine($" - Exception: {ex.Message}");
                            }

                            var individualResult = new ConsoleLLMResponseOutput
                            {
                                Model = model,
                                Prompt = prompt,
                                PredictedAircraft = predictedAircraft,
                                Probability = probability,
                                Temperature = 1.0,
                                Response = llmResponse
                            };

                            imageResults.Results.Add(individualResult);

                            csvRecords.Add(new CsvOutputRecord
                            {
                                Model = individualResult.Model,
                                ImageUrl = imageResults.ImageUrl,
                                Prompt = individualResult.Prompt,
                                PredictedAircraft = individualResult.PredictedAircraft,
                                Probability = individualResult.Probability,
                                Temperature = individualResult.Temperature,
                                Response = individualResult.Response.Replace("\"", "\"\"")
                            });

                            completedImageCount++;
                            // Console.WriteLine($"  Completed {completedImageCount}/{totalImages}");
                        }
                    }
                }

                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string folderSpecificFileName = folderPath.Replace("/", "_").Replace("\\", "_");

                string jsonOutputPath = Path.Combine(baseResultsFolder, $"{folderSpecificFileName}_results_{timestamp}_detailed.json");
                string csvOutputPath = Path.Combine(baseResultsFolder, $"{folderSpecificFileName}_results_{timestamp}_detailed.csv");

                SaveToJson(allResultsForFolder, jsonOutputPath);
                SaveToCsv(csvRecords, csvOutputPath);

                Console.WriteLine($"\nResults for folder '{folderPath}' saved to:");
                Console.WriteLine($"- {jsonOutputPath}");
                Console.WriteLine($"- {csvOutputPath}");
                Console.WriteLine(new string('-', 50) + "\n");
            }

            Console.WriteLine("Processing complete. Press any key to exit.");
            Console.ReadKey();
        }

        static async Task<(bool Success, int StatusCode, object? Result, object? Error)> ProcessImageRequestWithPrediction(string apiUrl, CNNRequest request)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };

            var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Console.Write("Calling API... ");
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.Write($"Status: {response.StatusCode}. ");

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var detailedResponse = JsonSerializer.Deserialize<ApiResponseWithPrediction>(responseBody);
                    return (true, (int)response.StatusCode, detailedResponse, null);
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"JSON Deserialization Error (Success Response): {jsonEx.Message}. Raw: {responseBody}");
                    return (false, (int)response.StatusCode, null, $"Failed to parse success response: {jsonEx.Message}");
                }
            }
            else
            {
                string errorDetails = responseBody;
                try
                {
                    using var errorDoc = JsonDocument.Parse(responseBody);
                    if (errorDoc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        errorDetails = errorDoc.RootElement.GetString() ?? responseBody;
                    }
                    else if (errorDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        errorDetails = errorElement.ToString();
                    }
                    else if (errorDoc.RootElement.TryGetProperty("detail", out var detailElement))
                    {
                        if (detailElement.ValueKind == JsonValueKind.String)
                        {
                            errorDetails = detailElement.GetString() ?? responseBody;
                        }
                        else
                        {
                            errorDetails = detailElement.ToString();
                        }
                    }
                }
                catch (JsonException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse error response body content: {ex.Message}");
                }


                return (false, (int)response.StatusCode, responseBody, errorDetails);
            }
        }


        static async Task<List<string>> ListImageUrlsFromGcs(string bucketName, string folderPath)
        {
            List<string> imageUrls = new List<string>();
            try
            {
                var storageClient = StorageClient.Create();
                if (!folderPath.EndsWith("/")) folderPath += "/";

                await foreach (var storageObject in storageClient.ListObjectsAsync(bucketName, folderPath))
                {
                    string lowerCaseName = storageObject.Name.ToLower();
                    if (lowerCaseName.EndsWith(".jpg") || lowerCaseName.EndsWith(".jpeg") ||
                        lowerCaseName.EndsWith(".png") || lowerCaseName.EndsWith(".gif"))
                    {
                        imageUrls.Add($"https://storage.googleapis.com/{bucketName}/{storageObject.Name}");
                    }
                }
            }
            catch (Google.GoogleApiException gcsEx)
            {
                Console.WriteLine($"Error listing images from GCS bucket '{bucketName}' folder '{folderPath}': {gcsEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred while listing images from GCS: {ex.Message}");
            }

            return imageUrls;
        }

        static void SaveToJson(List<ConsoleImageAnalysisResultOutput> results, string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString = JsonSerializer.Serialize(results, options);
            File.WriteAllText(filePath, jsonString);
        }

        static void SaveToCsv(List<CsvOutputRecord> records, string filePath)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
            };

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(records);
            }
        }
    }

}
