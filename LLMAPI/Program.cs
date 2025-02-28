using System.Reflection;
using LLMAPI.Controllers;
using LLMAPI.DTO;
using LLMAPI.Services.Interfaces;
using LLMAPI.Services.Google;
using LLMAPI.Services.OpenAI; 
using LLMAPI.Services.Llama;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.DeepSeek;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments in Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "API", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();  // Register the file upload filter
});

builder.Services.AddHttpClient();

// Add OpenRouterService (the core service for handling requests to the OpenRouter API)
builder.Services.AddScoped<OpenRouterService>();

// Google Services
builder.Services.AddScoped<IImageRecognitionService, GoogleImageRecognitionService>();
builder.Services.AddScoped<ITextGenerationService, GoogleTextGenerationService>();

// OpenAI Services
builder.Services.AddScoped<ITextGenerationService, OpenAITextGenerationService>();

// Meta Services
builder.Services.AddScoped<ITextGenerationService, LlamaTextGenerationService>();

// Add DeepSeek service (if applicable)
builder.Services.AddScoped<ITextGenerationService, DeepSeekTextGenerationService>();

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins("http://localhost:5256") // 5173
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors(MyAllowSpecificOrigins);
// Ensure HTTPS Redirection middleware
app.UseHttpsRedirection();
app.UseAuthorization();

// Redirect root URL to Swagger
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger");
    }
    else
    {
        await next.Invoke();
    }
});

// Map controllers
app.MapControllers();

app.Run();