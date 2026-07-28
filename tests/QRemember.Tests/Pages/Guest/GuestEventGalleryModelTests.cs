using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Pages.Guest;

namespace QRemember.Tests.Pages.Guest;

public class GuestEventGalleryModelTests
{
    private static (GuestEventGalleryModel Model, QRemember.Web.Data.AppDbContext Db) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var model = new GuestEventGalleryModel(db, NullLogger<GuestEventGalleryModel>.Instance);
        PageModelTestHelpers.Bind(model);
        return (model, db);
    }

    [Fact]
    public async Task OnGetAsync_LoadsMockData_WhenNoCodeProvided()
    {
        var (model, _) = CreateModel();

        var result = await model.OnGetAsync(null);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Sample Event", model.EventName);
        Assert.Empty(model.Photos);
    }

    [Fact]
    public async Task OnGetAsync_LoadsMockData_WhenEventCodeNotFound()
    {
        var (model, _) = CreateModel();

        var result = await model.OnGetAsync("does-not-exist");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Sample Event", model.EventName);
    }

    [Fact]
    public async Task OnGetAsync_LoadsMockData_WhenEventIsInactive()
    {
        var (model, db) = CreateModel();
        var organizer = new ApplicationUser { Email = "organizer@example.com" };
        db.Users.Add(organizer);
        db.Events.Add(new Event { EventCode = "inactive1", IsActive = false, OrganizerId = organizer.Id });
        await db.SaveChangesAsync();

        await model.OnGetAsync("inactive1");

        Assert.Equal("Sample Event", model.EventName);
    }

    [Fact]
    public async Task OnGetAsync_MatchesEventCode_CaseInsensitively()
    {
        var (model, db) = CreateModel();
        var organizer = new ApplicationUser { Email = "organizer@example.com", DisplayName = "Organizer Person" };
        db.Users.Add(organizer);
        db.Events.Add(new Event
        {
            EventCode = "MixedCase1",
            IsActive = true,
            OrganizerId = organizer.Id,
            Organizer = organizer,
            Name = "Big Party",
            EventDate = new DateTime(2026, 3, 1)
        });
        await db.SaveChangesAsync();

        var result = await model.OnGetAsync("mixedcase1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("Big Party", model.EventName);
        Assert.Equal("Organizer Person", model.OrganizerDisplayName);
    }

    [Fact]
    public async Task OnGetAsync_OnlyShowsApprovedAndNotHiddenPhotos_NewestFirst()
    {
        var (model, db) = CreateModel();
        var organizer = new ApplicationUser { Email = "organizer@example.com" };
        db.Users.Add(organizer);
        var ev = new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = organizer.Id };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "old.jpg", IsApproved = true, IsHidden = false, UploadedAt = new DateTime(2026, 1, 1) });
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "new.jpg", IsApproved = true, IsHidden = false, UploadedAt = new DateTime(2026, 6, 1) });
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "pending.jpg", IsApproved = false, IsHidden = false, UploadedAt = new DateTime(2026, 7, 1) });
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "hidden.jpg", IsApproved = true, IsHidden = true, UploadedAt = new DateTime(2026, 8, 1) });
        await db.SaveChangesAsync();

        await model.OnGetAsync("PARTY1");

        Assert.Equal(2, model.Photos.Count);
        Assert.Equal("new.jpg", model.Photos[0].ImageUrl);
        Assert.Equal("old.jpg", model.Photos[1].ImageUrl);
        Assert.Equal("new.jpg", model.HeroImageUrl);
    }
}
