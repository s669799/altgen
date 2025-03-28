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
    /// <summary>
    /// Console application to perform image analysis using a configured API, testing various Large Language Models (LLMs)
    /// against images fetched from a Google Cloud Storage bucket. Results are saved in CSV and JSON format, with support for processing multiple folders and subfolders, and improved console output readability.
    /// </summary>
    class Program
    {
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
            Console.WriteLine($"OpenRouter API Key (first 4 chars): {openRouterApiKey?.Substring(0, 4)}...");

            string bucketName = "altgen_dam_bucket";
            List<string> folderPaths = new List<string>()
            {
                "imagenet-sample-images/imagenet10",        // Options: imagenet10, imagenet20, imagenet50
                "Cifar-10/sample10",                        // Options: sample10, sample30, sample100 (now with subfolder support)
                "airplanes"                                 // Options : airplanes (20 images)
            };

            // List of prompts for the request.
            List<string> prompts = new List<string>()
            {
                "Write an alt text for this image.",
                //"Write a concise alt text identifying the key subjects or objects in this image and briefly describe the setting or context.",
                //"Generate an accessible alt text for this image, adhering to best practices for web accessibility. The alt text should be concise (one to two sentences maximum) yet effectively communicate the essential visual information for someone who cannot see the image. Describe the key figures or subjects, their relevant actions or states, the overall scene or environment, and any objects critical to understanding the image's context or message. Consider the likely purpose and context of the image when writing the alt text to ensure relevance. Do not include redundant phrases like 'image of' or 'picture of'. Focus on delivering informative content. This is an alt text for an end user. Avoid mentioning this prompt or any kind of greeting or introduction. Just provide the alt text description directly, without any conversational preamble like 'Certainly,' 'Here's the alt text,' 'Of course,' or similar."
            };

            // List of ModelType enums representing the Large Language Models to be tested.
            List<ModelType> modelsToTest = new List<ModelType>()
            {
                ModelType.ChatGpt4o,
                //ModelType.ChatGpt4oMini,
                //ModelType.Gemini2_5Flash,
                //ModelType.Gemini2_5FlashLite,
                //ModelType.Claude3_5Sonnet,
                //ModelType.Claude3Haiku,
                //ModelType.Qwen2_5Vl72bInstruct,
                //ModelType.Qwen2_5Vl7bInstruct
            };

            string baseResultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..", "Results");
            baseResultsFolder = Path.GetFullPath(baseResultsFolder);

            if (!Directory.Exists(baseResultsFolder))
            {
                Directory.CreateDirectory(baseResultsFolder);
            }


            foreach (string folderPath in folderPaths)
            {
                // Add folder separator to console output
                Console.WriteLine("\n" + new string('-', 50)); // Separator line
                Console.WriteLine($"--- Processing Folder: {folderPath} ---");
                Console.WriteLine(new string('-', 50)); // Separator line


                List<string> imageUrls = await GetImageUrlsFromBucket(bucketName, folderPath);

                if (imageUrls.Count == 0)
                {
                    Console.WriteLine($"No images found in bucket '{bucketName}/{folderPath}' or an error occurred. Please check the bucket name and permissions.");
                    continue; // Skip to the next folder if no images are found or an error occurs
                }

                Console.WriteLine($"Found {imageUrls.Count} images in bucket '{bucketName}/{folderPath}'.");

                // Construct timestamped folder name including bucket and path for each folder processed
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string folderPathForName = string.IsNullOrEmpty(folderPath) ? "bucket_root" : folderPath.Replace('/', '_'); // Replace slashes with underscores
                string timestampFolderName = $"{timestamp}_{bucketName}_{folderPathForName}";
                string resultsFolder = Path.Combine(baseResultsFolder, timestampFolderName);
                Directory.CreateDirectory(resultsFolder); // Create the new folder

                // Output folder name, bucket name, and folder path at the start of each folder processing
                Console.WriteLine($"\n--- Run Information ---");
                Console.WriteLine($"  Results Folder: {resultsFolder}");
                Console.WriteLine($"  Bucket Name:    {bucketName}");
                Console.WriteLine($"  Bucket Folder:  {folderPath ?? "(Bucket Root)"}"); // Handle null folderPath


                foreach (var selectedModel in modelsToTest)
                {
                    List<ImageAnalysisResult> allResults = new();

                    // CSV Setup for each model within each folder
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

                                    // Write CSV record for each prompt and image
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

                    // JSON Output - Files are placed in the timestamped subfolder for each model within each folder
                    string jsonPath = Path.Combine(resultsFolder, $"image_analysis_results_{modelNameForFile}.json");
                    File.WriteAllText(jsonPath, JsonConvert.SerializeObject(allResults, Formatting.Indented));
                    Console.WriteLine($"Results for {selectedModel} written as JSON to {jsonPath}");
                    Console.WriteLine($"Results for {selectedModel} written as CSV to {csvPath}");
                }
            } // Folder path loop ends here, next folder path will be processed

            Console.WriteLine("\nAll folder and model tests completed. Results saved in individual folders within the Results folder.");
        }

        /// <summary>
        /// Sends a request to the API endpoint to process an image for alt text generation using specified parameters.
        /// </summary>
        /// <param name="apiUrl">The base URL of the API, configured in appsettings.json.</param>
        /// <param name="apiKey">The API key for accessing the LLM service, configured in appsettings.json.</param>
        /// <param name="model">The <see cref="ModelType"/> enum value, specifying which LLM model to use for analysis.</param>
        /// <param name="prompt">The prompt text to guide the LLM in generating the alt text.</param>
        /// <param name="imageUrl">The public URL of the image to be analyzed.</param>
        /// <returns>A string containing the generated alt text response from the API, or an error message on failure.</returns>
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

                // Construct the full API endpoint URL for processing requests via request body.
                string fullUrl = $"{apiUrl}/process-request-body";

                HttpResponseMessage response = await client.PostAsync(fullUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Deserialize the JSON response to extract the text content.
                        var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                        if (jsonResponse != null && jsonResponse.ContainsKey("response"))
                        {
                            return jsonResponse["response"];
                        }
                        return "No valid response found in JSON"; // Indicate if no 'response' key is found.
                    }
                    catch (Exception ex)
                    {
                        return $"Error parsing response: {ex.Message}"; // Error message if JSON parsing fails.
                    }
                }
                else
                {
                    return $"Error: {response.StatusCode} - {responseContent}"; // Return status code and content for HTTP errors.
                }
            }
        }

        /// <summary>
        /// Helper method to retrieve the EnumMember Value of a <see cref="ModelType"/> enum.
        /// This value is used when sending requests to the API to specify the model in string format as expected by the API.
        /// </summary>
        /// <param name="value">The <see cref="ModelType"/> enum value for which to get the EnumMember attribute's Value.</param>
        /// <returns>The string value associated with the EnumMember attribute of the enum, or the enum's name if no attribute is set.</returns>
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
            return value.ToString(); // Fallback to ToString if EnumMember attribute is not defined or has no value.
        }

        /// <summary>
        /// Retrieves a list of image URLs from a Google Cloud Storage bucket, including images from **within the specified folder and all its subfolders**.
        /// Returns the count of images found in the logs.
        /// </summary>
        /// <param name="bucketName">The name of the GCS bucket to access.</param>
        /// <param name="folderPath">Optional. If specified, images from **this folder and all subfolders** are listed. If null or empty, images from the bucket root are listed.</param>
        /// <returns>A List of strings, each being a publicly accessible URL for an image in the specified GCS bucket and folder (or subfolder). Returns an empty list if no images are found or an error occurs.</returns>
        static async Task<List<string>> GetImageUrlsFromBucket(string bucketName, string folderPath = null)
        {
            var storageClient = await StorageClient.CreateAsync();
            var imageUrls = new List<string>();
            int imageCount = 0; // Initialize image counter

            try
            {
                string prefix = folderPath;
                if (!string.IsNullOrEmpty(prefix))
                {
                    // Ensure the prefix ends with a '/' to only match within the folder
                    if (!prefix.EndsWith("/"))
                    {
                        prefix += "/";
                    }
                }

                // List objects in the bucket, using the potentially modified prefix.
                var objectList = string.IsNullOrEmpty(prefix)
                    ? storageClient.ListObjectsAsync(bucketName)
                    : storageClient.ListObjectsAsync(bucketName, prefix: prefix);


                await foreach (var storageObject in objectList)
                {
                    // Check if the storage object's content type indicates it's an image.
                    if (storageObject.ContentType.StartsWith("image/"))
                    {
                        // Construct the public URL for accessing the image.
                        string imageUrl = $"https://storage.googleapis.com/{bucketName}/{storageObject.Name}";
                        imageUrls.Add(imageUrl);
                        imageCount++; // Increment the image count
                    }
                }
                Console.WriteLine($"    Found {imageCount} images in folder and subfolders."); // Output total count of images found, now including subfolders
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing objects in bucket {bucketName} (folder: {folderPath}): {ex.Message}");
                return new List<string>(); // Return an empty list in case of any error.
            }

            return imageUrls; // Return the list of image URLs found.
        }
    }
}
