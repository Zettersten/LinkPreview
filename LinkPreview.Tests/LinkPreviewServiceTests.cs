using Microsoft.Extensions.DependencyInjection;

namespace LinkPreview.Tests;

public class LinkPreviewServiceTests : IClassFixture<LinkPreviewServiceFixture>
{
    private readonly ILinkPreviewService service;

    public LinkPreviewServiceTests(LinkPreviewServiceFixture fixture)
    {
        this.service = fixture.ServiceProvider.GetRequiredService<ILinkPreviewService>();
    }

    [Fact]
    public async Task GetLinkPreviewAsync_ReturnsResult()
    {
        // Arrange
        var url = "https://www.example.com";

        // Act
        var result = await this.service.GetLinkPreviewAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
    }

    [Fact]
    public async Task GetLinkPreviewWhatNotAsync_ReturnsResult()
    {
        // Arrange
        var url = "https://www.whatnot.com/live/6b43784d-7609-456a-bcf1-fad3d517281e";

        // Act
        var result = await this.service.GetLinkPreviewAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
    }

    [Fact]
    public async Task GetLinkPreviewInstagramAsync_ReturnsResult()
    {
        // Arrange
        var url = "https://www.instagram.com/veefriendscards/reel/DGTJiQrMdqf";

        // Act
        var result = await this.service.GetLinkPreviewAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
    }
}
