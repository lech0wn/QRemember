using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

namespace QRemember.Web.Pages.Guest;

[AllowAnonymous]
public class GuestEventGalleryModel : PageModel
{
    private readonly AppDbContext _db;

    public GuestEventGalleryModel(AppDbContext db)
    {
        _db = db;
    }

    public record GalleryPhoto(int Id, string ImageUrl, string AuthorName, string? Caption);

    public string EventName { get; private set; } = string.Empty;
    public string EventCode { get; private set; } = string.Empty;
    public string EventDateDisplay { get; private set; } = string.Empty;
    public string OrganizerDisplayName { get; private set; } = string.Empty;
    public string? HeroImageUrl { get; private set; }
    public string EventHashtag { get; private set; } = string.Empty;
    public string? EventDescription { get; private set; }
    public IReadOnlyList<GalleryPhoto> Photos { get; private set; } = Array.Empty<GalleryPhoto>();

    public async Task<IActionResult> OnGetAsync(string? code = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            // Load mock data for preview if no code provided
            LoadMockData();
            return Page();
        }

        var normalizedCode = code.Trim();

        var eventEntity = await _db.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventCode.ToLower() == normalizedCode.ToLower() && e.IsActive, cancellationToken);

        if (eventEntity is null)
        {
            // If event not found, show mock data or return not found
            LoadMockData();
            return Page();
        }

        // Populate from database
        EventName = eventEntity.Name;
        EventCode = eventEntity.EventCode;
        EventHashtag = eventEntity.EventCode;
        EventDateDisplay = eventEntity.EventDate.ToString("MMMM d, yyyy");
        OrganizerDisplayName = eventEntity.Organizer?.DisplayName
            ?? eventEntity.Organizer?.Email
            ?? "Organizer";
        EventDescription = eventEntity.Description;

        // Get photos from database
        var photos = await _db.Photos
            .AsNoTracking()
            .Where(p => p.EventId == eventEntity.Id && p.IsApproved && !p.IsHidden)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new GalleryPhoto(
                p.Id,
                p.CloudinaryUrl,
                p.UploaderName ?? "Guest",
                p.Caption))
            .ToListAsync(cancellationToken);

        Photos = photos;

        // Use first photo as hero if available, otherwise null
        HeroImageUrl = photos.FirstOrDefault()?.ImageUrl;

        return Page();
    }

    private void LoadMockData()
    {
        // This is just for preview/testing when no real event is found
        EventName = "Sample Event";
        EventCode = "sample-event";
        EventHashtag = "sample-event";
        EventDateDisplay = DateTime.Now.AddDays(14).ToString("MMMM d, yyyy");
        OrganizerDisplayName = "Event Organizer";
        EventDescription = "This is a sample event. Create your own event to start collecting memories!";
        HeroImageUrl = null;

        Photos = new List<GalleryPhoto>();
    }
}