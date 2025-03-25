using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LLMAPI.Enums;
using Newtonsoft.Json;
using LLMAPI.DTO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Formats.Asn1;
using Google.Cloud.Storage.V1;

namespace ImageAnalysisConsole
{
    class Program
    {
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
            Console.WriteLine($"OpenRouter API Key (first 4 chars): {openRouterApiKey?.Substring(0, 4)}...");

            string bucketName = "altgen_dam_bucket";
            string folderPath = "Cifar-10/sample10";
            List<string> imageUrls = await GetImageUrlsFromBucket(bucketName, folderPath);

            if (imageUrls.Count == 0)
            {
                Console.WriteLine($"No images found in bucket '{bucketName}' or an error occurred. Please check the bucket name and permissions.");
                return;
            }

            Console.WriteLine($"Found {imageUrls.Count} images in bucket '{bucketName}/{folderPath}'.");

            // List of prompts for the request, ranging from simple to more complex and demanding.
            List<string> prompts = new List<string>()
            {
                "Write an alt text for this image.",
                "Write a concise alt text identifying the key subjects or objects in this image and briefly describe the setting or context.",
                "Generate an accessible alt text for this image, adhering to best practices for web accessibility.  The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image.  Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message.  Consider the likely purpose and context of the image when writing the alt text to ensure relevance.  Do not include redundant phrases like 'image of' or 'picture of'.  Focus on delivering informative content."
            };

            List<ModelType> modelsToTest = new List<ModelType>()
            {
                ModelType.ChatGpt4o,
                ModelType.ChatGpt4oMini,
                ModelType.Gemini2_5Flash,
                ModelType.Gemini2_5FlashLite,
                ModelType.Claude3_5Sonnet,
                ModelType.Claude3Haiku,
                ModelType.Qwen2_5Vl72bInstruct,
                ModelType.Qwen2_5Vl7bInstruct
            };

            string baseResultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..", "Results");
            baseResultsFolder = Path.GetFullPath(baseResultsFolder);

            if (!Directory.Exists(baseResultsFolder))
            {
                Directory.CreateDirectory(baseResultsFolder);
            }

            // Construct timestamped folder name including bucket and path
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string folderPathForName = string.IsNullOrEmpty(folderPath) ? "bucket_root" : folderPath.Replace('/', '_'); // Replace slashes with underscores
            string timestampFolderName = $"{timestamp}_{bucketName}_{folderPathForName}";
            string resultsFolder = Path.Combine(baseResultsFolder, timestampFolderName);
            Directory.CreateDirectory(resultsFolder); // Create the new folder

            // Output folder name, bucket name, and folder path at the start
            Console.WriteLine($"\n--- Run Information ---");
            Console.WriteLine($"  Results Folder: {resultsFolder}");
            Console.WriteLine($"  Bucket Name:    {bucketName}");
            Console.WriteLine($"  Bucket Folder:  {folderPath ?? "(Bucket Root)"}"); // Handle null folderPath

            foreach (var selectedModel in modelsToTest)
            {
                List<ImageAnalysisResult> allResults = new();

                // CSV Setup
                string modelNameForFile = selectedModel.ToString();
                string csvPath = Path.Combine(resultsFolder, $"image_analysis_results_{modelNameForFile}.csv"); // Files in timestamped subfolder

                using (var writer = new StreamWriter(csvPath))
                using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // Write CSV header
                    csv.WriteHeader<CsvOutputRecord>();
                    csv.NextRecord();

                    Console.WriteLine($"\n--- Testing Model: {selectedModel} ---");

                    foreach (string imageUrl in imageUrls)
                    {
                        ImageAnalysisResult imageResult = new() { ImageUrl = imageUrl };

                        foreach (string prompt in prompts)
                        {
                            try
                            {
                                string response = await ProcessImage(apiUrl, openRouterApiKey, selectedModel, prompt, imageUrl);
                                imageResult.Results.Add(new PromptResult { Prompt = prompt, Response = response });

                                // Write CSV record
                                csv.WriteRecord(new CsvOutputRecord
                                {
                                    Model = selectedModel,
                                    ImageUrl = imageUrl,
                                    Prompt = prompt,
                                    Response = response
                                });
                                csv.NextRecord();

                                Console.WriteLine($"Model: {selectedModel}, Image: {imageUrl}, Prompt: {prompt}, Response: {response.Substring(0, Math.Min(response.Length, 100))}...");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error processing Model: {selectedModel}, Image: {imageUrl}, Prompt: {prompt}. Error: {ex.Message}");
                                imageResult.Results.Add(new PromptResult { Prompt = prompt, Response = $"Error: {ex.Message}" });

                                // Write CSV record for error case
                                csv.WriteRecord(new CsvOutputRecord
                                {
                                    Model = selectedModel,
                                    ImageUrl = imageUrl,
                                    Prompt = prompt,
                                    Response = $"Error: {ex.Message}"
                                });
                                csv.NextRecord();
                            }
                        }
                        allResults.Add(imageResult);
                    }
                } // CSV writer will be flushed and closed here


