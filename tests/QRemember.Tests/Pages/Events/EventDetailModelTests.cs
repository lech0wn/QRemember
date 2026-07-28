using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;

namespace QRemember.Tests.Pages.Events;

public class EventDetailModelTests
{
    private const string OrganizerId = "organizer-1";

    private static (EventDetailModel Model, QRemember.Web.Data.AppDbContext Db) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var userManager = IdentityMockFactory.MockUserManager();
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(OrganizerId);

        var model = new EventDetailModel(db, userManager.Object);
        PageModelTestHelpers.Bind(model);
        return (model, db);
    }

    [Fact]
    public async Task OnGetAsync_RedirectsToMyEvents_WhenCodeIsMissing()
    {
        var (model, _) = CreateModel();

        var result = await model.OnGetAsync(null);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("MyEvents", redirect.PageName);
    }

    [Fact]
    public async Task OnGetAsync_RedirectsToMyEvents_WhenEventNotOwnedByCurrentOrganizer()
    {
        var (model, db) = CreateModel();
        db.Events.Add(new Event { EventCode = "ABC1", OrganizerId = "someone-else" });
        await db.SaveChangesAsync();

        var result = await model.OnGetAsync("ABC1");

        Assert.IsType<RedirectToPageResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_LoadsPhotos_OrderedByUploadedAtDescending()
    {
        var (model, db) = CreateModel();
        var ev = new Event { EventCode = "ABC1", OrganizerId = OrganizerId, Name = "My Event", AutoApprovePhotos = true };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "u1", UploadedAt = new DateTime(2026, 1, 1) });
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "u2", UploadedAt = new DateTime(2026, 6, 1) });
        await db.SaveChangesAsync();

        var result = await model.OnGetAsync("ABC1");

        Assert.IsType<PageResult>(result);
        Assert.Equal("My Event", model.EventName);
        Assert.True(model.AutoApprove);
        Assert.Equal(2, model.Photos.Count);
        Assert.Equal("u2", model.Photos[0].ImageUrl);
    }

    [Fact]
    public async Task OnPostSetStatusAsync_ReturnsError_ForInvalidStatus()
    {
        var (model, _) = CreateModel();

        var result = await model.OnPostSetStatusAsync(new EventDetailModel.SetStatusRequest { PhotoId = 1, Status = "bogus" });

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostSetStatusAsync_ReturnsError_WhenPhotoNotFoundForOrganizer()
    {
        var (model, db) = CreateModel();
        var ev = new Event { EventCode = "ABC1", OrganizerId = "someone-else" };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "u1" });
        await db.SaveChangesAsync();

        var result = await model.OnPostSetStatusAsync(new EventDetailModel.SetStatusRequest { PhotoId = 1, Status = "approved" });

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostSetStatusAsync_UpdatesPhotoStatus_ToHidden()
    {
        var (model, db) = CreateModel();
        var ev = new Event { EventCode = "ABC1", OrganizerId = OrganizerId };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var photo = new Photo { EventId = ev.Id, CloudinaryUrl = "u1", IsApproved = true, IsHidden = false };
        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        var result = await model.OnPostSetStatusAsync(new EventDetailModel.SetStatusRequest { PhotoId = photo.Id, Status = "hidden" });

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.Equal("hidden", json.GetProperty<string>("status"));
        Assert.True((await db.Photos.FindAsync(photo.Id))!.IsHidden);
    }

    [Fact]
    public async Task OnPostSetAutoApproveAsync_UpdatesFlag_WhenEventOwnedByOrganizer()
    {
        var (model, db) = CreateModel();
        var ev = new Event { EventCode = "ABC1", OrganizerId = OrganizerId, AutoApprovePhotos = false };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var result = await model.OnPostSetAutoApproveAsync(new EventDetailModel.SetAutoApproveRequest { EventCode = "ABC1", Enabled = true });

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.True((await db.Events.FindAsync(ev.Id))!.AutoApprovePhotos);
    }

    [Fact]
    public async Task OnPostDeletePhotoAsync_RemovesPhoto_WhenOwnedByOrganizer()
    {
        var (model, db) = CreateModel();
        var ev = new Event { EventCode = "ABC1", OrganizerId = OrganizerId };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var photo = new Photo { EventId = ev.Id, CloudinaryUrl = "u1" };
        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        var result = await model.OnPostDeletePhotoAsync(new EventDetailModel.DeletePhotoRequest { PhotoId = photo.Id });

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.Empty(db.Photos);
    }
}
