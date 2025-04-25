using LinkPreview.Polyfills;

namespace LinkPreview.Tests;

public class InstagramLinkPreviewServiceTests
{
    [Fact]
    public void CanParseInstagramHtmlContent()
    {
        var htmlContent = """
            <html>
            <head>
            <meta property="og:description" content="132 likes, 19 comments - veefriendscards on January 27, 2025: &quot;The 1/1 Adaptable Alien Super Sticker has landed! 👽🛸

            This ultra rare sticker was part of the Adaptable Alien Mystery Pin &amp; Sticker Boxes, an exclusive NYCC drop from October 2024. What makes it even more special? It’s signed by the creator and CEO of VeeFriends, @GaryVee ✍️ 

            #VeeFriends #NYCC #ComicCon #Stickers #oneofone&quot;. ">

            <meta property="og:url" content="https://www.instagram.com/p/DFWDA_Vy3xu/">
            <meta name="twitter:title" content="VeeFriends (&#064;veefriends) &#x2022; Instagram photos and videos" />
            <meta property="og:image" content="https://scontent-iad3-1.cdninstagram.com/v/t51.75761-15/475420923_17919349320030070_459570460250586686_n.jpg?stp=c216.0.648.648a_dst-jpg_e35_s640x640_tt6&amp;_nc_cat=102&amp;ccb=1-7&amp;_nc_sid=18de74&amp;_nc_ohc=ZuV4cgJBS5kQ7kNvwE5UnUS&amp;_nc_oc=AdkZrknYBj_MYsVNMPA-YaH9NPdpbw1c8qgEPLc2RZQLbTSpUp8SgeCND21HXcKC9Wo&amp;_nc_zt=23&amp;_nc_ht=scontent-iad3-1.cdninstagram.com&amp;_nc_gid=4Xx9QhoJv-K_08LiQ4HrzQ&amp;oh=00_AfGSVqji_aD4Ub6z7eGLeMTgniRQrq_orYh8VpEnvQfvEA&amp;oe=68101400">

            <title>VeeFriends Cards | The 1/1 Adaptable Alien Super Sticker has landed! 👽🛸

            This ultra rare sticker was part of the Adaptable Alien Mystery Pin &amp; Sticker Boxes,... | Instagram</title>
            </head>
            <body></body>
            </html>
            """;

        var responseToEval = InstagramLinkPreviewService.ParseInstagramResponse(htmlContent);

        Assert.NotNull(responseToEval);
        Assert.Equal("A post shared by VeeFriends (@veefriends)", responseToEval.Title);
        Assert.Equal(
            "The 1/1 Adaptable Alien Super Sticker has landed! 👽🛸This ultra rare sticker was part of the Adaptable Alien Mystery Pin & Sticker Boxes, an exclusive NYCC drop from October 2024. What makes it even more special? It’s signed by the creator and CEO of VeeFriends, @GaryVee ✍️ #VeeFriends #NYCC #ComicCon #Stickers #oneofone",
            responseToEval.Description
        );
        Assert.Equal("https://www.instagram.com/p/DFWDA_Vy3xu/", responseToEval.Url);
        Assert.Equal(
            "https://scontent-iad3-1.cdninstagram.com/v/t51.75761-15/475420923_17919349320030070_459570460250586686_n.jpg?stp=c216.0.648.648a_dst-jpg_e35_s640x640_tt6&amp;_nc_cat=102&amp;ccb=1-7&amp;_nc_sid=18de74&amp;_nc_ohc=ZuV4cgJBS5kQ7kNvwE5UnUS&amp;_nc_oc=AdkZrknYBj_MYsVNMPA-YaH9NPdpbw1c8qgEPLc2RZQLbTSpUp8SgeCND21HXcKC9Wo&amp;_nc_zt=23&amp;_nc_ht=scontent-iad3-1.cdninstagram.com&amp;_nc_gid=4Xx9QhoJv-K_08LiQ4HrzQ&amp;oh=00_AfGSVqji_aD4Ub6z7eGLeMTgniRQrq_orYh8VpEnvQfvEA&amp;oe=68101400",
            responseToEval.Image
        );
    }

    [Fact]
    public async Task CanMakeRequestAndParseContent()
    {
        var service = new InstagramLinkPreviewService();

        var response = await service.GetLinkPreviewAsync(
            "https://www.instagram.com/veefriendscards/p/DFWDA_Vy3xu/?img_index=1"
        );

        Assert.NotNull(response);
    }
}