                // JSON Output - Files in timestamped subfolder
                string jsonPath = Path.Combine(resultsFolder, $"image_analysis_results_{modelNameForFile}.json");
                File.WriteAllText(jsonPath, JsonConvert.SerializeObject(allResults, Formatting.Indented));
                Console.WriteLine($"Results for {selectedModel} written as JSON to {jsonPath}");
                Console.WriteLine($"Results for {selectedModel} written as CSV to {csvPath}");
            }

            Console.WriteLine("\nAll model tests completed. Results saved in the Results folder.");
        }

        static async Task<string> ProcessImage(string apiUrl, string apiKey, ModelType model, string prompt, string imageUrl)
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
                    ImageUrl = imageUrl
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Construct the full URL correctly
                string fullUrl = $"{apiUrl}/process-request-body";

                HttpResponseMessage response = await client.PostAsync(fullUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Parse the JSON response
                        var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                        if (jsonResponse != null && jsonResponse.ContainsKey("response"))
                        {
                            return jsonResponse["response"];
                        }
                        return "No valid response found in JSON";
                    }
                    catch (Exception ex)
                    {
                        return $"Error parsing response: {ex.Message}";
                    }
                }
                else
                {
                    return $"Error: {response.StatusCode} - {responseContent}";
                }
            }
        }

        // Helper method to get EnumMember Value
        static string GetEnumMemberValue(ModelType value)
        {
            var enumType = value.GetType();
            var memberInfo = enumType.GetMember(value.ToString()).FirstOrDefault();

            if (memberInfo != null)
            {
                var enumMemberAttribute = (System.Runtime.Serialization.EnumMemberAttribute)memberInfo.GetCustomAttributes(typeof(System.Runtime.Serialization.EnumMemberAttribute), false).FirstOrDefault();
                if (enumMemberAttribute != null && !string.IsNullOrEmpty(enumMemberAttribute.Value))
                {
                    return enumMemberAttribute.Value;
                }
            }
            return value.ToString();
        }

        static async Task<List<string>> GetImageUrlsFromBucket(string bucketName, string folderPath = null)
        {
            var storageClient = await StorageClient.CreateAsync();
            var imageUrls = new List<string>();

            try
            {
                // If folderPath is provided, list objects with that prefix; otherwise list from the bucket root
                var objectList = string.IsNullOrEmpty(folderPath)
                    ? storageClient.ListObjectsAsync(bucketName)
                    : storageClient.ListObjectsAsync(bucketName, prefix: folderPath);

                await foreach (var storageObject in objectList)
                {
                    // Check if the object is likely an image (you can refine this check if needed)
                    if (storageObject.ContentType.StartsWith("image/"))
                    {
                        // Construct the public URL for the object in Google Cloud Storage
                        string imageUrl = $"https://storage.googleapis.com/{bucketName}/{storageObject.Name}";
                        imageUrls.Add(imageUrl);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing objects in bucket {bucketName} (folder: {folderPath}): {ex.Message}");
                return new List<string>(); // Return empty list on error
            }

            return imageUrls;
        }
    }
}
