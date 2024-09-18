using System.Net;

namespace LinkPreview;

/// <summary>
/// Represents a custom exception for LinkPreview service errors.
/// </summary>
public sealed class LinkPreviewException : Exception
{
    /// <summary>
    /// Gets the HTTP status code associated with the error.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the error code from the LinkPreview service.
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// Gets the error description from the LinkPreview service.
    /// </summary>
    public string ErrorDescription { get; }

    /// <summary>
    /// Gets the URL that caused the error, if available.
    /// </summary>
    public string? Url { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkPreviewException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorResponse">The error response from the LinkPreview service.</param>
    public LinkPreviewException(HttpStatusCode statusCode, LinkPreviewErrorResponse errorResponse)
        : base(GetExceptionMessage(statusCode, errorResponse))
    {
        StatusCode = statusCode;
        ErrorCode = errorResponse.Error;
        ErrorDescription = errorResponse.Description;
        Url = string.IsNullOrEmpty(errorResponse.Url) ? null : errorResponse.Url;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkPreviewException"/> class.
    /// </summary>
    /// <param name="errorDescription">The error response from the LinkPreview service.</param>
    internal LinkPreviewException(string errorDescription)
    {
        StatusCode = HttpStatusCode.InternalServerError;
        ErrorCode = 0;
        ErrorDescription = errorDescription;
        Url = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LinkPreviewException"/> class.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorDescription">The error response from the LinkPreview service.</param>
    internal LinkPreviewException(HttpStatusCode statusCode, string errorDescription)
    {
        StatusCode = statusCode;
        ErrorCode = 0;
        ErrorDescription = errorDescription;
        Url = null;
    }

    /// <summary>
    /// Generates a detailed exception message.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="errorResponse">The error response from the LinkPreview service.</param>
    /// <returns>A formatted exception message.</returns>
    private static string GetExceptionMessage(HttpStatusCode statusCode, LinkPreviewErrorResponse errorResponse)
    {
        return $"LinkPreview API Error: HTTP {(int)statusCode} ({statusCode}), " +
               $"Error Code: {errorResponse.Error}, " +
               $"Description: {errorResponse.Description}" +
               (string.IsNullOrEmpty(errorResponse.Url) ? "" : $", URL: {errorResponse.Url}");
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"{base.ToString()}, " +
               $"StatusCode: {StatusCode}, " +
               $"ErrorCode: {ErrorCode}, " +
               $"ErrorDescription: {ErrorDescription}" +
               (Url != null ? $", URL: {Url}" : "");
    }
}