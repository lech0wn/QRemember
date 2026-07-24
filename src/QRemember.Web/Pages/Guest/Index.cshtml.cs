using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;

namespace QRemember.Web.Pages.Guest;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public string EventName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime EventDate { get; private set; }
    public bool EventFound { get; private set; }
    public bool EventExpired { get; private set; }
    public string EventCode { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string code)
    {
        EventCode = code;

        var organizerEvent = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventCode == code && e.IsActive);

        if (organizerEvent is null)
        {
            EventFound = false;
            return Page();
        }

        if (organizerEvent.ExpiresAt <= DateTime.UtcNow)
        {
            EventFound = false;
            EventExpired = true;
            return Page();
        }

        EventFound = true;
        EventName = organizerEvent.Name;
        Description = organizerEvent.Description;
        EventDate = organizerEvent.EventDate;

        return Page();
    }

    // Add a POST handler to redirect to gallery
    public IActionResult OnPostViewGallery(string code)
    {
        return RedirectToPage("/Guest/GuestEventGallery", new { code });
    }
}