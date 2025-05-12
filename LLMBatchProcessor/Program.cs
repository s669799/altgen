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
using LLMAPI.DTO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Google.Cloud.Storage.V1;
using System.Text.Encodings.Web;

namespace ImageAnalysisConsole
{
    /// <summary>
    /// Console application to perform image analysis using a configured API, testing various Large Language Models (LLMs)
    /// against images fetched from a Google Cloud Storage bucket. Results are saved in CSV and JSON format, with support for processing multiple folders and subfolders, and improved console output readability.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Represents a single model/prompt response for the console application's output JSON format.
        /// Includes the ModelType directly, unlike the API's LLMResponse.
        /// </summary>
        public class ConsoleLLMResponseOutput
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public ModelType Model { get; set; }
            public string Prompt { get; set; }
            public string Response { get; set; }
        }

        /// <summary>
        /// Represents the consolidated analysis results for a single image
        /// in the console application's output JSON format.
        /// Contains a list of ConsoleLLMResponseOutput objects.
        /// </summary>
        public class ConsoleImageAnalysisResultOutput
        {
            public string ImageUrl { get; set; }
            public List<ConsoleLLMResponseOutput> Results { get; set; } = new();
        }

        /// <summary>
        /// Represents a record for CSV output, containing the model, image URL, prompt, and response.
        /// This is kept separate from the API DTOs for clarity in output formatting.
        /// </summary>
        public sealed class CsvOutputRecord
        {
            public ModelType Model { get; set; }
            public string ImageUrl { get; set; }
            public string Prompt { get; set; }
            public string Response { get; set; }
        }



        /// <summary>
        /// Main entry point of the console application. Configures the application, fetches images from specified folders and their subfolders in a bucket,
        /// tests specified LLMs against these images, and saves the analysis results in separate folders for each processed folder path.
        /// </summary>
        /// <param name="args">Command line arguments (not used in this application).</param>
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            string apiBaseUrl = config["ApiBaseUrl"];
            string apiUrl = $"{apiBaseUrl}/api/llm";
            string openRouterApiKey = config["OpenRouter:APIKey"];

            Console.WriteLine($"ApiBaseUrl from config: {apiBaseUrl}");
            Console.WriteLine($"Full API URL: {apiUrl}");
            Console.WriteLine($"OpenRouter API Key (first 4 chars): {(string.IsNullOrEmpty(openRouterApiKey) ? "N/A" : openRouterApiKey.Substring(0, Math.Min(4, openRouterApiKey.Length)) + "...")}");

            string bucketName = "altgen_dam_bucket";
            List<string> folderPaths = new List<string>()
            {
                "imagenet-sample-images/imagenet1",
                //"Cifar-10/sample10",
                "airplanesInternet",
                "random-images/random1"
            };

            List<string> prompts = new List<string>()
            {
                "Write an alt text for this image. Here is two examples: 'Group of young college students laugh and walk along a tree-lined pathway.', 'Harvard’s Memorial Church with grand columns and hanging banners displaying Harvard shields.'",
                "Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar."
            };

            List<ModelType> modelsToTest = new List<ModelType>()
            {
                ModelType.ChatGpt4_1,
                ModelType.ChatGpt4_1Nano,
                ModelType.ChatGpt4o,
                ModelType.Gemini2_0Flash,
                ModelType.Gemini2_0FlashLite,
                ModelType.Gemini2_5FlashPreview,
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
                Console.WriteLine(new string('-', 50));

                List<string> imageUrls = await GetImageUrlsFromBucket(bucketName, folderPath);

                if (imageUrls.Count == 0)
                {
                    Console.WriteLine($"No images found in bucket '{bucketName}/{folderPath}' or an error occurred. Please check the bucket name and permissions.");
                    continue;
                }

                Console.WriteLine($"Found {imageUrls.Count} images in bucket '{bucketName}/{folderPath}'.");

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderPathForName = string.IsNullOrEmpty(folderPath) ? "bucket_root" : folderPath.Replace('/', '_');
                string timestampFolderName = $"{timestamp}_{bucketName}_{folderPathForName}";
                string resultsFolder = Path.Combine(baseResultsFolder, timestampFolderName);
                Directory.CreateDirectory(resultsFolder);

                Console.WriteLine($"\n--- Run Information ---");
                Console.WriteLine($"  Results Folder: {resultsFolder}");
                Console.WriteLine($"  Bucket Name:    {bucketName}");
                Console.WriteLine($"  Bucket Folder:  {folderPath ?? "(Bucket Root)"}");
                Console.WriteLine($"  Number of Prompts: {prompts.Count}");
                Console.WriteLine($"  Number of Models: {modelsToTest.Count}");

                List<CsvOutputRecord> allCsvRecords = new();
                List<ConsoleImageAnalysisResultOutput> allJsonResults = new();


                string csvPath = Path.Combine(resultsFolder, $"image_analysis_results_consolidated.csv");
                string jsonPath = Path.Combine(resultsFolder, $"image_analysis_results_consolidated.json");

                var configCsv = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    TrimOptions = TrimOptions.Trim,
                    AllowComments = true,
                    HasHeaderRecord = true,
                    Delimiter = ","
                };

                foreach (string imageUrl in imageUrls)
                {
                    Console.WriteLine($"\nProcessing Image: {imageUrl}");
                    ConsoleImageAnalysisResultOutput imageResultForJson = new() { ImageUrl = imageUrl, Results = new List<ConsoleLLMResponseOutput>() };


                    foreach (string prompt in prompts)
                    {
                        Console.WriteLine($"  Testing Prompts for Image: {imageUrl}");
                        foreach (var selectedModel in modelsToTest)
                        {
                            try
                            {
                                Console.WriteLine($"    Testing Model: {selectedModel} with Prompt: {prompt.Substring(0, Math.Min(prompt.Length, 50))}...");
                                string response = await ProcessImage(apiUrl, openRouterApiKey, selectedModel, prompt, imageUrl);

                                imageResultForJson.Results.Add(new ConsoleLLMResponseOutput
                                {
                                    Model = selectedModel,
                                    Prompt = prompt,
                                    Response = response
                                });

                                allCsvRecords.Add(new CsvOutputRecord
                                {
                                    Model = selectedModel,
                                    ImageUrl = imageUrl,
                                    Prompt = prompt,
                                    Response = response
                                });

                                Console.WriteLine($"      Success. Response Length: {response.Length}. First 100 chars: {response.Substring(0, Math.Min(response.Length, 100))}...");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"      Error processing Model: {selectedModel}, Image: {imageUrl}, Prompt: {prompt.Substring(0, Math.Min(prompt.Length, 50))}... Error: {ex.Message}");

                                imageResultForJson.Results.Add(new ConsoleLLMResponseOutput
                                {
                                    Model = selectedModel,
                                    Prompt = prompt,
                                    Response = $"Error: {ex.Message}"
                                });

                                allCsvRecords.Add(new CsvOutputRecord
                                {
                                    Model = selectedModel,
                                    ImageUrl = imageUrl,
                                    Prompt = prompt,
                                    Response = $"Error: {ex.Message}"
                                });
                            }
                        }
                        Console.WriteLine($"  Finished testing all models for Prompt: {prompt.Substring(0, Math.Min(prompt.Length, 50))}...");
                    }
                    allJsonResults.Add(imageResultForJson);

                }

                using (var writer = new StreamWriter(csvPath))
                using (var csv = new CsvWriter(writer, configCsv))
                {
                    csv.WriteHeader<CsvOutputRecord>();
                    csv.NextRecord();
                    csv.WriteRecords(allCsvRecords);
                }

                var systemTextJsonSerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() },
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                File.WriteAllText(jsonPath, JsonSerializer.Serialize(allJsonResults, systemTextJsonSerializerOptions));

                Console.WriteLine($"\nConsolidated results for folder '{folderPath}' written as CSV to {csvPath}");
                Console.WriteLine($"Consolidated results for folder '{folderPath}' written as JSON to {jsonPath}");
            }

            Console.WriteLine("\nAll folder processing completed. Consolidated results saved in individual folder-stamped directories within the Results folder.");
        }

        /// <summary>
        /// Sends a request to the API endpoint to process an image for alt text generation using specified parameters.
        /// </summary>
        /// <param name="apiUrl">The base URL of the API, configured in appsettings.json.</param>
        /// <param name="apiKey">The API key for accessing the LLM service, configured in appsettings.json.</param>
        /// <param name="model">The <see cref="ModelType"/> enum value, specifying which LLM model to use for analysis.</param>
        /// <param name="prompt">The prompt text to guide the LLM in generating the alt text.</param>
        /// <param name="imageUrl">The public URL of the image to be analyzed.</param>
        /// <param name="temperature">Optional temperature setting for the LLM (defaults to 1.0 in API if null).</param>
        /// <returns>A string containing the generated alt text response from the API, or an error message on failure.</returns>
        static async Task<string> ProcessImage(string apiUrl, string apiKey, ModelType model, string prompt, string imageUrl, double? temperature = null)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "ImageAnalysisConsoleApp");
                client.DefaultRequestHeaders.Add("X-Title", "ImageAnalysisConsole");

                var requestData = new
                {
                    Model = model,
                    Prompt = prompt,
                    ImageUrl = imageUrl,
                    Temperature = temperature
                };

                var requestSerializerOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(requestData, requestSerializerOptions);


                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                string fullUrl = $"{apiUrl}/process-request-body";

                HttpResponseMessage response = await client.PostAsync(fullUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var apiResponse = JsonSerializer.Deserialize<ApiResponseDTO>(responseContent);
                        if (apiResponse != null && apiResponse.Response != null)
                        {
                            return apiResponse.Response;
                        }
                        return $"API Success but unexpected response format: {response.Content.Headers.ContentType?.MediaType} - {responseContent}";
                    }
                    catch (JsonException ex)
                    {
                        return $"Error parsing API success response JSON: {ex.Message}. Raw Response: {responseContent}";
                    }
                    catch (Exception ex)
                    {
                        return $"Unexpected error processing API success response: {ex.Message}. Raw Response: {responseContent}";
                    }
                }
                else
                {
                    string errorDetails = responseContent;
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<ApiErrorResponseDTO>(responseContent);
                        if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
                        {
                            errorDetails = errorResponse.Error;
                        }
                        else if (errorResponse != null && errorResponse.Errors?.Any() == true)
                        {
                            errorDetails = string.Join("; ", errorResponse.Errors.Select(e => e.ErrorMessage));
                        }
                    }
                    catch (JsonException)
                    {
                    }
                    catch (Exception)
                    {
                    }
                    return $"Error from API: {response.StatusCode} - {errorDetails}";
                }
            }
        }

        private class ApiResponseDTO
        {
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }

        private class ApiErrorResponseDTO
        {
            [JsonPropertyName("error")]
            public string? Error { get; set; }
            [JsonPropertyName("errors")]
            public List<ApiErrorValidationDTO>? Errors { get; set; }
        }


        private class ApiErrorValidationDTO
        {
            [JsonPropertyName("propertyName")]
            public string? PropertyName { get; set; }
            [JsonPropertyName("errorMessage")]
            public string? ErrorMessage { get; set; }
        }


        /// <summary>
        /// Retrieves a list of image URLs from a Google Cloud Storage bucket, including images from **within the specified folder and all its subfolders**.
        /// Returns the count of images found in the logs.
        /// </summary>
        /// <param name="bucketName">The name of the Google Cloud Storage bucket to access.</param>
        /// <param name="folderPath">Optional. If specified, images from **this folder and all subfolders** are listed. If null or empty, images from the bucket root are listed.</param>
        /// <returns>A List of strings, each being a publicly accessible URL for an image in the specified GCS bucket and folder (or subfolder). Returns an empty list if no images are found or an error occurs.</returns>
        static async Task<List<string>> GetImageUrlsFromBucket(string bucketName, string folderPath = null)
        {
            var storageClient = await StorageClient.CreateAsync();
            var imageUrls = new List<string>();
            int imageCount = 0;

            try
            {
                string prefix = folderPath;
                if (!string.IsNullOrEmpty(prefix))
                {
                    if (!prefix.EndsWith("/"))
                    {
                        prefix += "/";
                    }
                }

                var objectList = string.IsNullOrEmpty(prefix)
                    ? storageClient.ListObjectsAsync(bucketName)
                    : storageClient.ListObjectsAsync(bucketName, prefix: prefix);


                await foreach (var storageObject in objectList)
                {
                    if (storageObject.ContentType != null && storageObject.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    {
                        string imageUrl = $"https://storage.googleapis.com/{bucketName}/{storageObject.Name}";
                        imageUrls.Add(imageUrl);
                        imageCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing objects in bucket {bucketName} (folder: {folderPath}): {ex.Message}");
                return new List<string>();
            }

            Console.WriteLine($"    Found {imageCount} images in folder and subfolders for bucket '{bucketName}/{folderPath ?? "(Bucket Root)"}'.");
            return imageUrls;
        }
    }
}