using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Pages.Shared.Events;

namespace QRemember.Tests.Pages.Events;

public class CreateEventModelTests
{
    private const string OrganizerId = "organizer-1";

    private static (CreateEventModel Model, QRemember.Web.Data.AppDbContext Db) CreateModel(bool organizerSignedIn = true)
    {
        var db = InMemoryDbContextFactory.Create();
        var userManager = IdentityMockFactory.MockUserManager();
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(organizerSignedIn ? OrganizerId : null);

        var server = new Mock<IServer>();
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns("Production");

        var model = new CreateEventModel(db, userManager.Object, server.Object, env.Object);
        PageModelTestHelpers.Bind(model);
        model.HttpContext.Request.Scheme = "https";
        model.HttpContext.Request.Host = new HostString("app.example.com");

        return (model, db);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, _) = CreateModel();
        model.ModelState.AddModelError("Name", "Event name is required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsChallenge_WhenNoOrganizerSignedIn()
    {
        var (model, _) = CreateModel(organizerSignedIn: false);
        model.Name = "Some Event";
        model.EventDate = DateTime.UtcNow.AddDays(1);

        var result = await model.OnPostAsync();

        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_CreatesEvent_AndRedirectsToEventReady()
    {
        var (model, db) = CreateModel();
        model.Name = "  Birthday Bash!  ";
        model.Description = "   ";
        model.EventDate = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Unspecified);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("EventReady", redirect.PageName);

        var savedEvent = Assert.Single(db.Events);
        Assert.Equal(redirect.RouteValues!["code"], savedEvent.EventCode);
        Assert.Equal("Birthday Bash!", savedEvent.Name);
        Assert.Null(savedEvent.Description);
        Assert.Equal(OrganizerId, savedEvent.OrganizerId);
        Assert.True(savedEvent.IsActive);
        Assert.StartsWith("birthdaybash", savedEvent.EventCode);
        Assert.NotNull(savedEvent.QrCodeUrl);
        Assert.Contains(savedEvent.EventCode, savedEvent.QrCodeUrl);
    }

    [Fact]
    public async Task OnPostAsync_TrimsDescription_WhenProvided()
    {
        var (model, db) = CreateModel();
        model.Name = "Reunion";
        model.Description = "  Come one, come all  ";
        model.EventDate = new DateTime(2026, 12, 25);

        await model.OnPostAsync();

        var savedEvent = Assert.Single(db.Events);
        Assert.Equal("Come one, come all", savedEvent.Description);
    }

    [Fact]
    public async Task OnPostAsync_GeneratesUniqueEventCode_WhenSlugAlreadyTaken()
    {
        var (model, db) = CreateModel();
        // Pre-seed an event whose code shares the "reunion" slug prefix; the generator
        // appends a random suffix so a collision on the base slug alone shouldn't be possible,
        // but this exercises that code creation still succeeds when other reunion-slugged codes exist.
        db.Events.Add(new Event { Name = "Reunion", EventCode = "reunion-aaaaaa", OrganizerId = "someone-else" });
        await db.SaveChangesAsync();

        model.Name = "Reunion";
        model.EventDate = new DateTime(2026, 12, 25);

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal(2, db.Events.Count());
    }
}
