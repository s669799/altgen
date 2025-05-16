using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LLMAPI.Controllers;
using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Service.Interfaces;
using LLMAPI.Services.Interfaces;
using Google.Protobuf;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Linq;

public class CNNControllerTests
{
    private readonly Mock<IImageRecognitionService> _mockImageRecognitionService = new();
    private readonly Mock<IImageFileService> _mockImageFileService = new();
    private readonly Mock<ICnnPredictionService> _mockCnnPredictionService = new();

    private readonly CNNController _controller;

    public CNNControllerTests()
    {
        _controller = new CNNController(
            _mockImageRecognitionService.Object,
            _mockImageFileService.Object,
            _mockCnnPredictionService.Object
        );
    }

    [Fact]
    public async Task PredictAndAnalyze_ReturnsBadRequest_WhenImageIsNull()
    {
        var request = new CNNRequest { Image = null! }; // null! to suppress CS8625

        var result = await _controller.PredictAndAnalyze(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid request. Ensure an image file is included.", badRequest.Value);
    }

    [Fact]
    public async Task PredictAndAnalyze_ReturnsBadRequest_WhenImageIsEmpty()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var request = new CNNRequest { Image = mockFile.Object };

        var result = await _controller.PredictAndAnalyze(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid request. Ensure an image file is included.", badRequest.Value);
    }

    [Fact]
    public async Task PredictAndAnalyze_Returns500_WhenCNNFails()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var mockFile = CreateMockFile(bytes);

        var request = new CNNRequest
        {
            Image = mockFile.Object,
            Model = Enum.GetValues<ModelType>().First() // fixed: no OpenRouter enum
        };

        _mockCnnPredictionService
            .Setup(s => s.PredictAircraftAsync(It.IsAny<ByteString>(), It.IsAny<string>()))
            .ReturnsAsync(new CNNResponse { Success = false, Detail = "CNN failed." });

        var result = await _controller.PredictAndAnalyze(request);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
        Assert.Contains("CNN prediction failed", status.Value!.ToString());
    }

    [Fact]
    public async Task PredictAndAnalyze_Returns500_WhenLLMFails()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var mockFile = CreateMockFile(bytes);

        var request = new CNNRequest
        {
            Image = mockFile.Object,
            Model = Enum.GetValues<ModelType>().First()
        };

        _mockCnnPredictionService
            .Setup(s => s.PredictAircraftAsync(It.IsAny<ByteString>(), It.IsAny<string>()))
            .ReturnsAsync(new CNNResponse { Success = true, PredictedAircraft = "F-16", Probability = 0.95 });

        _mockImageRecognitionService
            .Setup(s => s.AnalyzeImage(
                It.IsAny<string>(),
                It.IsAny<ByteString>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<double?>(),
                It.IsAny<double>()))
            .ReturnsAsync("Error: LLM service crashed");

        var result = await _controller.PredictAndAnalyze(request);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
        Assert.Contains("LLM analysis failed", status.Value!.ToString());
    }


    [Fact]
    public async Task PredictAndAnalyze_Returns500_OnUnhandledException()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(100);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Throws(new Exception("Simulated fatal"));

        var request = new CNNRequest
        {
            Image = mockFile.Object,
            Model = Enum.GetValues<ModelType>().First()
        };

        var result = await _controller.PredictAndAnalyze(request);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
    }

    private Mock<IFormFile> CreateMockFile(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(bytes.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
                .Returns<Stream, System.Threading.CancellationToken>((s, _) => s.WriteAsync(bytes, 0, bytes.Length));
        return mockFile;
    }
}

