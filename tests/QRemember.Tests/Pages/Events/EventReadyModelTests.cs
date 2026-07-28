using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Services;

namespace QRemember.Tests.Pages.Events;

public class EventReadyModelTests
{
    private const string OrganizerId = "organizer-1";

    private static (EventReadyModel Model, QRemember.Web.Data.AppDbContext Db, Mock<IQrCodeService> QrCode) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var userManager = IdentityMockFactory.MockUserManager();
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(OrganizerId);
        var qrCode = new Mock<IQrCodeService>();

        var model = new EventReadyModel(db, userManager.Object, qrCode.Object);
        PageModelTestHelpers.Bind(model);
        return (model, db, qrCode);
    }

    [Fact]
    public async Task OnGetAsync_RedirectsToCreateEvent_WhenCodeIsMissing()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGetAsync(null);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("CreateEvent", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_RedirectsToCreateEvent_WhenEventNotFoundForOrganizer()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(new Event { EventCode = "ABC1", OrganizerId = "someone-else", QrCodeUrl = "https://x/ABC1" });
        await db.SaveChangesAsync();

        var result = await model.OnGetAsync("ABC1");

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_PopulatesEventDetails_AndGeneratesQrCode()
    {
        var (model, db, qrCode) = CreateModel();
        var ev = new Event
        {
            EventCode = "ABC1",
            OrganizerId = OrganizerId,
            Name = "Birthday Bash",
            QrCodeUrl = "https://example.com/guest/ABC1",
            CreatedAt = new DateTime(2026, 1, 1)
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        qrCode.Setup(q => q.GeneratePngDataUri("https://example.com/guest/ABC1", 20))
            .Returns("data:image/png;base64,xyz");

        var result = await model.OnGetAsync("ABC1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Birthday Bash", model.EventName);
        Assert.Equal("https://example.com/guest/ABC1", model.EventLink);
        Assert.Equal("data:image/png;base64,xyz", model.QrCodeDataUri);
        Assert.Equal(ev.ExpiresAt, model.ExpiresAt);
    }
}
