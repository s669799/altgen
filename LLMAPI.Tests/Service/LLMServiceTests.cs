using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;
using LLMAPI.Service;

namespace LLMAPI.Tests.Service
{
    public class LLMServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly LLMService _llmService;

        public LLMServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration values
            _mockConfiguration.Setup(x => x["OpenRouter:APIKey"]).Returns("test-api-key");
            _mockConfiguration.Setup(x => x["OpenRouter:APIUrl"]).Returns("https://api.test.com/v1/chat/completions");

            _llmService = new LLMService(_mockHttpClientFactory.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task GetDataOpenRouter_SuccessfulResponse_ReturnsContent()
        {
            // Arrange
            var expectedResponse = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "Test response"
                        }
                    }
                }
            };

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(expectedResponse))
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            var result = await _llmService.GetDataOpenRouter("test-model", "test-prompt");

            // Assert
            Assert.Equal("Test response", result);
        }

        [Fact]
        public async Task GetDataOpenRouter_ErrorResponse_ReturnsError()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

            // Act
            var result = await _llmService.GetDataOpenRouter("test-model", "test-prompt");

            // Assert
            Assert.Equal("Error: BadRequest", result);
        }

        [Fact]
        public async Task GetDataFromImageGoogle_NullImage_ReturnsErrorMessage()
        {
            // Arrange
            IFormFile nullImage = null;

            // Act
            var result = await _llmService.GetDataFromImageGoogle(nullImage);

            // Assert
            Assert.Equal("No image uploaded.", result);
        }

        [Fact]
        public async Task GetDataFromImageGoogle_EmptyImage_ReturnsErrorMessage()
        {
            // Arrange
            var mockFormFile = new Mock<IFormFile>();
            mockFormFile.Setup(x => x.Length).Returns(0);

            // Act
            var result = await _llmService.GetDataFromImageGoogle(mockFormFile.Object);

            // Assert
            Assert.Equal("No image uploaded.", result);
        }
    }
} 