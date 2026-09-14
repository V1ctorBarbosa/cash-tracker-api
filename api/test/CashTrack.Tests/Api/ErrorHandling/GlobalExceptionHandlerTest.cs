using System.Text.Json;
using CashTrack.Api.ErrorHandling;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CashTrack.Tests.Api.ErrorHandling;

public sealed class GlobalExceptionHandlerTest
{
    private readonly ILogger<GlobalExceptionHandler> _logger = Substitute.For<ILogger<GlobalExceptionHandler>>();
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTest()
    {
        _sut = new GlobalExceptionHandler(_logger);
    }

    [Fact]
    public async Task TryHandleAsync_WritesBadRequest_ForArgumentException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new ArgumentException("Name cannot be empty.");

        // Act
        var wasHandled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        wasHandled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(ValidationErrorCode.InvalidRequest);
        body.Message.Should().Be("Name cannot be empty.");
    }

    [Fact]
    public async Task TryHandleAsync_WritesInternalServerError_ForAnyOtherException()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var exception = new InvalidOperationException("Something unexpected happened.");

        // Act
        var wasHandled = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        // Assert
        wasHandled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var body = await ReadResponseBodyAsync(httpContext);
        body.ErrorCode.Should().Be(GeneralErrorCode.UnexpectedError);
        body.Message.Should().NotBe(exception.Message);
    }

    private static DefaultHttpContext CreateHttpContext() => new() { Response = { Body = new MemoryStream() } };

    private static async Task<ApiErrorResponse> ReadResponseBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return body!;
    }
}
