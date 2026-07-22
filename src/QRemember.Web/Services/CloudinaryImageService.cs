using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace QRemember.Web.Services;

public class CloudinaryImageService : ICloudinaryImageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryImageService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary;
    }

    public async Task<(string Url, string PublicId)> UploadEventPhotoAsync(
        Stream fileStream,
        string fileName,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = $"qremember/events/{eventId}",
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        return (result.SecureUrl.ToString(), result.PublicId);
    }
}
