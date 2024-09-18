![LinkPreview Logo](https://raw.githubusercontent.com/Zettersten/LinkPreview/main/icon.png)

# LinkPreview 🔗🖼️

[![NuGet version](https://badge.fury.io/nu/LinkPreview.svg)](https://badge.fury.io/nu/LinkPreview)

Welcome to the **LinkPreview** for .NET! This library provides a robust and efficient way to interact with the LinkPreview API, allowing you to easily fetch rich previews for URLs in your .NET applications. It features built-in caching, resilience policies, and flexible configuration options.

## Overview

The **LinkPreview** offers:

- **Easy Integration**: Simple setup with dependency injection in ASP.NET Core applications.
- **Caching**: Built-in memory caching with configurable TTL to reduce API calls and improve performance.
- **Resilience**: Implements retry and circuit breaker patterns for improved reliability.
- **Customizable**: Flexible configuration options to tailor the SDK to your needs.
- **Strongly Typed**: Fully typed responses for a great developer experience.

## Getting Started

### Installation

Install the LinkPreview package from NuGet:

```sh
dotnet add package LinkPreview
```

### Setting Up Dependency Injection

Configure the LinkPreview service in your `Program.cs` or `Startup.cs`:

#### Using Default Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddLinkPreviewService(Configuration);

    // Other service configurations...
}
```

#### Customizing Configuration

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddLinkPreviewService(options =>
    {
        options.ApiKey = "your-api-key-here";
        options.CacheTTLMinutes = 30;
    });

    // Other service configurations...
}
```

## Usage

Inject `ILinkPreviewService` into your controllers or services:

```csharp
public class MyController : ControllerBase
{
    private readonly ILinkPreviewService _linkPreviewService;

    public MyController(ILinkPreviewService linkPreviewService)
    {
        _linkPreviewService = linkPreviewService;
    }

    [HttpGet("preview")]
    public async Task<IActionResult> GetPreview(string url)
    {
        try
        {
            var preview = await _linkPreviewService.GetLinkPreviewAsync(url);
            return Ok(preview);
        }
        catch (LinkPreviewException ex)
        {
            return StatusCode((int)ex.StatusCode, ex.Message);
        }
    }
}
```

## How It Works

The LinkPreview:

1. Accepts a URL and optional fields to retrieve.
2. Checks the in-memory cache for an existing preview.
3. If not cached, makes an API call to the LinkPreview service.
4. Applies resilience policies (retry, circuit breaker) to handle transient failures.
5. Caches the result for future requests.
6. Returns a strongly-typed `LinkPreviewResponse`.

## Features

### Caching

The SDK includes built-in memory caching to reduce API calls:

```csharp
services.AddLinkPreviewService(options =>
{
    options.CacheTTLMinutes = 60; // Cache previews for 1 hour
});
```

### Optional Fields

Specify additional fields to retrieve:

```csharp
var preview = await _linkPreviewService.GetLinkPreviewAsync(
    "https://www.example.com",
    LinkPreviewOptionalField.ImageX | LinkPreviewOptionalField.IconType | LinkPreviewOptionalField.Locale
);
```

### Error Handling

The SDK throws `LinkPreviewException` for all errors, including API errors and network issues:

```csharp
try
{
    var preview = await _linkPreviewService.GetLinkPreviewAsync(url);
}
catch (LinkPreviewException ex)
{
    Console.WriteLine($"Error: {ex.Message}, Status Code: {ex.StatusCode}");
}
```

## Configuration Options

| Option          | Description                                     | Default               |
|-----------------|-------------------------------------------------|-----------------------|
| ApiKey          | Your LinkPreview API key                        | -                     |
| ApiBaseUrl      | Base URL for the LinkPreview API                | https://api.linkpreview.net |
| CacheTTLMinutes | Cache time-to-live in minutes                   | 60                    |

## Supported Classes and Methods

### ILinkPreviewService Interface

| Method                      | Description                                     |
|-----------------------------|-------------------------------------------------|
| `GetLinkPreviewAsync(string url, LinkPreviewOptionalField? optionalFields = null, CancellationToken cancellationToken = default)` | Retrieves a link preview for the specified URL. |

### LinkPreviewResponse Class

Contains properties for all preview data, including:

- Title
- Description
- Image URL
- Site Name
- Favicon
- And more, depending on the optional fields requested

### LinkPreviewOptionalField Enum

Flags enum for specifying additional fields to retrieve:

- Canonical
- Locale
- SiteName
- ImageX, ImageY, ImageSize, ImageType
- Icon, IconX, IconY, IconSize, IconType

## Performance Considerations

- **Efficient Caching**: Reduces API calls for frequently requested URLs.
- **Resilience Policies**: Implements retry and circuit breaker patterns to handle transient failures gracefully.
- **Asynchronous Operations**: All operations are asynchronous for improved application responsiveness.

## Requirements

- **.NET 6.0 or higher**: The SDK is built targeting .NET 6.0 and above.
- **ASP.NET Core**: Designed to work seamlessly with ASP.NET Core dependency injection.

## License

This library is available under the [MIT License](LICENSE).

## Contributions

Contributions are welcome! Please open an issue to discuss any changes before submitting a pull request.

## About

For more information, support, or to report issues, please visit the [GitHub Repository](https://github.com/zettersten/LinkPreview).

---

Thank you for using the **LinkPreview**. We look forward to seeing what you build with it!
