using Microsoft.AspNetCore.Mvc;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Pages;
using QRemember.Web.Services;

namespace QRemember.Tests.Pages;

public class LandingModelTests
{
    private static (LandingModel Model, Mock<IEventLookupService> Lookup) CreateModel()
    {
        var lookup = new Mock<IEventLookupService>();
        var model = new LandingModel(lookup.Object);
        PageModelTestHelpers.Bind(model);
        return (model, lookup);
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_ReturnsError_WhenRequestIsNull()
    {
        var (model, _) = CreateModel();

        var result = await model.OnPostResolveCodeAsync(null!, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_ReturnsError_WhenDecodedTextIsWhitespace()
    {
        var (model, lookup) = CreateModel();

        var result = await model.OnPostResolveCodeAsync(new LandingModel.ResolveCodeRequest { DecodedText = "   " }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
        lookup.Verify(l => l.GetActiveEventByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_ExtractsTrailingSegment_WhenDecodedTextIsFullUrl()
    {
        var (model, lookup) = CreateModel();
        lookup.Setup(l => l.GetActiveEventByCodeAsync("abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        await model.OnPostResolveCodeAsync(
            new LandingModel.ResolveCodeRequest { DecodedText = "https://qremember.app/Guest/GuestEventGallery/abc123" },
            CancellationToken.None);

        lookup.Verify(l => l.GetActiveEventByCodeAsync("abc123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_UsesRawText_WhenDecodedTextIsBareCode()
    {
        var (model, lookup) = CreateModel();
        lookup.Setup(l => l.GetActiveEventByCodeAsync("bare-code", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        await model.OnPostResolveCodeAsync(new LandingModel.ResolveCodeRequest { DecodedText = "bare-code" }, CancellationToken.None);

        lookup.Verify(l => l.GetActiveEventByCodeAsync("bare-code", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_ReturnsError_WhenEventNotFound()
    {
        var (model, lookup) = CreateModel();
        lookup.Setup(l => l.GetActiveEventByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var result = await model.OnPostResolveCodeAsync(new LandingModel.ResolveCodeRequest { DecodedText = "unknown" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostResolveCodeAsync_ReturnsRedirectUrl_WhenEventFound()
    {
        var (model, lookup) = CreateModel();
        var ev = new Event { EventCode = "abc123" };
        lookup.Setup(l => l.GetActiveEventByCodeAsync("abc123", It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var urlHelper = Mock.Get(model.Url);
        urlHelper.Setup(u => u.RouteUrl(It.IsAny<Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext>()))
            .Returns("/Guest/GuestEventGallery?code=abc123");

        var result = await model.OnPostResolveCodeAsync(new LandingModel.ResolveCodeRequest { DecodedText = "abc123" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.Equal("/Guest/GuestEventGallery?code=abc123", json.GetProperty<string>("redirectUrl"));
    }
}
