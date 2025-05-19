using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using LLMAPI.Services.OpenRouter;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Google.Protobuf;
using RichardSzalay.MockHttp;
using System.Text.Json;

public class OpenRouterServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenRouterService _service;

    public OpenRouterServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockHttp = new MockHttpMessageHandler();

        var client = _mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://fakeapi.com");

        var clientFactoryMock = new Mock<IHttpClientFactory>();
        clientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        _httpClientFactory = clientFactoryMock.Object;

        _mockConfig.Setup(c => c["OpenRouter:APIKey"]).Returns("fake-key");
        _mockConfig.Setup(c => c["OpenRouter:APIUrl"]).Returns("https://fakeapi.com/api");
        _mockConfig.Setup(c => c["OpenRouter:Referer"]).Returns("http://localhost");
        _mockConfig.Setup(c => c["OpenRouter:Title"]).Returns("Test");

        _service = new OpenRouterService(_httpClientFactory, _mockConfig.Object);
    }

    [Fact]
    public async Task GenerateText_ThrowsIfModelIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GenerateText("", "prompt"));
    }

    [Fact]
    public async Task GenerateText_ThrowsIfPromptIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GenerateText("model", ""));
    }

    [Fact]
    public async Task GenerateText_ReturnsResponse_OnSuccess()
    {
        // Arrange
        var expected = "This is a generated response";
        _mockHttp.When("https://fakeapi.com/api")
            .Respond("application/json", JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new {
                        message = new {
                            content = expected
                        }
                    }
                }
            }));

        // Act
        var result = await _service.GenerateText("model", "prompt");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task AnalyzeImage_ThrowsIfModelIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.AnalyzeImage("", "http://img.jpg", "prompt", null, null, 1.0));
    }

    [Fact]
    public async Task AnalyzeImage_ThrowsIfImageUrlIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.AnalyzeImage("model", "", "prompt", null, null, 1.0));
    }

    [Fact]
    public async Task AnalyzeImage_FromBytes_ThrowsIfImageBytesAreEmpty()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.AnalyzeImage("model", ByteString.Empty, "prompt", null, null, 1.0));
    }

    [Fact]
    public async Task ConvertImageToByteString_ReturnsNullIfEmpty()
    {
        var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var result = await _service.ConvertImageToByteString(mockFile.Object);
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadImageFileAsync_ThrowsIfUrlIsInvalid()
    {
        await Assert.ThrowsAsync<UriFormatException>(() =>
            _service.ReadImageFileAsync("not-a-url"));
    }
}
