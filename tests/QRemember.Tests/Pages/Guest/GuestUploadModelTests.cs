using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Pages.Guest;
using QRemember.Web.Services;

namespace QRemember.Tests.Pages.Guest;

public class GuestUploadModelTests
{
    // A 1x1 transparent PNG, base64-encoded, wrapped as a data URL - a realistic guest upload payload.
    private const string SamplePngDataUrl =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private static (GuestUploadModel Model, QRemember.Web.Data.AppDbContext Db, Mock<ICloudinaryImageService> Cloudinary) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var cloudinary = new Mock<ICloudinaryImageService>();
        var model = new GuestUploadModel(db, cloudinary.Object, NullLogger<GuestUploadModel>.Instance);
        PageModelTestHelpers.Bind(model);
        return (model, db, cloudinary);
    }

    private static Event ActiveEvent(string code = "PARTY1", bool autoApprove = false, DateTime? createdAt = null) => new()
    {
        EventCode = code,
        IsActive = true,
        OrganizerId = "org1",
        Name = "Party",
        AutoApprovePhotos = autoApprove,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    [Fact]
    public async Task OnGetAsync_EventNotFound_WhenNoCodeGiven()
    {
        var (model, _, _) = CreateModel();

        await model.OnGetAsync(null);

        Assert.False(model.EventFound);
        Assert.Null(model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_SetsError_WhenEventDoesNotExist()
    {
        var (model, _, _) = CreateModel();

        await model.OnGetAsync("nope");

        Assert.False(model.EventFound);
        Assert.Equal("Event not found or no longer active.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_SetsError_WhenEventHasExpired()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(ActiveEvent(createdAt: DateTime.UtcNow.AddDays(-(Event.LifespanDays + 1))));
        await db.SaveChangesAsync();

        await model.OnGetAsync("PARTY1");

        Assert.False(model.EventFound);
        Assert.Equal("This event has expired.", model.ErrorMessage);
    }

    [Fact]
    public async Task OnGetAsync_LoadsEventData_AndRecentApprovedPhotos()
    {
        var (model, db, _) = CreateModel();
        var ev = ActiveEvent();
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "approved.jpg", IsApproved = true, IsHidden = false });
        db.Photos.Add(new Photo { EventId = ev.Id, CloudinaryUrl = "pending.jpg", IsApproved = false, IsHidden = false });
        await db.SaveChangesAsync();

        await model.OnGetAsync("PARTY1");

        Assert.True(model.EventFound);
        Assert.Equal("Party", model.EventName);
        Assert.Single(model.RecentPhotos);
        Assert.Equal("approved.jpg", model.RecentPhotos[0].ImageUrl);
    }

    [Fact]
    public async Task OnPostScanAsync_ReturnsError_WhenQrDataIsMissing()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnPostScanAsync(new GuestUploadModel.ScanRequest { QrData = "" });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostScanAsync_ReturnsSuccess_WhenEventActiveAndNotExpired()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(ActiveEvent());
        await db.SaveChangesAsync();

        var result = await model.OnPostScanAsync(new GuestUploadModel.ScanRequest { QrData = "PARTY1" });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostLookupAsync_ReturnsError_WhenEventCodeMissing()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnPostLookupAsync(new GuestUploadModel.LookupRequest { EventCode = "" });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostUploadAsync_ReturnsError_WhenNoPhotosProvided()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(ActiveEvent());
        await db.SaveChangesAsync();

        var result = await model.OnPostUploadAsync(new GuestUploadModel.UploadRequest { EventCode = "PARTY1", PhotoData = new List<string>() });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostUploadAsync_ReturnsError_WhenBatchExceedsMax()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(ActiveEvent());
        await db.SaveChangesAsync();

        var tooMany = Enumerable.Repeat(SamplePngDataUrl, GuestUploadModel.MaxPhotosPerBatch + 1).ToList();
        var result = await model.OnPostUploadAsync(new GuestUploadModel.UploadRequest { EventCode = "PARTY1", PhotoData = tooMany });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.False(json.GetProperty<bool>("success"));
    }

    [Fact]
    public async Task OnPostUploadAsync_SavesPhotosAsPending_WhenAutoApproveOff()
    {
        var (model, db, cloudinary) = CreateModel();
        db.Events.Add(ActiveEvent(autoApprove: false));
        await db.SaveChangesAsync();
        cloudinary.Setup(c => c.UploadEventPhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://cdn.example/photo.png", "public-id-1"));

        var result = await model.OnPostUploadAsync(new GuestUploadModel.UploadRequest
        {
            EventCode = "PARTY1",
            GuestName = "Riley",
            PhotoData = new List<string> { SamplePngDataUrl }
        });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.Equal(1, json.GetProperty<int>("photoCount"));

        var saved = Assert.Single(db.Photos);
        Assert.Equal("Riley", saved.UploaderName);
        Assert.False(saved.IsApproved);
    }

    [Fact]
    public async Task OnPostUploadAsync_SavesPhotosAsApproved_WhenAutoApproveOn()
    {
        var (model, db, cloudinary) = CreateModel();
        db.Events.Add(ActiveEvent(autoApprove: true));
        await db.SaveChangesAsync();
        cloudinary.Setup(c => c.UploadEventPhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://cdn.example/photo.png", "public-id-1"));

        await model.OnPostUploadAsync(new GuestUploadModel.UploadRequest
        {
            EventCode = "PARTY1",
            PhotoData = new List<string> { SamplePngDataUrl }
        });

        var saved = Assert.Single(db.Photos);
        Assert.True(saved.IsApproved);
        Assert.Equal("Anonymous", saved.UploaderName);
    }

    [Fact]
    public async Task OnPostUploadAsync_SkipsUnparseablePhotoPayloads()
    {
        var (model, db, cloudinary) = CreateModel();
        db.Events.Add(ActiveEvent());
        await db.SaveChangesAsync();

        var result = await model.OnPostUploadAsync(new GuestUploadModel.UploadRequest
        {
            EventCode = "PARTY1",
            PhotoData = new List<string> { "not-a-data-url" }
        });

        var json = Assert.IsType<Microsoft.AspNetCore.Mvc.JsonResult>(result);
        Assert.True(json.GetProperty<bool>("success"));
        Assert.Equal(0, json.GetProperty<int>("photoCount"));
        Assert.Empty(db.Photos);
        cloudinary.Verify(c => c.UploadEventPhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
