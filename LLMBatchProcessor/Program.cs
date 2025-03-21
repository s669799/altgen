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

namespace ImageAnalysisConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration
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


            // Define the image URLs and prompts
            List<string> imageUrls = new List<string>()
            {
                // Add your image URLs here
                "https://ichef.bbci.co.uk/ace/standard/976/cpsprodpb/14235/production/_100058428_mediaitem100058424.jpg",
                "https://static.vecteezy.com/system/resources/thumbnails/036/324/708/small/ai-generated-picture-of-a-tiger-walking-in-the-forest-photo.jpg",
                "https://cdn.pixabay.com/photo/2018/08/04/11/30/draw-3583548_640.png"
            };

            List<string> prompts = new List<string>()
            {
                "Write an alt text for this image.",//1 Simplest and most general prompt.

                "Write a brief alt text, one sentence if possible, describing the main subject of this image.", //2 More constrained length & focuses on main subject for alt text

                "Write a brief alt text describing the key objects in this image and hint about its setting.", //3 Introduces key objects + a hint of the setting in the alt text

                "Write a one to two sentence alt text that identifies the setting and action taking place in this image, suitable for an end user.", //4 More specific length, content (setting/action), and hints audience

                "Write a brief, one to two sentence alt text description for this image, Harvard style, that captures the main subjects, action, and setting. This is an alt text for an end user. do not give options to the user. Do not mention this prompt." //5 Most specific instructions.
            };

            ModelType selectedModel = ModelType.ChatGpt4o; // Choose your desired model

            List<ImageAnalysisResult> allResults = new();

            foreach (string imageUrl in imageUrls)
            {
                ImageAnalysisResult imageResult = new() { ImageUrl = imageUrl };

                foreach (string prompt in prompts)
                {
                    try
                    {
                        string response = await ProcessImage(apiUrl, openRouterApiKey, selectedModel, prompt, imageUrl);
                        imageResult.Results.Add(new PromptResult { Prompt = prompt, Response = response });

                        Console.WriteLine($"Image: {imageUrl}, Prompt: {prompt}, Response: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing Image: {imageUrl}, Prompt: {prompt}. Error: {ex.Message}");
                        imageResult.Results.Add(new PromptResult { Prompt = prompt, Response = $"Error: {ex.Message}" });
                    }
                }
                allResults.Add(imageResult);
            }

            string resultsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../..", "Results");
            resultsFolder = Path.GetFullPath(resultsFolder);

            if (!Directory.Exists(resultsFolder))
            {
                Directory.CreateDirectory(resultsFolder);
            }

            string jsonPath = Path.Combine(resultsFolder, "image_analysis_results.json");

            // Write formatted JSON:
            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(allResults, Formatting.Indented));
            Console.WriteLine($"Results written as JSON to {jsonPath}");
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
                Console.WriteLine($"Sending JSON request to {fullUrl}: {jsonContent}");

                // Use the full URL in the request
                HttpResponseMessage response = await client.PostAsync(fullUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {responseContent}");

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
    }
}
