using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Google.Protobuf;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Net;
using System.Net.Http.Headers;
using LLMAPI.Services.CnnPrediction;
using LLMAPI.DTO;
using RichardSzalay.MockHttp;

public class CNNPredictionServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _mockClient;
    private readonly CNNPredictionService _service;

    public CNNPredictionServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockHttp = new MockHttpMessageHandler();
        _mockClient = _mockHttp.ToHttpClient();

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_mockClient);

        _mockConfig.Setup(c => c["CnnApi:Url"]).Returns("https://cnnapi.com");

        _service = new CNNPredictionService(httpClientFactory.Object, _mockConfig.Object);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsError_IfImageBytesIsEmpty()
    {
        var response = await _service.PredictAircraftAsync(ByteString.Empty, "file.jpg");

        Assert.False(response.Success);
        Assert.Equal("Image data is empty.", response.Detail);
    }

    [Fact]
    public async Task PredictAircraftAsync_UsesDefaultFilename_IfMissing()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 1, 2, 3 });

        _mockHttp.When("https://cnnapi.com/predict")
                 .Respond("application/json", JsonSerializer.Serialize(new CNNResponse
                 {
                     Success = true,
                     PredictedAircraft = "F-16",
                     Probability = 0.99
                 }));

        var result = await _service.PredictAircraftAsync(byteString, "");

        Assert.True(result.Success);
        Assert.Equal("F-16", result.PredictedAircraft);
        Assert.Equal(0.99, result.Probability);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsParsedResponse_OnSuccess()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 1, 2, 3 });

        var expectedResponse = new CNNResponse
        {
            Success = true,
            PredictedAircraft = "F/A-18 Hornet",
            Probability = 0.87
        };

        _mockHttp.When("https://cnnapi.com/predict")
                 .Respond("application/json", JsonSerializer.Serialize(expectedResponse));

        var result = await _service.PredictAircraftAsync(byteString, "image.jpg");

        Assert.True(result.Success);
        Assert.Equal("F/A-18 Hornet", result.PredictedAircraft);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsFailure_IfCnnReturnsSuccessFalse()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 4, 5, 6 });

        var responseBody = new CNNResponse
        {
            Success = false,
            Detail = "Classification failed"
        };

        _mockHttp.When("https://cnnapi.com/predict")
                 .Respond("application/json", JsonSerializer.Serialize(responseBody));

        var result = await _service.PredictAircraftAsync(byteString, "bad.jpg");

        Assert.False(result.Success);
        Assert.Contains("Classification failed", result.Detail);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsFailure_OnHttpError()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 7, 8, 9 });

        _mockHttp.When("https://cnnapi.com/predict")
                 .Respond(HttpStatusCode.BadRequest, "application/json", "{\"detail\":\"Invalid image\"}");

        var result = await _service.PredictAircraftAsync(byteString, "invalid.jpg");

        Assert.False(result.Success);
        Assert.Contains("Invalid image", result.Detail);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsFailure_OnInvalidJson()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 10, 11, 12 });

        _mockHttp.When("https://cnnapi.com/predict")
                 .Respond("application/json", "{ not valid json }");

        var result = await _service.PredictAircraftAsync(byteString, "corrupt.jpg");

        Assert.False(result.Success);
        Assert.Contains("Failed to parse", result.Detail);
    }

    [Fact]
public async Task PredictAircraftAsync_ReturnsFailure_OnTimeout()
{
    var byteString = ByteString.CopyFrom(new byte[] { 1, 2, 3 });

    _mockHttp.When("https://cnnapi.com/predict")
             .Throw(new TaskCanceledException("Simulated timeout", new TimeoutException("Real timeout")));

    var result = await _service.PredictAircraftAsync(byteString, "timeout.jpg");

    Assert.False(result.Success);
    Assert.Contains("Timeout", result.Detail);
}


    [Fact]
    public async Task PredictAircraftAsync_ReturnsFailure_OnHttpRequestException()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 16, 17, 18 });

        _mockHttp.When("https://cnnapi.com/predict")
                 .Throw(new HttpRequestException("Connection error"));

        var result = await _service.PredictAircraftAsync(byteString, "fail.jpg");

        Assert.False(result.Success);
        Assert.Contains("Error communicating with CNN service", result.Detail);
    }

    [Fact]
    public async Task PredictAircraftAsync_ReturnsFailure_OnUnexpectedException()
    {
        var byteString = ByteString.CopyFrom(new byte[] { 19, 20, 21 });

        _mockHttp.When("https://cnnapi.com/predict")
                 .Throw(new Exception("Something bad happened"));

        var result = await _service.PredictAircraftAsync(byteString, "explode.jpg");

        Assert.False(result.Success);
        Assert.Contains("An unexpected error occurred", result.Detail);
    }
}
