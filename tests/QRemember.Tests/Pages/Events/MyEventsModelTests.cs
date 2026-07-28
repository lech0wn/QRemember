using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Events;

public class MyEventsModelTests
{
    private const string OrganizerId = "organizer-1";

    private static (MyEventsModel Model, QRemember.Web.Data.AppDbContext Db) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var userManager = IdentityMockFactory.MockUserManager();
        userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(OrganizerId);

        var model = new MyEventsModel(db, userManager.Object);
        PageModelTestHelpers.Bind(model);
        return (model, db);
    }

    [Fact]
    public async Task OnGetAsync_OnlyReturnsEventsForCurrentOrganizer()
    {
        var (model, db) = CreateModel();
        db.Events.Add(new Event { Name = "Mine", EventCode = "MINE1", OrganizerId = OrganizerId });
        db.Events.Add(new Event { Name = "Not Mine", EventCode = "OTHER1", OrganizerId = "someone-else" });
        await db.SaveChangesAsync();

        await model.OnGetAsync();

        var card = Assert.Single(model.Events);
        Assert.Equal("Mine", card.Title);
    }

    [Fact]
    public async Task OnGetAsync_OrdersEventsByCreatedAtDescending()
    {
        var (model, db) = CreateModel();
        db.Events.Add(new Event { Name = "Older", EventCode = "OLD1", OrganizerId = OrganizerId, CreatedAt = new DateTime(2026, 1, 1) });
        db.Events.Add(new Event { Name = "Newer", EventCode = "NEW1", OrganizerId = OrganizerId, CreatedAt = new DateTime(2026, 6, 1) });
        await db.SaveChangesAsync();

        await model.OnGetAsync();

        Assert.Equal("Newer", model.Events[0].Title);
        Assert.Equal("Older", model.Events[1].Title);
    }

    [Fact]
    public async Task OnPostCancelAsync_RemovesEvent_WhenOwnedByCurrentOrganizer()
    {
        var (model, db) = CreateModel();
        db.Events.Add(new Event { Name = "To Cancel", EventCode = "CANCEL1", OrganizerId = OrganizerId });
        await db.SaveChangesAsync();

        await model.OnPostCancelAsync("CANCEL1");

        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task OnPostCancelAsync_DoesNothing_WhenEventBelongsToAnotherOrganizer()
    {
        var (model, db) = CreateModel();
        db.Events.Add(new Event { Name = "Not Mine", EventCode = "OTHER1", OrganizerId = "someone-else" });
        await db.SaveChangesAsync();

        await model.OnPostCancelAsync("OTHER1");

        Assert.Single(db.Events);
    }
}
