using QRemember.Web.Pages.Guest;

namespace QRemember.Web.Models.ViewModels;

public class UploadFormModel
{
    public string? EventName { get; set; }
    public string? EventDescription { get; set; }
    public string? EventCode { get; set; }
    public int MaxPhotos { get; set; }
    public IReadOnlyList<GuestEventGalleryModel.GalleryPhoto> RecentPhotos { get; set; } = Array.Empty<GuestEventGalleryModel.GalleryPhoto>();
}