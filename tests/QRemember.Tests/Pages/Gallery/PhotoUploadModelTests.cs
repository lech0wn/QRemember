using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using QRemember.Tests.TestHelpers;
using QRemember.Web.Models;
using QRemember.Web.Services;

namespace QRemember.Tests.Pages.Gallery;

public class PhotoUploadModelTests
{
    private static (PhotoUploadModel Model, QRemember.Web.Data.AppDbContext Db, Mock<ICloudinaryImageService> Cloudinary) CreateModel()
    {
        var db = InMemoryDbContextFactory.Create();
        var cloudinary = new Mock<ICloudinaryImageService>();
        var model = new PhotoUploadModel(db, cloudinary.Object);
        PageModelTestHelpers.Bind(model);
        return (model, db, cloudinary);
    }

    private static Mock<IFormFile> CreateFormFile(string contentType = "image/png", int length = 100, string fileName = "photo.png")
    {
        var file = new Mock<IFormFile>();
        file.Setup(f => f.ContentType).Returns(contentType);
        file.Setup(f => f.Length).Returns(length);
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(new byte[length]));
        return file;
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenEventCodeDoesNotExist()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnGetAsync("nope", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetAsync_LoadsEventName_WhenEventExists()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(new Event { EventCode = "PARTY1", IsActive = true, Name = "Party", OrganizerId = "org1" });
        await db.SaveChangesAsync();

        var result = await model.OnGetAsync("party1", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Equal("Party", model.EventName);
        Assert.Equal("PARTY1", model.EventCode);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsNotFound_WhenEventCodeDoesNotExist()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnPostAsync("nope", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_ReturnsPage_WhenModelStateInvalid()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = "org1" });
        await db.SaveChangesAsync();
        model.ModelState.AddModelError("Input.UploaderName", "Required");

        var result = await model.OnPostAsync("PARTY1", CancellationToken.None);

        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPostAsync_AddsModelError_WhenPhotoFileIsMissing()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = "org1" });
        await db.SaveChangesAsync();
        model.Input = new PhotoUploadModel.UploadInput { UploaderName = "Alex", PhotoFile = null };

        var result = await model.OnPostAsync("PARTY1", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_AddsModelError_WhenContentTypeNotAllowed()
    {
        var (model, db, _) = CreateModel();
        db.Events.Add(new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = "org1" });
        await db.SaveChangesAsync();
        model.Input = new PhotoUploadModel.UploadInput { UploaderName = "Alex", PhotoFile = CreateFormFile(contentType: "application/pdf").Object };

        var result = await model.OnPostAsync("PARTY1", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
    }

    [Fact]
    public async Task OnPostAsync_UploadsPhoto_AndRedirectsToGallery_OnSuccess()
    {
        var (model, db, cloudinary) = CreateModel();
        var ev = new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = "org1" };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        cloudinary.Setup(c => c.UploadEventPhotoAsync(It.IsAny<Stream>(), "photo.png", ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://cdn.example/photo.png", "public-id-1"));

        model.Input = new PhotoUploadModel.UploadInput
        {
            UploaderName = "  Alex  ",
            Caption = "  Great day  ",
            PhotoFile = CreateFormFile().Object
        };

        var result = await model.OnPostAsync("PARTY1", CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Shared/Events/EventDetail", redirect.PageName);

        var saved = Assert.Single(db.Photos);
        Assert.Equal("Alex", saved.UploaderName);
        Assert.Equal("Great day", saved.Caption);
        Assert.Equal("https://cdn.example/photo.png", saved.CloudinaryUrl);
        Assert.True(saved.IsApproved);
    }

    [Fact]
    public async Task OnPostAsync_AddsModelError_WhenCloudinaryUploadFails()
    {
        var (model, db, cloudinary) = CreateModel();
        db.Events.Add(new Event { EventCode = "PARTY1", IsActive = true, OrganizerId = "org1" });
        await db.SaveChangesAsync();

        cloudinary.Setup(c => c.UploadEventPhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cloudinary is down"));

        model.Input = new PhotoUploadModel.UploadInput { UploaderName = "Alex", PhotoFile = CreateFormFile().Object };

        var result = await model.OnPostAsync("PARTY1", CancellationToken.None);

        Assert.IsType<PageResult>(result);
        var error = Assert.Single(model.ModelState[string.Empty]!.Errors);
        Assert.Equal("Cloudinary is down", error.ErrorMessage);
        Assert.Empty(db.Photos);
    }
}
