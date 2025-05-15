using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using LLMAPI.Service.Interfaces;
using LLMAPI.Services.Interfaces;
using LLMAPI.Services.OpenRouter;
using LLMAPI.Services.Google;
using LLMAPI.Services.CnnPrediction;
using LLMAPI.Service.Replicate;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with JSON options so that enums are serialized as strings.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments in Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
    c.OperationFilter<FileUploadOperationFilter>();
    c.EnableAnnotations();
});

builder.Services.AddHttpClient();

// Register services
builder.Services.AddScoped<OpenRouterService>();
builder.Services.AddScoped<IImageRecognitionService>(sp => sp.GetRequiredService<OpenRouterService>());
builder.Services.AddScoped<IImageFileService>(sp => sp.GetRequiredService<OpenRouterService>());
builder.Services.AddScoped<ITextGenerationService>(sp => sp.GetRequiredService<OpenRouterService>());
builder.Services.AddScoped<IGoogleService, GoogleImageRecognitionService>();
builder.Services.AddScoped<IImageFileService, GoogleImageRecognitionService>();
builder.Services.AddScoped<IReplicateService, ReplicateService>();
builder.Services.AddScoped<ICnnPredictionService, CNNPredictionService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5256",  // First origin
                "http://localhost:5173"   // Vue dev server
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

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

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();