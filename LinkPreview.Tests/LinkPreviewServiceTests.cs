using Microsoft.Extensions.DependencyInjection;

namespace LinkPreview.Tests;

public class LinkPreviewServiceTests : IClassFixture<LinkPreviewServiceFixture>
{
    private readonly ILinkPreviewService service;

    public LinkPreviewServiceTests(LinkPreviewServiceFixture fixture)
    {
        this.service = fixture.ServiceProvider.GetRequiredService<ILinkPreviewService>();
    }

    [Theory]
    [InlineData("https://veefriends.com/")]
    [InlineData("https://bestbuy.com")]
    [InlineData(
        "https://www.bestbuy.com/site/samsung-55-class-u7900-series-uhd-4k-smart-tizen-tv-2025/6632331.p?skuId=6632331"
    )]
    [InlineData(
        "https://www.amazon.com/Plexon-Vintage-Outdoor-Farmhouse-Container/dp/B0DYW9XXP1/?_encoding=UTF8&pd_rd_w=uF9VR&content-id=amzn1.sym.255b3518-6e7f-495c-8611-30a58648072e%3Aamzn1.symc.a68f4ca3-28dc-4388-a2cf-24672c480d8f&pf_rd_p=255b3518-6e7f-495c-8611-30a58648072e&pf_rd_r=42D11E4EVW5VC0JXH304&pd_rd_wg=lDhh0&pd_rd_r=b83bb70e-4fc3-4ad7-b0d4-4090be7158b6&ref_=pd_hp_d_atf_ci_mcx_mr_ca_hp_atf_d"
    )]
    [InlineData(
        "https://www.etsy.com/listing/1174856473/foragers-daughter-tarot-afterlight?ls=r&ref=rlp-listing-grid-2&external=1&space_id=1368461662182&frs=1&sts=1&content_source=5f13ae6f4b890822a5e3fdd28946a4260aeaf65a%253A1174856473&logging_key=5f13ae6f4b890822a5e3fdd28946a4260aeaf65a%3A1174856473"
    )]
    [InlineData("https://www.youtube.com/watch?v=-Q8c5vzYjfg")]
    public async Task GetLinkPreviewAsync_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(url, LinkPreviewOptionalField.Icon);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));
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
