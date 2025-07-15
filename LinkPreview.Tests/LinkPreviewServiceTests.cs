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
    [InlineData(
        "https://www.amazon.com/Plexon-Vintage-Outdoor-Farmhouse-Container/dp/B0DYW9XXP1/?_encoding=UTF8&pd_rd_w=uF9VR&content-id=amzn1.sym.255b3518-6e7f-495c-8611-30a58648072e%3Aamzn1.symc.a68f4ca3-28dc-4388-a2cf-24672c480d8f&pf_rd_p=255b3518-6e7f-495c-8611-30a58648072e&pf_rd_r=42D11E4EVW5VC0JXH304&pd_rd_wg=lDhh0&pd_rd_r=b83bb70e-4fc3-4ad7-b0d4-4090be7158b6&ref_=pd_hp_d_atf_ci_mcx_mr_ca_hp_atf_d"
    )]
    [InlineData(
        "https://www.etsy.com/listing/1174856473/foragers-daughter-tarot-afterlight?ls=r&ref=rlp-listing-grid-2&external=1&space_id=1368461662182&frs=1&sts=1&content_source=5f13ae6f4b890822a5e3fdd28946a4260aeaf65a%253A1174856473&logging_key=5f13ae6f4b890822a5e3fdd28946a4260aeaf65a%3A1174856473"
    )]
    [InlineData("https://www.youtube.com/watch?v=-Q8c5vzYjfg")]
    [InlineData("https://www.youtube.com/watch?v=lDVtXSpm378")]
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

    [Theory]
    [InlineData("https://x.com/808KJY/status/1735721434696360176")]
    [InlineData("https://x.com/JohnnyOeleven/status/1927810467449221210")]
    [InlineData("https://x.com/PokeScoot/status/1887180265681744111")]
    [InlineData("https://x.com/taek2212/status/1569812373313134595")]
    [InlineData("https://x.com/TheMateverse/status/1585736927491559424")]
    [InlineData("https://x.com/zachcyphers/status/1922095624368869521")]
    [InlineData("https://x.com/TwentyAfterIV/status/1549831089216262149")]
    [InlineData("https://x.com/Kry5tian_eth/status/1551265957410136064")]
    [InlineData("https://x.com/johnv1d/status/1660311123759972353")]
    [InlineData("https://x.com/adrianerwiltse/status/1581101724277751809")]
    [InlineData("https://x.com/Michael88059543/status/1743297835175403550")]
    [InlineData("https://x.com/Marshall_Macro/status/1557577400858185728")]
    [InlineData("https://x.com/johnv1d/status/1748019697944010836")]
    [InlineData("https://x.com/Jacob_tyler_m/status/1746936832556667061")]
    public async Task GetLinkPreviewXAsync_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(
            url,
            LinkPreviewOptionalField.ImageX | LinkPreviewOptionalField.ImageY
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);

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

    [Theory]
    [InlineData("https://www.instagram.com/p/DEXyYQsuH68/")]
    [InlineData("https://www.instagram.com/p/ClOb_lfLWCt/")]
    [InlineData("https://www.instagram.com/veefriends/p/C04FZbCOD_T/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/CsrEYQnrk-I/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/C199G60rHpO/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/DDX82NSRmjK/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/reel/C7M71UOvNoB/")]
    [InlineData("https://www.instagram.com/veefriends/p/C5bHf5TrAaJ/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/CgE5n_jOL4-/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/C8XkB8_v74-/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DEXyYQsuH68/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C-NwvVnsMI6/")]
    [InlineData("https://www.instagram.com/p/COdsnBrFtmT/")]
    [InlineData("https://www.instagram.com/p/COdsV9hlnWj/")]
    [InlineData("https://www.instagram.com/p/COeKa_Rl5TB/")]
    [InlineData("https://www.instagram.com/p/COeKWdYFAC_/")]
    [InlineData("https://www.instagram.com/p/DC1-WMRRvtY/")]
    [InlineData("https://www.instagram.com/p/COeJ9u1FY5Q/")]
    [InlineData("https://www.instagram.com/p/DCf3jDGsg56/")]
    [InlineData("https://www.instagram.com/p/COeJ0D6F7Eg/")]
    [InlineData("https://www.instagram.com/p/COeAm5IlXqg/")]
    [InlineData("https://www.instagram.com/p/COd-fzmF1BV/")]
    [InlineData("https://www.instagram.com/p/CcOb_ItPaiS/")]
    [InlineData("https://www.instagram.com/p/C2Csu0sv251/")]
    [InlineData("https://www.instagram.com/p/CdV-lQWr9tH/")]
    [InlineData("https://www.instagram.com/p/COdvr1YnmS1/")]
    [InlineData("https://www.instagram.com/p/COdIOmrnBa2/")]
    [InlineData("https://www.instagram.com/p/COdCf3THHPn/")]
    [InlineData("https://www.instagram.com/p/CXG6wFXlX8O/")]
    [InlineData("https://www.instagram.com/p/CQRyGc7lJZu/")]
    [InlineData("https://www.instagram.com/p/COc91-7nTxz/")]
    [InlineData("https://www.instagram.com/p/COc9gz_nt-B/")]
    [InlineData("https://www.instagram.com/p/COdHQi0Fx2r/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C9dKFNiJ05o/")]
    [InlineData("https://www.instagram.com/veefriends/p/C9dal6AP38j/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/reel/CyjKZgbvh-C/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFDksyEueeU/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DCPCwqRukVx/?img_index=9")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C_LYil6sRRu/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DCmOlk3upmE/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C-xcqQaudZk/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DGTJiQrMdqf")]
    [InlineData("https://www.instagram.com/veefriends/reel/Cy6nDR3PD6t/")]
    [InlineData("https://www.instagram.com/p/CZmgbUkF_gm/")]
    [InlineData("https://www.instagram.com/p/CS7Rkgzlaoo/")]
    [InlineData("https://www.instagram.com/veefriends/p/C3Thz_qvMWJ/?img_index=1")]
    [InlineData("https://www.instagram.com/p/COdTbRfF-0N/")]
    [InlineData("https://www.instagram.com/veefriends/p/CyLc_3kOfno/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C1sdXwouU6W/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DC5B2JYyEiI/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendsapparel/p/DHbJ3xuRheS/?img_index=5")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C6q-npbMel_/")]
    [InlineData("https://www.instagram.com/p/DC4rZHvSplA/")]
    [InlineData("https://www.instagram.com/p/Cz_eFgIOBRM/")]
    [InlineData("https://www.instagram.com/p/C1CbF3fLoi3/")]
    [InlineData("https://www.instagram.com/p/COyot4ohmZM/")]
    [InlineData("https://www.instagram.com/p/COxxQU2sOZf/")]
    [InlineData("https://www.instagram.com/veefriends/p/DEyMdcqRvJB/?img_index=1")]
    [InlineData("https://www.instagram.com/p/DHUVaKEyA5q/?img_index=6")]
    [InlineData("https://www.instagram.com/p/COdXRRxF-q8/")]
    [InlineData("https://www.instagram.com/veefriends/p/CgE5tF3Ok2B/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DCm_ZsLSQix/?img_index=9")]
    [InlineData("https://www.instagram.com/p/COdXpm_FyJ1/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C1ulR9mMdiS/")]
    [InlineData("https://www.instagram.com/veefriends/reel/C84YZAGOOHu/")]
    [InlineData("https://www.instagram.com/p/COdYbsvFrDm/")]
    [InlineData("https://www.instagram.com/p/CPHuwFbDLlq/")]
    [InlineData("https://www.instagram.com/p/Ca-fV-iOf7v/")]
    [InlineData("https://www.instagram.com/p/CxEIBwQPQqx/")]
    [InlineData("https://www.instagram.com/p/COdYnyrFrJR/")]
    [InlineData("https://www.instagram.com/veefriends/p/C_dspmrvlp4/?img_index=8")]
    [InlineData("https://www.instagram.com/p/COdY7B5lk5G/")]
    [InlineData("https://www.instagram.com/p/DHMKqVWvmdK/?img_index=10")]
    [InlineData("https://www.instagram.com/p/COdZVTSlXP0/")]
    [InlineData("https://www.instagram.com/p/CyQy6OprUCf/")]
    [InlineData("https://www.instagram.com/p/COdZbyQlw8k/")]
    [InlineData("https://www.instagram.com/p/COdZiTKl5LS/")]
    [InlineData("https://www.instagram.com/reel/C6grJYfMIic/")]
    [InlineData("https://www.instagram.com/veefriends/p/DEfcmpfxW9r/?img_index=9")]
    [InlineData("https://www.instagram.com/veefriendsapparel/p/DHwMiELRx8z/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C-JQIstMdqt/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DHbYLKVs41a/?img_index=4")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C-VwqTgpseR/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DF57KjvSw11/?img_index=4")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DDsB7MMxgpp/?img_index=3")]
    [InlineData("https://www.instagram.com/p/DCf3amfMDFU/")]
    [InlineData("https://www.instagram.com/p/Ckn62RqLJfo/")]
    [InlineData("https://www.instagram.com/p/DLcv8OKsjNp/")]
    [InlineData("https://www.instagram.com/p/COdR4XJFx5K/")]
    [InlineData("https://www.instagram.com/p/CUXfEN3ruA2/")]
    [InlineData("https://www.instagram.com/p/Cdn48qAO2wH/")]
    [InlineData("https://www.instagram.com/p/CO5y_r0AlHX/")]
    [InlineData("https://www.instagram.com/p/C68uoTjO1Vo/")]
    [InlineData("https://www.instagram.com/p/DEKuZUrR8nm/")]
    [InlineData("https://www.instagram.com/p/CbKtJ4crYjQ/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DCm_ZsLSQix/?img_index=7")]
    [InlineData("https://www.instagram.com/veefriends/p/DBPA5OpyuiT/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFu_pFLOo0x/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFa1CLDRtVH/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFGog-HyaBn/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DC1o27iOYpU/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DCEuRlMu4eT/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DBwK5gcOdWA/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DBt9l5ay76e/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DBePKaQuPtm/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DBjjKleRZ3S/")]
    [InlineData("https://www.instagram.com/vintagestockreserve/reel/DCjgBnxxO_h/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/DCzfvNUy9zA/")]
    [InlineData("https://www.instagram.com/veefriendscards/reel/C-A5-rGuzEy/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/C6rkmYvurTU/?img_index=1")]
    [InlineData("https://www.instagram.com/psacard/p/DHUVaKEyA5q/?img_index=11")]
    [InlineData("https://www.instagram.com/p/C9hh-tuu5Cs/")]
    [InlineData("https://www.instagram.com/nikolas.ilic/p/DAjL3DHSRTG/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/C3fZav3tmfA/?img_index=5")]
    [InlineData("https://www.instagram.com/veefriends/p/C4q1gNCvaw-/?img_index=1")]
    [InlineData("https://www.instagram.com/veefriends/p/C_yMZWVPovS/")]
    [InlineData("https://www.instagram.com/veefriends/reel/C_9M8hnvQE1/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/DFLGE6gOoaF/?img_index=3")]
    [InlineData("https://www.instagram.com/p/DKPxx_MRzl3/")]
    [InlineData("https://www.instagram.com/p/C4QiPzyLNGc/")]
    [InlineData("https://www.instagram.com/veefriendscards/p/C2fa9s1ufeg/?img_index=1")]
    [InlineData("https://www.instagram.com/p/CeO_hiDvXGR/")]
    [InlineData("https://www.instagram.com/p/DHlogQls8se/")]
    public async Task GetLinkPreviewForKnownFailuresAsync_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(
            url,
            LinkPreviewOptionalField.ImageX | LinkPreviewOptionalField.ImageY
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);

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

    [Theory]
    [InlineData("https://x.com/djsmeadows/status/1870129791078367436")]
    [InlineData("https://www.instagram.com/p/DK04p9GMe-4/")]
    public async Task GetLinkPreviewForKnownVideos_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(
            url,
            LinkPreviewOptionalField.ImageX | LinkPreviewOptionalField.ImageY
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);

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

    [Theory]
    [InlineData("https://giphy.com/embed/TRaV5IQ2NvKYYXvq9Q")]
    [InlineData("https://giphy.com/gifs/VeeFriends-sloth-veefriends-vee-friend-TRaV5IQ2NvKYYXvq9Q")]
    [InlineData(
        "https://media.giphy.com/media/v1.Y2lkPWVjZjA1ZTQ3OGVpbHlobm1pNW04OGpreHdra2FubDZyZ3h0aG5yOHRhd3JuZ3FieCZlcD12MV9naWZzX3NlYXJjaCZjdD1n/nnegoZlVjaG9dzW37l/giphy.gif"
    )]
    public async Task GetLinkPreviewForGiphy_ReturnsResult(string url)
    {
        // Act
        var result = await this.service.GetLinkPreviewAsync(
            url,
            LinkPreviewOptionalField.ImageX | LinkPreviewOptionalField.ImageY
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Title));
        Assert.False(string.IsNullOrEmpty(result.Image));

        Assert.True(result.ImageHeight > 0);
        Assert.True(result.ImageWidth > 0);

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
