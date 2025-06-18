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

    [Theory]
    [InlineData("https://www.whatnot.com/live/6b43784d-7609-456a-bcf1-fad3d517281e")]
    [InlineData("https://www.whatnot.com/live/a865b6e1-6ba1-443c-8d8d-5666bd2c507e")]
    [InlineData("https://www.whatnot.com/live/17b291d0-4bd9-48b7-bfc2-67ceb0e94d98")]
    [InlineData(
        "https://www.whatnot.com/live/a865b6e1-6ba1-443c-8d8d-5666bd2c507e?app=web&sharing_channel=copyLink&invitedBy=zettersten&sender_id=11867393"
    )]
    [InlineData("https://www.whatnot.com/live/86c38e20-a300-4b6f-b7a0-6b16276302b2")]
    public async Task GetLinkPreviewWhatNotAsync_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);
        Assert.True(result.ImageType?.Length > 16);
    }

    [Theory]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DGTJiQrMdqf/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DGTJiQrMdqf")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFWDA_Vy3xu/?img_index=1")]
    [InlineData("https://www.instagram.com/p/DG5979qs3Zs/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DJRyygogdbZ/")]
    public async Task GetLinkPreviewInstagramAsync_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);
        Assert.True(result.ImageType?.Length > 16);

        Assert.DoesNotContain(
            "private media",
            result.Title,
            StringComparison.CurrentCultureIgnoreCase
        );

        Assert.DoesNotContain(
            "private media",
            result.Description,
            StringComparison.CurrentCultureIgnoreCase
        );
    }
}
