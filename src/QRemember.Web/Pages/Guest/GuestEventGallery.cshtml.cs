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
    private readonly AppDbContext _dbContext;
    private readonly ILogger<GuestEventGalleryModel> _logger;

    public GuestEventGalleryModel(AppDbContext dbContext, ILogger<GuestEventGalleryModel> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
            LoadMockData();
            return Page();
        }

        var eventEntity = await GetEventByCodeAsync(code.Trim(), cancellationToken);
        
        if (eventEntity is null)
        {
            LoadMockData();
            return Page();
        }

        await LoadEventDataAsync(eventEntity, cancellationToken);
        return Page();
    }

    private async Task<Event?> GetEventByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.Events
            .AsNoTracking()
            .Include(e => e.Organizer)
            .FirstOrDefaultAsync(e => e.EventCode.ToLower() == code.ToLower() && e.IsActive, cancellationToken);
    }

    private async Task LoadEventDataAsync(Event eventEntity, CancellationToken cancellationToken)
    {
        EventName = eventEntity.Name;
        EventCode = eventEntity.EventCode;
        EventHashtag = eventEntity.EventCode;
        EventDateDisplay = eventEntity.EventDate.ToString("MMMM d, yyyy");
        OrganizerDisplayName = GetOrganizerDisplayName(eventEntity.Organizer);
        EventDescription = eventEntity.Description;

        var photos = await GetPhotosForEventAsync(eventEntity.Id, cancellationToken);
        Photos = photos;
        HeroImageUrl = photos.FirstOrDefault()?.ImageUrl;
    }

    private string GetOrganizerDisplayName(ApplicationUser? organizer)
    {
        return organizer?.DisplayName ?? organizer?.Email ?? "Organizer";
    }

    private async Task<List<GalleryPhoto>> GetPhotosForEventAsync(int eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.Photos
            .AsNoTracking()
            .Where(p => p.EventId == eventId && p.IsApproved && !p.IsHidden)
            .OrderByDescending(p => p.UploadedAt)
            .Select(p => new GalleryPhoto(
                p.Id,
                p.CloudinaryUrl,
                p.UploaderName ?? "Guest",
                p.Caption))
            .ToListAsync(cancellationToken);
    }

    private void LoadMockData()
    {
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