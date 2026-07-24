using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

namespace QRemember.Web.Pages;

[AllowAnonymous]
public class SharePhotosModel : PageModel
{
    private readonly AppDbContext _db;

    public SharePhotosModel(AppDbContext db)
    {
        _db = db;
    }

    public int MaxPhotos { get; } = 15;

    [BindProperty(SupportsGet = true)]
    public string EventCode { get; set; } = string.Empty;

    public string EventHashtag { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public int EventId { get; set; }

    public async Task<IActionResult> OnGetAsync(string eventCode)
    {
        if (string.IsNullOrWhiteSpace(eventCode))
        {
            return NotFound();
        }

        var eventEntity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventCode == eventCode && e.IsActive);

        if (eventEntity is null)
        {
            return NotFound();
        }

        EventCode = eventEntity.EventCode;
        EventHashtag = eventEntity.EventCode;
        EventName = eventEntity.Name;
        EventId = eventEntity.Id;

        return Page();
    }

    public async Task<IActionResult> OnPostUploadAsync([FromBody] UploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventCode))
        {
            return new JsonResult(new { success = false, message = "Event code is required." });
        }

        var eventEntity = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == request.EventCode && e.IsActive);

        if (eventEntity is null)
        {
            return new JsonResult(new { success = false, message = "Event not found." });
        }

        // TODO: Process the uploaded photos
        // For now, just return success
        return new JsonResult(new { success = true, message = "Photos uploaded successfully!" });
    }

    public class UploadRequest
    {
        public string EventCode { get; set; } = string.Empty;
        public string GuestName { get; set; } = string.Empty;
        public List<string> PhotoData { get; set; } = new List<string>();
    }
}