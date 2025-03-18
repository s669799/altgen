using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using LLMAPI.Enums;



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

            string apiUrl = config["ApiUrl"]; // e.g., "https://localhost:5256/api/llm/process-request"
            string openRouterApiKey = config["OpenRouter:APIKey"];

            // Define the image URLs and prompts
            List<string> imageUrls = new List<string>()
            {
                // Add your image URLs here
                "https://as1.ftcdn.net/v2/jpg/01/11/22/44/1000_F_111224494_Vcl3eafzhx6Uc5GulbI2rk0eAOq0np59.jpg",
                "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3f/Walking_tiger_female.jpg/1200px-Walking_tiger_female.jpg",
                "https://upload.wikimedia.org/wikipedia/commons/a/a4/2019_Toyota_Corolla_Icon_Tech_VVT-i_Hybrid_1.8.jpg"
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

            // Prepare data for CSV output
            List<string> csvLines = new List<string>();
            csvLines.Add("Image URL,Prompt,Model,Response"); // CSV header

            // Loop through images and prompts
            foreach (string imageUrl in imageUrls)
            {
                foreach (string prompt in prompts)
                {
                    try
                    {
                        string response = await ProcessImage(apiUrl, openRouterApiKey, selectedModel, prompt, imageUrl);
                        string csvLine = $"{imageUrl.Replace(",", "")},{prompt.Replace(",", "")},{selectedModel},{response.Replace(",", "")}"; //Handle commas in values
                        csvLines.Add(csvLine);
                        Console.WriteLine($"Image: {imageUrl}, Prompt: {prompt}, Response: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing Image: {imageUrl}, Prompt: {prompt}. Error: {ex.Message}");
                        string csvLine = $"{imageUrl.Replace(",", "")},{prompt.Replace(",", "")},{selectedModel},Error: {ex.Message.Replace(",", "")}"; //Handle commas in values
                        csvLines.Add(csvLine);
                    }
                }
            }

            // Write to CSV file
            string filePath = "image_analysis_results.csv";
            File.WriteAllLines(filePath, csvLines);
            Console.WriteLine($"Results written to {filePath}");
        }

        static async Task<string> ProcessImage(string apiUrl, string apiKey, ModelType model, string prompt, string imageUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("HTTP-Referer", "ImageAnalysisConsoleApp"); // Or your app name
                client.DefaultRequestHeaders.Add("X-Title", "ImageAnalysisConsole");

                // Construct the request URL
                string modelString = GetEnumMemberValue(model);
                string requestUrl = $"{apiUrl}?model={modelString}&prompt={Uri.EscapeDataString(prompt)}&imageUrl={Uri.EscapeDataString(imageUrl)}";

                HttpResponseMessage response = await client.PostAsync(requestUrl, null);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    // Parse the JSON response
                    using (JsonDocument document = JsonDocument.Parse(responseContent))
                    {
                        JsonElement root = document.RootElement;
                        if (root.TryGetProperty("response", out JsonElement responseElement))
                        {
                            return responseElement.GetString();
                        }
                        else if (root.TryGetProperty("Response", out JsonElement responseElement2))
                        {
                            return responseElement2.GetString();
                        }
                        else
                        {
                            return $"Unexpected response format: {responseContent}";
                        }
                    }

                    //return responseContent;
                }
                else
                {
                    return $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}";
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
