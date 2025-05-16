using Xunit;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LLMAPI.Controllers;
using LLMAPI.DTO;
using LLMAPI.Enums;
using LLMAPI.Service.Interfaces;
using LLMAPI.Services.Interfaces;
using LLMAPI.Helpers;
using Google.Protobuf;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Linq;

public class LLMControllerTests
{
    private readonly Mock<IImageRecognitionService> _mockImageRecognition = new();
    private readonly Mock<ITextGenerationService> _mockTextGen = new();
    private readonly Mock<IImageFileService> _mockImageFileService = new();

    private readonly LLMController _controller;

    public LLMControllerTests()
    {
        _controller = new LLMController(
            _mockImageRecognition.Object,
            _mockTextGen.Object,
            _mockImageFileService.Object
        );
    }

    [Fact]
    public async Task ProcessForm_ReturnsBadRequest_WhenImageIsMissing()
    {
        var request = new LLMFormRequest
        {
            Image = null! 
        };

        var result = await _controller.ProcessForm(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No image provided.", badRequest.Value);
    }

    [Fact]
    public async Task ProcessForm_ReturnsBadRequest_WhenImageIsEmpty()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        var request = new LLMFormRequest { Image = mockFile.Object };

        var result = await _controller.ProcessForm(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("No image provided.", badRequest.Value);
    }

    [Fact]
    public async Task ProcessForm_ReturnsBadRequest_WhenConversionFails()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(123);

        _mockImageFileService
            .Setup(s => s.ConvertImageToByteString(It.IsAny<IFormFile>()))
            .ReturnsAsync((ByteString?)null); 

        var request = new LLMFormRequest
        {
            Image = mockFile.Object,
            Model = EnumHelper.GetEnumMemberValue(ModelType.ChatGpt4_1) 
        };

        var result = await _controller.ProcessForm(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Could not convert image to bytes.", badRequest.Value);
    }
    
}


