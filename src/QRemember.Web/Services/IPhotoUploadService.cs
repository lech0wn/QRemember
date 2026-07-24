using QRemember.Web.Data;
using QRemember.Web.Models;

namespace QRemember.Web.Services;

public interface IPhotoUploadService
{
    Task<Photo> UploadPhotoAsync(int eventId, string imageData, string uploaderName, string? caption = null);
}

public class PhotoUploadService : IPhotoUploadService
{
    private readonly AppDbContext _db;
    private readonly ICloudinaryImageService _cloudinary;

    public PhotoUploadService(AppDbContext db, ICloudinaryImageService cloudinary)
    {
        _db = db;
        _cloudinary = cloudinary;
    }

    public async Task<Photo> UploadPhotoAsync(int eventId, string imageData, string uploaderName, string? caption = null)
    {
        // Upload to Cloudinary or other service
        var cloudinaryUrl = await _cloudinary.UploadAsync(imageData);

        var photo = new Photo
        {
            EventId = eventId,
            CloudinaryUrl = cloudinaryUrl,
            UploaderName = uploaderName,
            Caption = caption,
            UploadedAt = DateTime.UtcNow,
            IsApproved = true, // Or false if moderation is needed
            IsHidden = false
        };

        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();

        return photo;
    }
}