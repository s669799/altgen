using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using System.Linq;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Find parameters with IFormFile type
        var fileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ParameterDescriptor.ParameterType == typeof(IFormFile))
            .ToList();

        foreach (var fileParam in fileParams)
        {
            // Create a new OpenApiRequestBody for the file upload
            var requestBody = new OpenApiRequestBody
            {
                Content = 
                {
                    [ "multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = 
                            {
                                // Define the IFormFile as a string with binary format (file)
                                [fileParam.Name] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            }
                        }
                    }
                }
            };

            // Set the requestBody for the operation, which will handle the file upload
            operation.RequestBody = requestBody;
        }
    }
}
