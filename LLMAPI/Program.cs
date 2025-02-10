using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using LLMAPI.Controllers;
using LLMAPI.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(); // Register controllers
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Include XML comments in Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});


builder.Services.AddHttpClient();
    //client.DefaultRequestHeaders.Add("Authorization", "Bearer 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");


builder.Services.AddScoped<ILLMService, LLMService>();



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();