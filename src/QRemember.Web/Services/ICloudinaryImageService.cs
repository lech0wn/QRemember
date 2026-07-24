namespace QRemember.Web.Services;

public interface ICloudinaryImageService
{
    Task<string> UploadAsync(string imageData);
    Task<(string Url, string PublicId)> UploadEventPhotoAsync(
        Stream fileStream,
        string fileName,
        int eventId,
        CancellationToken cancellationToken = default);
}
