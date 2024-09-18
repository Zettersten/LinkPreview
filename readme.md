![BadBotBlocker Icon](https://raw.githubusercontent.com/Zettersten/BadBotBlocker/main/icon.png)

# BadBotBlocker 🛡️

[![NuGet version](https://badge.fury.io/nu/BadBotBlocker.svg)](https://badge.fury.io/nu/BadBotBlocker)

Welcome to the **BadBotBlocker** ASP.NET Core middleware! This library provides an efficient and customizable way to block malicious bots, scrapers, and unwanted traffic based on User-Agent patterns and IP ranges. It leverages a popular list of rules from an `.htaccess` file and focuses on extreme performance using the latest C# features.

## Overview

The **BadBotBlocker** middleware offers:

- **Default Blocking Rules**: Preloaded with a comprehensive list of bad bot User-Agent patterns and IP ranges.
- **Customizable**: Easily add or remove patterns and IP ranges to suit your application's needs.
- **High Performance**: Optimized pattern matching and minimal overhead.
- **Extensibility**: Provides extension methods for dependency injection and middleware configuration.

## Getting Started

### Installation

You can install the **BadBotBlocker** package from [NuGet](https://www.nuget.org/packages/BadBotBlocker/):

```sh
dotnet add package BadBotBlocker
```

### Setting Up Dependency Injection

To use the **BadBotBlocker** middleware in your ASP.NET Core application, configure your services in `Program.cs` or `Startup.cs`.

#### Using Default Blocking Rules

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddBadBotBlocker();

    // Other service configurations...
}
```

#### Customizing Blocking Rules

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddBadBotBlocker(options =>
    {
        options.ClearBadBotPatterns();
        options.ClearBlockedIPRanges();

        options.AddBadBotPattern("^MyCustomBot")
               .AddBlockedIPRange("192.168.1.0/24");
    });

    // Other service configurations...
}
```

## Usage

In your `Program.cs` or `Startup.cs`, add the middleware to the HTTP request pipeline:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseBadBotBlocker();

    // Other middleware...
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

## How It Works

The **BadBotBlocker** middleware intercepts incoming HTTP requests and performs the following checks:

1. **IP Address Check**: Determines if the client's IP address falls within any of the blocked IP ranges.
2. **User-Agent Check**: Matches the client's User-Agent string against a list of known bad bot patterns.

If a match is found in either check, the middleware responds with a `403 Forbidden` status code, effectively blocking the request.

## Default Blocking Rules

The middleware comes preloaded with a comprehensive list of bad bot User-Agent patterns and IP ranges, extracted from a popular `.htaccess` file. These include:

- **Bad Bot User-Agent Patterns**: Over 200 patterns matching known malicious bots and scrapers.
- **Blocked IP Ranges**: Specific IP ranges associated with unwanted traffic.

### Examples of Default User-Agent Patterns

- `^Aboundex`
- `^80legs`
- `Baiduspider` (Aggressive Chinese Search Engine)
- `Yandex` (Aggressive Russian Search Engine)
- `Acunetix` (Vulnerability Scanner)

### Examples of Default Blocked IP Ranges

- `38.100.19.8/29`
- `65.213.208.128/27`
- IP ranges associated with Cyveillance and other entities.

## Extensibility

You can customize the blocking rules by adding or removing patterns and IP ranges:

```csharp
services.AddBadBotBlocker(options =>
{
    // Remove all default patterns and IP ranges
    options.ClearBadBotPatterns();
    options.ClearBlockedIPRanges();

    // Add custom patterns
    options.AddBadBotPattern("^CustomBot")
           .AddBadBotPattern("BadScraper");

    // Add custom IP ranges
    options.AddBlockedIPRange("123.456.789.0/24");
});
```

## Supported Classes and Methods

### BadBotOptions Class

| Method                      | Description                                     |
|-----------------------------|-------------------------------------------------|
| `AddBadBotPattern(string)`  | Adds a User-Agent pattern to block.             |
| `AddBlockedIPRange(string)` | Adds an IP range in CIDR notation to block.     |
| `ClearBadBotPatterns()`     | Clears all User-Agent patterns.                 |
| `ClearBlockedIPRanges()`    | Clears all blocked IP ranges.                   |

### BadBotMiddlewareExtensions Class

| Method                      | Description                                     |
|-----------------------------|-------------------------------------------------|
| `UseBadBotBlocker()`        | Adds the middleware to the HTTP request pipeline. |
| `AddBadBotBlocker()`        | Registers the middleware services with default configurations. |
| `AddBadBotBlocker(Action<BadBotOptions>)` | Registers the middleware services with custom configurations. |

## Performance Considerations

- **Optimized Pattern Matching**: Differentiates between simple `StartsWith` patterns and complex regex patterns to minimize overhead.
- **Compiled Regular Expressions**: Uses `RegexOptions.Compiled` for regex patterns to improve matching performance.
- **Efficient IP Address Checking**: Utilizes an extension method for `IPAddress` to check IP ranges without external libraries.

## Example

### Blocking Custom Bots and IP Ranges

```csharp
services.AddBadBotBlocker(options =>
{
    options.AddBadBotPattern("^SneakyBot")
           .AddBadBotPattern("EvilScraper")
           .AddBlockedIPRange("10.0.0.0/8")
           .AddBlockedIPRange("172.16.0.0/12");
});
```

### Middleware Configuration

```csharp
app.UseBadBotBlocker();
```

## Requirements

- **.NET 8.0 or higher**: The library utilizes the latest features of C# 12 and .NET 8.
- **ASP.NET Core Application**: Designed to work with ASP.NET Core middleware pipeline.

## License

This library is available under the [MIT License](LICENSE).

## Contributions

Pull requests and contributions are welcome! Please open an issue to discuss any changes before submitting a pull request.

## About

For more information or support, please visit the [GitHub Repository](https://github.com/zettersten/BadBotBlocker).

---

Thank you for using **BadBotBlocker**. We look forward to your contributions and feedback!
