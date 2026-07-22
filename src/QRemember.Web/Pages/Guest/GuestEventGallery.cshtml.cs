using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

public class EventGalleryModel : PageModel
{
    private readonly AppDbContext _db;

    public EventGalleryModel(AppDbContext db)
    {
        _db = db;
    }

    public record GalleryPhoto(int Id, string ImageUrl, string AuthorName, string? Caption);

    public string EventName { get; private set; } = string.Empty;
    public string EventCode { get; private set; } = string.Empty;
    public string EventDateDisplay { get; private set; } = string.Empty;
    public string OrganizerDisplayName { get; private set; } = string.Empty;
    public string? HeroImageUrl { get; private set; }
    public IReadOnlyList<GalleryPhoto> Photos { get; private set; } = Array.Empty<GalleryPhoto>();

    public async Task<IActionResult> OnGetAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var normalizedCode = code.Trim().ToUpperInvariant();

        var eventEntity = await _db.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventCode == normalizedCode && e.IsActive, cancellationToken);

        if (eventEntity is null)
        {
            return NotFound();
        }

        EventName = eventEntity.Name;
        EventCode = eventEntity.EventCode;
        EventDateDisplay = eventEntity.EventDate.ToString("MMMM d yyyy");
        OrganizerDisplayName = eventEntity.Organizer?.DisplayName
            ?? eventEntity.Organizer?.Email
            ?? "Organizer";

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
        HeroImageUrl = photos.FirstOrDefault()?.ImageUrl;

        return Page();
    }
}
