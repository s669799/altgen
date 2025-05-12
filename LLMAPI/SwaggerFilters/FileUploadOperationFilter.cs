using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Linq;
using Microsoft.AspNetCore.Http;

/// <summary>
/// An Operation Filter for Swagger/OpenAPI that configures request body for endpoints accepting file uploads (IFormFile).
/// This filter modifies the Swagger documentation to correctly represent file upload endpoints as using 'multipart/form-data'
/// and specifies the parameter of type IFormFile as a binary format in the request body schema.
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    /// <summary>
    /// Applies the filter to modify the Swagger/OpenAPI operation representation for file upload endpoints.
    /// It checks for parameters of type IFormFile in the API endpoint description and, if found,
    /// configures the request body to use 'multipart/form-data' with a binary schema for the file parameter.
    /// </summary>
    /// <param name="operation">The Swagger/OpenAPI operation object being configured.</param>
    /// <param name="context">The context information about the operation, including its API description.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ParameterDescriptor.ParameterType == typeof(IFormFile))
            .ToList();

        if (fileParams.Any())
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                [fileParams.First().Name] = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}
