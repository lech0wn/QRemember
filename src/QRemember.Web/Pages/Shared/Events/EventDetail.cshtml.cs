using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRemember.Web.Data;
using QRemember.Web.Models;

public class EventDetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EventDetailModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }


    public record PhotoCardViewModel(int Id, string ImageUrl, string UploaderName, string? Caption, string Status);

    public string EventName { get; private set; } = string.Empty;
    public string EventCode { get; private set; } = string.Empty;
    public bool AutoApprove { get; private set; }

    public List<PhotoCardViewModel> Photos { get; } = new();

    public async Task<IActionResult> OnGetAsync(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToPage("MyEvents");
        }

        var organizerId = _userManager.GetUserId(User);
        var organizerEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == code && e.OrganizerId == organizerId);

        if (organizerEvent is null)
        {
            return RedirectToPage("MyEvents");
        }

        EventCode = organizerEvent.EventCode;
        EventName = organizerEvent.Name;
        AutoApprove = organizerEvent.AutoApprovePhotos;

        var photos = await _db.Photos
            .AsNoTracking()
            .Where(p => p.EventId == organizerEvent.Id)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        foreach (var p in photos)
        {
            Photos.Add(new PhotoCardViewModel(p.Id, p.CloudinaryUrl, p.UploaderName ?? "Guest", p.Caption, p.Status));
        }

        return Page();
    }


    public async Task<IActionResult> OnPostSetStatusAsync([FromBody] SetStatusRequest request)
    {
        if (request is null)
        {
            return new JsonResult(new { success = false, message = "Bad Request." });
        }

        var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "approved", "hidden", "pending"};
        if(!validStatuses.Contains(request.Status))
        {
            return new JsonResult(new { success = false, message = "Invalid status."});
        }

        var organizerId = _userManager.GetUserId(User);

        var photo = await _db.Photos
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.Event.OrganizerId == organizerId);

        if (photo is null)
        {
            return new JsonResult(new { success = false, message = "Photo not found."});
        }

        switch (request.Status.ToLowerInvariant())
        {
            case "approved":
                photo.IsApproved = true;
                photo.IsHidden = false;
                break;
            case "hidden":
                photo.IsHidden = true;
                break;
            case "pending":
                photo.IsApproved = false;
                photo.IsHidden = false;
                break;
        }

        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, photoId = photo.Id, status = photo.Status });
    }


    public async Task<IActionResult> OnPostSetAutoApproveAsync([FromBody] SetAutoApproveRequest request)
    {
        if (request is null)
        {
            return new JsonResult(new { success = false, message = "Bad Request." });
        }

        var organizerId = _userManager.GetUserId(User);
        var organizerEvent = await _db.Events
            .FirstOrDefaultAsync(e => e.EventCode == request.EventCode && e.OrganizerId == organizerId);

        if (organizerEvent is null)
        {
            return new JsonResult(new { success = false, message = "Event not found." });
        }

        organizerEvent.AutoApprovePhotos = request.Enabled;
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, autoApprove = organizerEvent.AutoApprovePhotos });
    }


    public async Task<IActionResult> OnPostDeletePhotoAsync([FromBody] DeletePhotoRequest request)
    {
        if (request is null)
        {
            return new JsonResult(new { success = false });
        }

        var organizerId = _userManager.GetUserId(User);
        var photo = await _db.Photos
            .Include(p => p.Event)
            .FirstOrDefaultAsync(p => p.Id == request.PhotoId && p.Event.OrganizerId == organizerId);

        if (photo is null)
        {
            return new JsonResult(new { success = false});
        }

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync();

        return new JsonResult(new { success = true, photoId = photo.Id});
    }


    public class SetStatusRequest
    {
        public int PhotoId { get; set; }
        public string Status { get; set; } = "";
    }

    public class SetAutoApproveRequest
    {
        public string EventCode { get; set; } = "";
        public bool Enabled { get; set; }
    }

    public class DeletePhotoRequest
    {
        public int PhotoId { get; set; }
    }
}
