using LinkPreview.Polyfills;

namespace LinkPreview.Tests;

public class WhatNotLinkPreviewServiceTests
{
    [Fact]
    public async Task CanMakeRequestAndParseContent()
    {
        var service = new WhatNotLinkPreviewService();

        var urlsToTest = new List<string>
        {
            "https://www.whatnot.com/live/6b43784d-7609-456a-bcf1-fad3d517281e",
        };

        foreach (var item in urlsToTest)
        {
            var response = await service.GetLinkPreviewAsync(item);

            Assert.NotNull(response);
        }
    }
}
